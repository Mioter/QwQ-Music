using System;
using System.Threading;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities.Tasks;

/// <summary>
/// 异步任务控制句柄接口
/// </summary>
public interface IAsyncTaskHandle : IDisposable
{
    /// <summary>
    /// 获取当前任务状态
    /// </summary>
    AsyncTaskStatus Status { get; }

    /// <summary>
    /// 获取是否已请求取消
    /// </summary>
    bool IsCancellationRequested { get; }

    /// <summary>
    /// 获取是否已释放资源
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// 获取取消令牌
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// 暂停任务执行
    /// </summary>
    /// <returns>暂停操作的任务</returns>
    Task PauseAsync();

    /// <summary>
    /// 恢复任务执行
    /// </summary>
    void Resume();

    /// <summary>
    /// 取消任务
    /// </summary>
    void Cancel();

    /// <summary>
    /// 等待任务完成
    /// </summary>
    /// <returns>等待任务</returns>
    Task WaitForCompletionAsync();
}
