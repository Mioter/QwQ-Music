using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 强引用消息总线，使用强引用保存订阅者，适用于生命周期明确的场景
/// </summary>
public sealed class StrongMessageBus : MessageBusBase
{
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<StrongMessageBus> _instance = new(() => new StrongMessageBus());

    /// <summary>
    /// 获取 StrongMessageBus 的单例实例
    /// </summary>
    public static StrongMessageBus Instance => _instance.Value;
    
    /// <summary>
    /// 总线名称
    /// </summary>
    protected override string BusName => "StrongMessageBus";

    /// <summary>
    /// 创建订阅对象
    /// </summary>
    protected override object CreateSubscription<TMessage>(Action<TMessage> handler)
    {
        return handler;
    }

    /// <summary>
    /// 获取有效的消息处理器列表
    /// </summary>
    protected override List<Action<TMessage>> GetValidHandlers<TMessage>()
    {
        if (!Subscriptions.TryGetValue(typeof(TMessage), out var entry))
        {
            return [];
        }

        return entry.GetSubscriptionsSnapshot().Cast<Action<TMessage>>().ToList();
    }

    /// <summary>
    /// 查找要移除的订阅
    /// </summary>
    protected override List<object> FindSubscriptionsToRemove<TMessage>(
        SubscriptionEntry entry,
        Action<TMessage> handler
    )
    {
        return entry
            .GetSubscriptionsSnapshot()
            .Where(sub => sub is Action<TMessage> action && action.Equals(handler))
            .ToList();
    }
}