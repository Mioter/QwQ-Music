using System;
using System.Threading;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities.Tasks;

/// <summary>
/// 异步任务控制句柄（线程安全）
/// </summary>
public sealed class AsyncTaskHandle : IAsyncTaskHandle
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Lock _statusLock = new();
    private AsyncTaskStatus _status = AsyncTaskStatus.Created;
    private readonly AsyncManualResetEvent _pauseEvent = new();
    private readonly TaskCompletionSource<bool> _completionSource = new();

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
            _pauseEvent.Set();
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
        _completionSource.TrySetCanceled();
    }

    /// <summary>
    /// 等待任务完成
    /// </summary>
    public Task WaitForCompletionAsync() => _completionSource.Task;

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

            // 如果是终止状态，设置完成信号
            if (newStatus is AsyncTaskStatus.Completed or AsyncTaskStatus.Cancelled or AsyncTaskStatus.Faulted)
            {
                if (newStatus == AsyncTaskStatus.Completed)
                    _completionSource.TrySetResult(true);
                else if (newStatus == AsyncTaskStatus.Cancelled)
                    _completionSource.TrySetCanceled();
                else
                    _completionSource.TrySetException(new Exception("Task faulted"));
            }
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
    /// 标记任务完成
    /// </summary>
    internal void Complete()
    {
        TransitionTo(AsyncTaskStatus.Completed);
    }

    /// <summary>
    /// 标记任务失败
    /// </summary>
    internal void Fault(Exception exception)
    {
        TransitionTo(AsyncTaskStatus.Faulted);
        _completionSource.TrySetException(exception);
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
