using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 弱引用消息总线，使用弱引用保存订阅者，避免内存泄漏
/// </summary>
public sealed class WeakMessageBus : MessageBusBase
{
    private static readonly Lazy<WeakMessageBus> _instance = new(() => new WeakMessageBus());

    /// <summary>
    /// 获取 WeakMessageBus 的单例实例
    /// </summary>
    public static WeakMessageBus Instance => _instance.Value;

    /// <summary>
    /// 创建订阅对象
    /// </summary>
    protected override object CreateSubscription<TMessage>(Action<TMessage> handler)
    {
        return new WeakReference(handler);
    }

    /// <summary>
    /// 获取有效的消息处理器列表
    /// </summary>
    protected override List<Action<TMessage>> GetValidHandlers<TMessage>()
    {
        if (!Subscriptions.TryGetValue(typeof(TMessage), out var entry))
            return [];

        // 获取线程安全的订阅项快照
        var subscriptions = entry.GetSubscriptionsSnapshot();

        // 分离存活/失效的订阅项
        var validHandlers = new List<Action<TMessage>>();
        var deadSubscriptions = new List<object>();

        foreach (var subscription in subscriptions.OfType<WeakReference>())
        {
            if (subscription.IsAlive)
            {
                if (subscription.Target is Action<TMessage> handler)
                    validHandlers.Add(handler);
            }
            else
            {
                deadSubscriptions.Add(subscription);
            }
        }

        // 清理失效订阅项
        foreach (object? dead in deadSubscriptions)
        {
            entry.RemoveSubscription(dead);
        }

        return validHandlers;
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
            .OfType<WeakReference>()
            .Where(weakRef => weakRef is { IsAlive: true, Target: Action<TMessage> target } && target.Equals(handler))
            .Cast<object>()
            .ToList();
    }
}
