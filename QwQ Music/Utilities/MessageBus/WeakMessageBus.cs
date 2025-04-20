using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 弱引用消息总线，使用弱引用保存订阅者，避免内存泄漏
/// </summary>
public sealed class WeakMessageBus : MessageBusBase
{
    // 单例实现
    private static readonly Lazy<WeakMessageBus> _instance = new(() => new WeakMessageBus());

    /// <summary>
    /// 获取 WeakMessageBus 的单例实例
    /// </summary>
    public static WeakMessageBus Instance => _instance.Value;
    
    /// <summary>
    /// 上次清理无效引用的时间
    /// </summary>
    private DateTime _lastCleanupTime = DateTime.Now;
    
    /// <summary>
    /// 清理间隔（秒）
    /// </summary>
    public int CleanupIntervalSeconds { get; set; } = 60;
    
    /// <summary>
    /// 总线名称
    /// </summary>
    protected override string BusName => "WeakMessageBus";
    
    /// <summary>
    /// 构造函数
    /// </summary>
    private WeakMessageBus()
    {
        // 默认不启用性能监控，因为弱引用消息总线通常用于UI等场景
        EnablePerformanceMonitoring = false;
    }

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
        {
            return [];
        }

        CheckForGlobalCleanup();
        
        var handlers = new List<Action<TMessage>>();
        var deadReferences = new List<object>();
        
        foreach (var subscription in entry.GetSubscriptionsSnapshot())
        {
            if (subscription is WeakReference weakRef && weakRef.Target is Action<TMessage> handler)
            {
                handlers.Add(handler);
            }
            else
            {
                deadReferences.Add(subscription);
            }
        }
        
        // 移除无效的弱引用
        foreach (var deadRef in deadReferences)
        {
            entry.RemoveSubscription(deadRef);
        }
        
        return handlers;
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
            .Where(sub => sub is WeakReference weakRef && 
                          weakRef.Target is Action<TMessage> action && 
                          action.Equals(handler))
            .ToList();
    }
    
    /// <summary>
    /// 检查是否需要进行全局清理
    /// </summary>
    private void CheckForGlobalCleanup()
    {
        var now = DateTime.Now;
        if ((now - _lastCleanupTime).TotalSeconds >= CleanupIntervalSeconds)
        {
            CleanupDeadReferences();
            _lastCleanupTime = now;
        }
    }
    
    /// <summary>
    /// 清理所有失效的弱引用
    /// </summary>
    private void CleanupDeadReferences()
    {
        foreach (var entry in Subscriptions.Values)
        {
            var subscriptions = entry.GetSubscriptionsSnapshot();
            var deadReferences = subscriptions
                .Where(sub => sub is WeakReference weakRef && !weakRef.IsAlive)
                .ToList();
                
            foreach (var deadRef in deadReferences)
            {
                entry.RemoveSubscription(deadRef);
            }
        }
    }
    
    /// <summary>
    /// 发布消息前执行清理
    /// </summary>
    public override IMessageBus Publish<TMessage>(TMessage message)
    {
        CheckForGlobalCleanup();
        return base.Publish(message);
    }
}
