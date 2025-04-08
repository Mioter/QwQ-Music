using System;
using System.Threading;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities.Tasks;

/// <summary>
/// 异步手动重置事件（线程安全）
/// </summary>
internal sealed class AsyncManualResetEvent : IDisposable
{
    private readonly Lock _syncRoot = new();
    private TaskCompletionSource<bool> _tcs = new();
    private bool _isDisposed;

    /// <summary>
    /// 等待信号
    /// </summary>
    /// <returns>等待任务</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public Task WaitAsync()
    {
        ThrowIfDisposed();
        lock (_syncRoot)
        {
            return _tcs.Task;
        }
    }

    /// <summary>
    /// 设置信号
    /// </summary>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public void Set()
    {
        ThrowIfDisposed();
        lock (_syncRoot)
            _tcs.TrySetResult(true);
    }

    /// <summary>
    /// 重置信号
    /// </summary>
    /// <returns>重置操作的任务</returns>
    /// <exception cref="ObjectDisposedException">对象已释放时抛出</exception>
    public Task ResetAsync()
    {
        ThrowIfDisposed();
        lock (_syncRoot)
        {
            if (!_tcs.Task.IsCompleted)
                return Task.CompletedTask;

            _tcs = new TaskCompletionSource<bool>();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _tcs.TrySetCanceled();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
