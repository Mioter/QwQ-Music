using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 弱引用消息总线类，使用弱引用来管理订阅者。
/// </summary>
public class WeakMessageBus : MessageBusBase
{
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<WeakMessageBus> _instance = new(() => new WeakMessageBus());

    /// <summary>
    /// 获取 WeakMessageBus 的单例实例。
    /// </summary>
    public static WeakMessageBus Instance => _instance.Value;

    /// <summary>
    /// 创建订阅项。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <param name="handler">处理程序。</param>
    /// <returns>订阅项。</returns>
    protected override object CreateSubscription<TMessage>(Action<TMessage> handler)
    {
        return new WeakReference(handler);
    }

    /// <summary>
    /// 获取有效的处理程序列表，并清理无效的订阅者。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <returns>有效的处理程序列表。</returns>
    protected override List<Action<TMessage>> GetValidHandlers<TMessage>()
    {
        var messageType = typeof(TMessage);
        if (!Subscriptions.TryGetValue(messageType, out var entry))
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

        // 清理失效订阅项（线程安全操作）
        foreach (object dead in deadSubscriptions)
        {
            entry.RemoveSubscription(dead);
        }


        return validHandlers;
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
            .OfType<WeakReference>()
            .Where(weakRef => 
                weakRef.IsAlive && 
                weakRef.Target is Action<TMessage> target && 
                target.Equals(handler))
            .Cast<object>()
            .ToList();
    }
}
