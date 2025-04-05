using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 消息总线基类，提供消息发布和订阅的基本实现
/// </summary>
public abstract class MessageBusBase : IMessageBus
{
    /// <summary>
    /// 存储所有订阅信息的线程安全字典
    /// </summary>
    protected readonly ConcurrentDictionary<Type, SubscriptionEntry> Subscriptions = new();

    /// <summary>
    /// 发布消息到总线
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="message">消息实例</param>
    public void Publish<TMessage>(TMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var handlers = GetValidHandlers<TMessage>();

        // 异步执行所有处理器，避免阻塞发布者
        if (handlers.Count > 0)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    handler(message);
                }
                catch (Exception ex)
                {
                    // 处理异常，避免一个处理器的异常影响其他处理器
                    OnHandlerException(ex, typeof(TMessage), message);
                }
            }
        }
    }

    /// <summary>
    /// 处理消息处理器执行过程中的异常
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="messageType">消息类型</param>
    /// <param name="message">消息实例</param>
    protected virtual void OnHandlerException(Exception exception, Type messageType, object message)
    {
        // 默认实现只是记录异常，子类可以覆盖此方法提供自定义异常处理
        System.Diagnostics.Debug.WriteLine($"MessageBus处理器异常: {exception.Message}, 消息类型: {messageType.Name}");
    }

    /// <summary>
    /// 订阅指定类型的消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="handler">消息处理器</param>
    /// <returns>订阅令牌，用于取消订阅</returns>
    public IDisposable Subscribe<TMessage>(Action<TMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var messageType = typeof(TMessage);
        var entry = Subscriptions.GetOrAdd(messageType, _ => new SubscriptionEntry());

        object? subscription = CreateSubscription(handler);
        entry.AddSubscription(subscription);

        return new SubscriptionToken(() => Unsubscribe(entry, subscription));
    }

    /// <summary>
    /// 取消订阅指定类型的消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="handler">消息处理器</param>
    /// <returns>是否成功取消订阅</returns>
    public bool Unsubscribe<TMessage>(Action<TMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var messageType = typeof(TMessage);

        if (!Subscriptions.TryGetValue(messageType, out var entry))
            return false;

        var subscriptionsToRemove = FindSubscriptionsToRemove(entry, handler);

        return subscriptionsToRemove.Any(subscription => entry.RemoveSubscription(subscription));
    }

    /// <summary>
    /// 取消指定的订阅
    /// </summary>
    private static void Unsubscribe(SubscriptionEntry entry, object subscription)
    {
        entry.RemoveSubscription(subscription);
    }

    /// <summary>
    /// 获取有效的消息处理器列表
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <returns>有效的处理器列表</returns>
    protected abstract List<Action<TMessage>> GetValidHandlers<TMessage>();

    /// <summary>
    /// 创建订阅对象
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="handler">消息处理器</param>
    /// <returns>订阅对象</returns>
    protected abstract object CreateSubscription<TMessage>(Action<TMessage> handler);

    /// <summary>
    /// 查找要移除的订阅
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="entry">订阅条目</param>
    /// <param name="handler">消息处理器</param>
    /// <returns>要移除的订阅列表</returns>
    protected abstract List<object> FindSubscriptionsToRemove<TMessage>(
        SubscriptionEntry entry,
        Action<TMessage> handler
    );

    /// <summary>
    /// 清除所有订阅
    /// </summary>
    public void ClearAllSubscriptions()
    {
        foreach (var entry in Subscriptions.Values)
        {
            entry.ClearSubscriptions();
        }

        Subscriptions.Clear();
    }

    /// <summary>
    /// 释放资源，清除所有订阅
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源的虚方法，允许子类重写
    /// </summary>
    /// <param name="disposing">是否正在释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearAllSubscriptions();
        }
    }

    /// <summary>
    /// 订阅令牌，用于取消订阅
    /// </summary>
    private sealed class SubscriptionToken(Action unsubscribeAction) : IDisposable
    {
        private readonly Action _unsubscribeAction =
            unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _unsubscribeAction();
            _disposed = true;
        }
    }

    /// <summary>
    /// 订阅条目，管理特定消息类型的所有订阅
    /// </summary>
    protected sealed class SubscriptionEntry
    {
        private readonly List<object> _subscribers = [];
        private readonly Lock _lock = new();

        public void AddSubscription(object subscription)
        {
            lock (_lock)
            {
                _subscribers.Add(subscription);
            }
        }

        public bool RemoveSubscription(object subscription)
        {
            lock (_lock)
            {
                return _subscribers.Remove(subscription);
            }
        }

        public List<object> GetSubscriptionsSnapshot()
        {
            lock (_lock)
            {
                return [.. _subscribers];
            }
        }

        public void ClearSubscriptions()
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }
        }
    }
}
