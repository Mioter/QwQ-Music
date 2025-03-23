using System;
using System.Threading;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities;

/// <summary>
/// 异步任务管理器，提供带生命周期控制的任务操作
/// </summary>
public static class AsyncTaskManager
{
    /// <summary>
    /// 创建并启动可控延迟任务
    /// </summary>
    public static AsyncTaskHandle CreateDelayTask(
        TimeSpan delay,
        Action onCompleted,
        Action<Exception>? exceptionHandler = null
    )
    {
        var handle = new AsyncTaskHandle();
        _ = RunDelayTaskAsync(delay, onCompleted, handle, exceptionHandler);
        return handle;
    }

    /// <summary>
    /// 创建并启动可控异步任务
    /// </summary>
    public static AsyncTaskHandle CreateTask(
        Func<CancellationToken, Task> taskFactory,
        Action? onCompleted = null,
        Action<Exception>? exceptionHandler = null
    )
    {
        var handle = new AsyncTaskHandle();
        _ = RunGeneralTaskAsync(taskFactory, onCompleted, handle, exceptionHandler);
        return handle;
    }

    private async static Task RunDelayTaskAsync(
        TimeSpan delay,
        Action onCompleted,
        AsyncTaskHandle handle,
        Action<Exception>? exceptionHandler
    )
    {
        try
        {
            handle.TransitionTo(AsyncTaskStatus.Running);
            var startTime = DateTime.UtcNow;
            var elapsed = TimeSpan.Zero;

            while (elapsed < delay && !handle.IsDisposed)
            {
                await handle.WaitIfPausedAsync();

                var remaining = delay - elapsed;
                var waitTime = remaining.TotalMilliseconds > 100 ? TimeSpan.FromMilliseconds(100) : remaining;

                try
                {
                    await Task.Delay(waitTime, handle.CancellationToken);
                }
                catch (TaskCanceledException) when (handle.IsCancellationRequested)
                {
                    return;
                }

                elapsed = DateTime.UtcNow - startTime;
            }

            if (ShouldInvokeCompletion(handle))
            {
                handle.TransitionTo(AsyncTaskStatus.Completed);
                onCompleted();
            }
        }
        catch (Exception ex)
        {
            HandleTaskError(handle, ex, exceptionHandler);
        }
    }

    private async static Task RunGeneralTaskAsync(
        Func<CancellationToken, Task> taskFactory,
        Action? onCompleted,
        AsyncTaskHandle handle,
        Action<Exception>? exceptionHandler
    )
    {
        try
        {
            handle.TransitionTo(AsyncTaskStatus.Running);
            await handle.WaitIfPausedAsync();

            await taskFactory(handle.CancellationToken).ConfigureAwait(false);

            if (ShouldInvokeCompletion(handle))
            {
                handle.TransitionTo(AsyncTaskStatus.Completed);
                onCompleted?.Invoke();
            }
        }
        catch (OperationCanceledException) when (handle.IsCancellationRequested)
        {
            handle.TransitionTo(AsyncTaskStatus.Cancelled);
        }
        catch (Exception ex)
        {
            HandleTaskError(handle, ex, exceptionHandler);
        }
    }

    private static bool ShouldInvokeCompletion(AsyncTaskHandle handle) =>
        handle is { IsDisposed: false, IsCancellationRequested: false };

    private static void HandleTaskError(AsyncTaskHandle handle, Exception ex, Action<Exception>? handler)
    {
        handle.TransitionTo(AsyncTaskStatus.Faulted);
        handler?.Invoke(ex);
    }
}

/// <summary>
/// 异步任务状态枚举
/// </summary>
public enum AsyncTaskStatus
{
    Created,
    Running,
    Paused,
    Completed,
    Cancelled,
    Faulted,
}

