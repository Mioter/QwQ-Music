using System;
using System.Collections.Generic;
using System.Linq;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 弱引用消息总线类，使用弱引用来管理订阅者。
/// </summary>
public class WeakMessageBus : MessageBusBase
{
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
        if (!_subscriptions.TryGetValue(messageType, out var entry)) return [];
        lock (entry.Lock)
        {
            var validHandlers = entry.Subscribers
                .OfType<WeakReference>()
                .Where(weakRef => weakRef.IsAlive)
                .Select(weakRef => weakRef.Target as Action<TMessage>)
                .Where(handler => handler != null)
                .ToList();

#if DEBUG
            Console.WriteLine($"Valid handlers for {messageType.Name}: {validHandlers.Count}");
#endif

            // 清理无效订阅者
            entry.Subscribers.RemoveAll(weakRef => !((WeakReference)weakRef).IsAlive);
            return validHandlers!;
        }
    }
}
