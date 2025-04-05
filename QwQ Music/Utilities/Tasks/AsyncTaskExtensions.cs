using System;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities.Tasks;

/// <summary>
/// 异步任务扩展方法
/// </summary>
public static class AsyncTaskExtensions
{
    /// <summary>
    /// 使用可控任务包装现有任务
    /// </summary>
    /// <param name="task">要包装的任务</param>
    /// <param name="onCompleted">完成时的回调</param>
    /// <param name="exceptionHandler">异常处理器</param>
    /// <returns>任务控制句柄</returns>
    public static IAsyncTaskHandle? WithControl(
        this Task task,
        Action? onCompleted = null,
        Action<Exception>? exceptionHandler = null
    )
    {
        return AsyncTaskManager.CreateTask(
            async ct =>
            {
                var tcs = new TaskCompletionSource<bool>();
                await using var ctr = ct.Register(() => tcs.TrySetCanceled());

                await task.ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                            tcs.TrySetException(t.Exception!.InnerExceptions);
                        else if (t.IsCanceled)
                            tcs.TrySetCanceled();
                        else
                            tcs.TrySetResult(true);
                    },
                    TaskContinuationOptions.ExecuteSynchronously
                );

                await tcs.Task;
            },
            onCompleted,
            exceptionHandler
        );
    }

    /// <summary>
    /// 使用可控任务包装现有带返回值的任务
    /// </summary>
    /// <typeparam name="T">返回值类型</typeparam>
    /// <param name="task">要包装的任务</param>
    /// <param name="onCompleted">完成时的回调</param>
    /// <param name="exceptionHandler">异常处理器</param>
    /// <returns>任务控制句柄和结果任务</returns>
    public static (IAsyncTaskHandle Handle, Task<T> ResultTask) WithControl<T>(
        this Task<T> task,
        Action<T>? onCompleted = null,
        Action<Exception>? exceptionHandler = null
    )
    {
        return AsyncTaskManager.CreateTask(
            async ct =>
            {
                var tcs = new TaskCompletionSource<T>();
                await using var ctr = ct.Register(() => tcs.TrySetCanceled());

                await task.ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                            tcs.TrySetException(t.Exception!.InnerExceptions);
                        else if (t.IsCanceled)
                            tcs.TrySetCanceled();
                        else
                            tcs.TrySetResult(t.Result);
                    },
                    TaskContinuationOptions.ExecuteSynchronously
                );

                return await tcs.Task;
            },
            onCompleted,
            exceptionHandler
        );
    }

    /// <summary>
    /// 添加超时控制
    /// </summary>
    /// <param name="handle">任务控制句柄</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="onTimeout">超时回调</param>
    /// <returns>原始任务控制句柄</returns>
    public static IAsyncTaskHandle WithTimeout(this IAsyncTaskHandle handle, TimeSpan timeout, Action? onTimeout = null)
    {
        var timeoutHandle = AsyncTaskManager.CreateDelayTask(
            timeout,
            () =>
            {
                if (handle.Status is not (AsyncTaskStatus.Running or AsyncTaskStatus.Paused))
                    return;
                handle.Cancel();
                onTimeout?.Invoke();
            }
        );

        // 当原始任务完成时，取消超时任务
        _ = handle.WaitForCompletionAsync().ContinueWith(_ => timeoutHandle.Dispose());

        return handle;
    }
}
