using System;
using System.Threading;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 消息总线扩展方法
/// </summary>
public static class MessageBusExtensions
{
    /// <summary>
    /// 订阅一次性消息，处理后自动取消订阅
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="messageBus">消息总线</param>
    /// <param name="handler">消息处理器</param>
    /// <returns>订阅令牌</returns>
    public static IDisposable? SubscribeOnce<TMessage>(this IMessageBus messageBus, Action<TMessage> handler)
    {
        // 使用闭包安全的方式创建一个可变引用
        var subscriptionRef = new SubscriptionReference<IDisposable?>();

        subscriptionRef.Value = messageBus.Subscribe<TMessage>(message =>
        {
            try
            {
                handler(message);
            }
            finally
            {
                subscriptionRef.Value?.Dispose();
            }
        });

        return subscriptionRef.Value;
    }

    /// <summary>
    /// 等待特定消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="messageBus">消息总线</param>
    /// <param name="predicate">消息过滤条件，可选</param>
    /// <param name="timeout">超时时间（毫秒），默认为无限等待</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收到的消息</returns>
    public static Task<TMessage> WaitForMessageAsync<TMessage>(
        this IMessageBus messageBus,
        Func<TMessage, bool>? predicate = null,
        int timeout = Timeout.Infinite,
        CancellationToken cancellationToken = default
    )
    {
        var taskCompletionSource = new TaskCompletionSource<TMessage>();
        var subscriptionRef = new SubscriptionReference<IDisposable?>();

        // 创建取消令牌源
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // 如果设置了超时，添加超时取消
        if (timeout != Timeout.Infinite)
        {
            cts.CancelAfter(timeout);
        }

        // 注册取消回调
        cts.Token.Register(() =>
        {
            subscriptionRef.Value?.Dispose();
            if (!taskCompletionSource.Task.IsCompleted)
            {
                taskCompletionSource.TrySetCanceled();
            }
        });

        // 订阅消息
        subscriptionRef.Value = messageBus.Subscribe<TMessage>(message =>
        {
            if (predicate == null || predicate(message))
            {
                subscriptionRef.Value?.Dispose();
                taskCompletionSource.TrySetResult(message);
            }
        });

        return taskCompletionSource.Task;
    }

    /// <summary>
    /// 发布消息并等待特定回复
    /// </summary>
    /// <typeparam name="TMessage">发送的消息类型</typeparam>
    /// <typeparam name="TResponse">期望的回复消息类型</typeparam>
    /// <param name="messageBus">消息总线</param>
    /// <param name="message">要发送的消息</param>
    /// <param name="predicate">回复消息过滤条件</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收到的回复消息</returns>
    public static async Task<TResponse> PublishAndWaitForResponseAsync<TMessage, TResponse>(
        this IMessageBus messageBus,
        TMessage message,
        Func<TResponse, bool>? predicate = null,
        int timeout = 5000,
        CancellationToken cancellationToken = default
    )
    {
        // 先订阅回复
        var responseTask = messageBus.WaitForMessageAsync(predicate, timeout, cancellationToken);

        // 发送消息
        await messageBus.PublishAsync(message);

        // 等待回复
        return await responseTask;
    }

    /// <summary>
    /// 用于安全地在闭包中引用可变对象的辅助类
    /// </summary>
    private class SubscriptionReference<T>
    {
        public T? Value { get; set; }
    }
}
