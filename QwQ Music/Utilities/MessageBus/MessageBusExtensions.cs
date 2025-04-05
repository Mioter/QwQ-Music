using System;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 消息总线扩展方法
/// </summary>
public static class MessageBusExtensions
{
    /// <summary>
    /// 异步发布消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="messageBus">消息总线</param>
    /// <param name="message">消息实例</param>
    /// <returns>表示异步操作的任务</returns>
    public static Task PublishAsync<TMessage>(this IMessageBus messageBus, TMessage message)
    {
        return Task.Run(() => messageBus.Publish(message));
    }

    /// <summary>
    /// 订阅指定类型的消息，并在接收到消息时执行指定的操作
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <typeparam name="TTarget">目标对象类型</typeparam>
    /// <param name="messageBus">消息总线</param>
    /// <param name="target">目标对象</param>
    /// <param name="action">要执行的操作</param>
    /// <returns>订阅令牌，用于取消订阅</returns>
    public static IDisposable Subscribe<TMessage, TTarget>(
        this IMessageBus messageBus,
        TTarget target,
        Action<TTarget, TMessage> action
    )
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(action);

        return messageBus.Subscribe<TMessage>(message => action(target, message));
    }

    /// <summary>
    /// 使用过滤条件订阅消息，只有满足条件的消息才会被处理
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="messageBus">消息总线</param>
    /// <param name="handler">消息处理器</param>
    /// <param name="filter">消息过滤条件，返回true表示处理该消息</param>
    /// <returns>订阅令牌，用于取消订阅</returns>
    public static IDisposable SubscribeWithFilter<TMessage>(
        this IMessageBus messageBus,
        Action<TMessage> handler,
        Func<TMessage, bool> filter
    )
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(filter);

        return messageBus.Subscribe<TMessage>(message =>
        {
            if (filter(message))
                handler(message);
        });
    }
}
