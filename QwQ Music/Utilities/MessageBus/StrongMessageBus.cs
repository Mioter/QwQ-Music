using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 强引用消息总线类，使用强引用来管理订阅者。
/// </summary>
public class StrongMessageBus : MessageBusBase
{
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<StrongMessageBus> _instance = new(() => new StrongMessageBus());

    /// <summary>
    /// 获取 StrongMessageBus 的单例实例。
    /// </summary>
    public static StrongMessageBus Instance => _instance.Value;

    /// <summary>
    /// 创建订阅项。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <param name="handler">处理程序。</param>
    /// <returns>订阅项。</returns>
    protected override object CreateSubscription<TMessage>(Action<TMessage> handler)
    {
        return handler;
    }

    /// <summary>
    /// 获取有效的处理程序列表。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <returns>有效的处理程序列表。</returns>
    protected override List<Action<TMessage>> GetValidHandlers<TMessage>()
    {
        var messageType = typeof(TMessage);
        if (!Subscriptions.TryGetValue(messageType, out var entry))
            return [];
    
        return entry.GetSubscriptionsSnapshot()
            .Cast<Action<TMessage>>()
            .ToList();
    }

    /// <summary>
    /// 查找要移除的订阅项。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <param name="entry">订阅项。</param>
    /// <param name="handler">处理程序。</param>
    /// <returns>要移除的订阅项列表。</returns>
    protected override List<object> FindSubscriptionsToRemove<TMessage>(SubscriptionEntry entry, Action<TMessage> handler)
    {
        return entry.GetSubscriptionsSnapshot()
            .Where(sub => sub is Action<TMessage> action && action.Equals(handler))
            .ToList();
    }
}