/// <summary>
/// 异步任务控制句柄（线程安全）
/// </summary>
public sealed class AsyncTaskHandle : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Lock _statusLock = new();
    private AsyncTaskStatus _status = AsyncTaskStatus.Created;
    private readonly AsyncManualResetEvent _pauseEvent = new();

    /// <summary>
    /// 获取当前任务状态
    /// </summary>
    public AsyncTaskStatus Status
    {
        get
        {
            lock (_statusLock)
                return _status;
        }
    }

    /// <summary>
    /// 获取是否已请求取消
    /// </summary>
    public bool IsCancellationRequested => _cts.IsCancellationRequested;

    /// <summary>
    /// 获取是否已释放资源
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// 获取取消令牌
    /// </summary>
    public CancellationToken CancellationToken => _cts.Token;

    /// <summary>
    /// 暂停任务执行（线程安全）
    /// </summary>
    public async Task PauseAsync()
    {
        lock (_statusLock)
        {
            if (_status != AsyncTaskStatus.Running)
                return;
            TransitionTo(AsyncTaskStatus.Paused);
        }
        await _pauseEvent.ResetAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// 恢复任务执行（线程安全）
    /// </summary>
    public void Resume()
    {
        lock (_statusLock)
        {
            if (_status != AsyncTaskStatus.Paused)
                return;
            TransitionTo(AsyncTaskStatus.Running);
            _pauseEvent.Set(); // ✅ 先转换状态再触发信号
        }
    }

    /// <summary>
    /// 取消任务（线程安全）
    /// </summary>
    public void Cancel()
    {
        lock (_statusLock)
        {
            if (_status >= AsyncTaskStatus.Completed)
                return;
            TransitionTo(AsyncTaskStatus.Cancelled);
        }
        _cts.Cancel();
        _pauseEvent.Set();
    }

    /// <summary>
    /// 等待暂停状态解除
    /// </summary>
    internal async Task WaitIfPausedAsync()
    {
        while (Status == AsyncTaskStatus.Paused)
        {
            await _pauseEvent.WaitAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 执行状态转换（线程安全）
    /// </summary>
    internal void TransitionTo(AsyncTaskStatus newStatus)
    {
        lock (_statusLock)
        {
            ValidateStatusTransition(newStatus);
            _status = newStatus;
        }
    }

    private void ValidateStatusTransition(AsyncTaskStatus newStatus)
    {
        if (_status == newStatus)
            return;

        bool valid = (_status, newStatus) switch
        {
            (AsyncTaskStatus.Created, AsyncTaskStatus.Running) => true,
            (AsyncTaskStatus.Running, AsyncTaskStatus.Paused) => true,
            (AsyncTaskStatus.Running, AsyncTaskStatus.Completed) => true,
            (AsyncTaskStatus.Running, AsyncTaskStatus.Cancelled) => true,
            (AsyncTaskStatus.Running, AsyncTaskStatus.Faulted) => true,
            (AsyncTaskStatus.Paused, AsyncTaskStatus.Running) => true,
            (AsyncTaskStatus.Paused, AsyncTaskStatus.Cancelled) => true,
            (_, AsyncTaskStatus.Faulted) => true,
            _ => false,
        };

        if (!valid)
            throw new InvalidOperationException($"无效状态转换: {_status} -> {newStatus}");
    }

    /// <summary>
    /// 释放资源并取消任务
    /// </summary>
    public void Dispose()
    {
        lock (_statusLock)
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
        }

        Cancel();
        _cts.Dispose();
        _pauseEvent.Dispose();
    }
}

/// <summary>
/// 异步手动重置事件（线程安全）
/// </summary>
internal sealed class AsyncManualResetEvent : IDisposable
{
    private readonly object _syncRoot = new();
    private TaskCompletionSource<bool> _tcs = new();
    private bool _isDisposed;

    public Task WaitAsync()
    {
        CheckDisposed();
        lock (_syncRoot)
        {
            return _tcs.Task; // ✅ 正确返回等待任务
        }
    }

    public void Set()
    {
        CheckDisposed();
        lock (_syncRoot)
            _tcs.TrySetResult(true);
    }

    public Task ResetAsync()
    {
        CheckDisposed();
        lock (_syncRoot)
        {
            if (!_tcs.Task.IsCompleted)
                return Task.CompletedTask;

            _tcs = new TaskCompletionSource<bool>();
            return Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            _isDisposed = true;
            _tcs.TrySetCanceled();
        }
    }

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
