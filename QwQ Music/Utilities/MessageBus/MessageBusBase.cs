using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 消息总线基类，提供通用的发布/订阅逻辑。
/// </summary>
public abstract class MessageBusBase : IDisposable
{
    /// <summary>
    /// 存储所有消息类型及其对应订阅项的字典。
    /// </summary>
    protected readonly ConcurrentDictionary<Type, SubscriptionEntry> _subscriptions = new();

    /// <summary>
    /// 当发生异常时触发的事件。
    /// </summary>
    public event EventHandler<Exception>? OnException;

    /// <summary>
    /// 释放资源并清理所有订阅。
    /// </summary>
    public void Dispose()
    {
        foreach (var entry in _subscriptions.Values)
        {
            lock (entry.Lock)
            {
                entry.Subscribers.Clear();
            }
        }
        _subscriptions.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 订阅指定消息类型的处理程序。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <param name="handler">处理程序。</param>
    /// <returns>一个可释放的对象，用于取消订阅。</returns>
    public IDisposable Subscribe<TMessage>(Action<TMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var messageType = typeof(TMessage);
        var entry = _subscriptions.GetOrAdd(messageType, _ => new SubscriptionEntry());
        object? subscription = CreateSubscription(handler);
        lock (entry.Lock)
        {
            entry.Subscribers.Add(subscription);
        }
        return new SubscriptionToken(() => Unsubscribe(handler, entry, subscription));
    }

    /// <summary>
    /// 发布指定的消息（异步）。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <param name="message">消息对象。</param>
    public async Task PublishAsync<TMessage>(TMessage message)
    {
        var tasks = GetValidHandlers<TMessage>()
            .Select(handler => Task.Run(() => SafeInvokeHandler(handler, message)))
            .ToList();
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 发布指定的消息（同步）。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <param name="message">消息对象。</param>
    public void Publish<TMessage>(TMessage message)
    {
        foreach (var handler in GetValidHandlers<TMessage>())
        {
            SafeInvokeHandler(handler, message);
        }
    }

    /// <summary>
    /// 安全地调用处理程序并捕获异常。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <param name="handler">处理程序。</param>
    /// <param name="message">消息对象。</param>
    private void SafeInvokeHandler<TMessage>(Action<TMessage> handler, TMessage message)
    {
        try
        {
            handler(message);
        }
        catch (Exception ex)
        {
            OnException?.Invoke(this, ex);
        }
    }

    /// <summary>
    /// 获取有效的处理程序列表。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <returns>有效的处理程序列表。</returns>
    protected abstract List<Action<TMessage>> GetValidHandlers<TMessage>();

    /// <summary>
    /// 创建订阅项。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <param name="handler">处理程序。</param>
    /// <returns>订阅项。</returns>
    protected abstract object CreateSubscription<TMessage>(Action<TMessage> handler);

    /// <summary>
    /// 取消订阅指定消息类型的处理程序。
    /// </summary>
    /// <typeparam name="TMessage">消息类型。</typeparam>
    /// <param name="handler">处理程序。</param>
    /// <param name="entry">订阅项。</param>
    /// <param name="subscription">订阅项。</param>
    private static void Unsubscribe<TMessage>(Action<TMessage> handler, SubscriptionEntry entry, object subscription)
    {
        lock (entry.Lock)
        {
            entry.Subscribers.Remove(subscription);
        }
    }

    /// <summary>
    /// 用于取消订阅的令牌类。
    /// </summary>
    private class SubscriptionToken(Action unsubscribeAction) : IDisposable
    {
        private readonly Action _unsubscribeAction = unsubscribeAction ?? throw new ArgumentNullException(nameof(unsubscribeAction));

        public void Dispose()
        {
            _unsubscribeAction();
        }
    }

    /// <summary>
    /// 订阅项类，包含订阅者的列表和锁对象。
    /// </summary>
    protected class SubscriptionEntry
    {
        public List<object> Subscribers { get; } = [];
        public object Lock { get; } = new();
    }
}