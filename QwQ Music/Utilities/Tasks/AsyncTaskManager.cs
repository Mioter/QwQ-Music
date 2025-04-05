using System;
using System.Threading;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities.Tasks;

/// <summary>
/// 异步任务管理器，提供带生命周期控制的任务操作
/// </summary>
public static class AsyncTaskManager
{
    /// <summary>
    /// 创建并启动可控延迟任务
    /// </summary>
    /// <param name="delay">延迟时间</param>
    /// <param name="onCompleted">完成时的回调</param>
    /// <param name="exceptionHandler">异常处理器</param>
    /// <returns>任务控制句柄</returns>
    public static IAsyncTaskHandle CreateDelayTask(
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
    /// <param name="taskFactory">任务工厂</param>
    /// <param name="onCompleted">完成时的回调</param>
    /// <param name="exceptionHandler">异常处理器</param>
    /// <returns>任务控制句柄</returns>
    public static IAsyncTaskHandle CreateTask(
        Func<CancellationToken, Task> taskFactory,
        Action? onCompleted = null,
        Action<Exception>? exceptionHandler = null
    )
    {
        var handle = new AsyncTaskHandle();
        _ = RunGeneralTaskAsync(taskFactory, onCompleted, handle, exceptionHandler);
        return handle;
    }

    /// <summary>
    /// 创建并启动带返回值的可控异步任务
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="taskFactory">任务工厂</param>
    /// <param name="onCompleted">完成时的回调</param>
    /// <param name="exceptionHandler">异常处理器</param>
    /// <returns>任务控制句柄和结果任务</returns>
    public static (IAsyncTaskHandle Handle, Task<T> ResultTask) CreateTask<T>(
        Func<CancellationToken, Task<T>> taskFactory,
        Action<T>? onCompleted = null,
        Action<Exception>? exceptionHandler = null
    )
    {
        var handle = new AsyncTaskHandle();
        var resultTask = RunGeneralTaskWithResultAsync(taskFactory, onCompleted, handle, exceptionHandler);
        return (handle, resultTask);
    }

    /// <summary>
    /// 创建并启动周期性任务
    /// </summary>
    /// <param name="interval">执行间隔</param>
    /// <param name="action">执行的操作</param>
    /// <param name="exceptionHandler">异常处理器</param>
    /// <returns>任务控制句柄</returns>
    public static IAsyncTaskHandle CreatePeriodicTask(
        TimeSpan interval,
        Action action,
        Action<Exception>? exceptionHandler = null
    )
    {
        var handle = new AsyncTaskHandle();
        _ = RunPeriodicTaskAsync(interval, action, handle, exceptionHandler);
        return handle;
    }

    private static async Task RunDelayTaskAsync(
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
                handle.Complete();
                onCompleted();
            }
        }
        catch (Exception ex)
        {
            HandleTaskError(handle, ex, exceptionHandler);
        }
    }

    private static async Task RunGeneralTaskAsync(
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
                handle.Complete();
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

    private static async Task<T> RunGeneralTaskWithResultAsync<T>(
        Func<CancellationToken, Task<T>> taskFactory,
        Action<T>? onCompleted,
        AsyncTaskHandle handle,
        Action<Exception>? exceptionHandler
    )
    {
        try
        {
            handle.TransitionTo(AsyncTaskStatus.Running);
            await handle.WaitIfPausedAsync();

            var result = await taskFactory(handle.CancellationToken).ConfigureAwait(false);

            if (ShouldInvokeCompletion(handle))
            {
                handle.Complete();
                onCompleted?.Invoke(result);
            }

            return result;
        }
        catch (OperationCanceledException) when (handle.IsCancellationRequested)
        {
            handle.TransitionTo(AsyncTaskStatus.Cancelled);
            throw;
        }
        catch (Exception ex)
        {
            HandleTaskError(handle, ex, exceptionHandler);
            throw;
        }
    }

    private static async Task RunPeriodicTaskAsync(
        TimeSpan interval,
        Action action,
        AsyncTaskHandle handle,
        Action<Exception>? exceptionHandler
    )
    {
        try
        {
            handle.TransitionTo(AsyncTaskStatus.Running);

            while (handle is { IsDisposed: false, IsCancellationRequested: false })
            {
                await handle.WaitIfPausedAsync();

                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                        exceptionHandler(ex);
                    else
                        throw;
                }

                try
                {
                    await Task.Delay(interval, handle.CancellationToken);
                }
                catch (TaskCanceledException) when (handle.IsCancellationRequested)
                {
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            HandleTaskError(handle, ex, exceptionHandler);
        }
    }

    private static bool ShouldInvokeCompletion(AsyncTaskHandle? handle) =>
        handle is { IsDisposed: false, IsCancellationRequested: false };

    private static void HandleTaskError(AsyncTaskHandle handle, Exception ex, Action<Exception>? handler)
    {
        handle.Fault(ex);
        handler?.Invoke(ex);
    }
}
