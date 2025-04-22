using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using QwQ_Music.Services;

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
    /// 消息处理统计信息
    /// </summary>
    protected readonly Dictionary<Type, MessageStats> MessageStats = new();
    
    /// <summary>
    /// 是否启用性能监控
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
    
    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;
    
    /// <summary>
    /// 慢消息处理阈值（毫秒）
    /// </summary>
    public int SlowMessageThreshold { get; set; } = 50;
    
    /// <summary>
    /// 总线名称，用于日志标识
    /// </summary>
    protected abstract string BusName { get; }

    /// <summary>
    /// 发布消息到总线
    /// </summary>
    public virtual IMessageBus Publish<TMessage>(TMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = typeof(TMessage);
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
        
        try
        {
            if (EnableVerboseLogging)
            {
                LoggerService.Debug($"[{BusName}] 发布消息: {messageType.Name}");
            }
            
            var handlers = GetValidHandlers<TMessage>();
            
            if (EnableVerboseLogging)
            {
                LoggerService.Debug($"[{BusName}] 开始处理消息 {messageType.Name}, 订阅者数量: {handlers.Count}");
            }
            
            // 执行所有处理器
            foreach (var handler in handlers)
            {
                try
                {
                    handler(message);
                }
                catch (Exception ex)
                {
                    OnHandlerException(ex, messageType);
                }
            }
            
            if (EnablePerformanceMonitoring && stopwatch != null)
            {
                stopwatch.Stop();
                var elapsed = stopwatch.ElapsedMilliseconds;
                
                if (elapsed > SlowMessageThreshold)
                {
                    LoggerService.Warning($"[{BusName}] 慢消息处理: {messageType.Name}, 耗时: {elapsed}ms, 处理器数量: {handlers.Count}");
                }
                
                UpdateMessageStats(messageType, handlers.Count, elapsed);
            }
        }
        catch (Exception ex)
        {
            LoggerService.Error($"[{BusName}] 发布消息时发生错误: {ex.Message}");
            LoggerService.Debug($"[{BusName}] 异常详情: {ex}");
        }
        finally
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
                if (EnableVerboseLogging)
                {
                    LoggerService.Debug($"[{BusName}] 消息 {messageType.Name} 处理完成, 总耗时: {stopwatch.ElapsedMilliseconds}ms");
                }
            }
        }
        
        return this;
    }

    /// <summary>
    /// 异步发布消息到总线
    /// </summary>
    public virtual async Task<IMessageBus> PublishAsync<TMessage>(TMessage message)
    {
        await Task.Run(() => Publish(message));
        return this;
    }

    /// <summary>
    /// 订阅指定类型的消息
    /// </summary>
    public virtual IDisposable? Subscribe<TMessage>(Action<TMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var messageType = typeof(TMessage);
        
        if (EnableVerboseLogging)
        {
            LoggerService.Debug($"[{BusName}] 订阅消息: {messageType.Name}, 处理器: {handler.Method.Name}");
        }
        
        var entry = Subscriptions.GetOrAdd(messageType, _ => new SubscriptionEntry());

        object subscription = CreateSubscription(handler);
        entry.AddSubscription(subscription);

        return new SubscriptionToken(() => Unsubscribe(entry, subscription));
    }

    /// <summary>
    /// 取消订阅指定类型的消息
    /// </summary>
    public virtual IMessageBus Unsubscribe<TMessage>(Action<TMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var messageType = typeof(TMessage);
        
        if (EnableVerboseLogging)
        {
            LoggerService.Debug($"[{BusName}] 取消订阅: {messageType.Name}, 处理器: {handler.Method.Name}");
        }

        if (Subscriptions.TryGetValue(messageType, out var entry))
        {
            var subscriptionsToRemove = FindSubscriptionsToRemove(entry, handler);
            foreach (var subscription in subscriptionsToRemove)
            {
                entry.RemoveSubscription(subscription);
            }
        }
        
        return this;
    }
    
    /// <summary>
    /// 处理消息处理器执行过程中的异常
    /// </summary>
    protected virtual void OnHandlerException(Exception exception, Type messageType)
    {
        LoggerService.Error($"[{BusName}] 处理消息 {messageType.Name} 时发生异常: {exception.Message}");
        LoggerService.Debug($"[{BusName}] 异常详情: {exception}");
        
        // 更新异常统计
        if (EnablePerformanceMonitoring)
        {
            lock (MessageStats)
            {
                if (!MessageStats.TryGetValue(messageType, out var stats))
                {
                    stats = new MessageStats(messageType.Name);
                    MessageStats[messageType] = stats;
                }
                
                stats.ErrorCount++;
            }
        }
    }
    
    /// <summary>
    /// 更新消息统计信息
    /// </summary>
    protected void UpdateMessageStats(Type messageType, int handlerCount, long elapsedMilliseconds)
    {
        if (!EnablePerformanceMonitoring)
            return;
            
        lock (MessageStats)
        {
            if (!MessageStats.TryGetValue(messageType, out var stats))
            {
                stats = new MessageStats(messageType.Name);
                MessageStats[messageType] = stats;
            }
            
            stats.MessageCount++;
            stats.TotalHandlerCount += handlerCount;
            stats.TotalProcessingTime += elapsedMilliseconds;
            
            if (elapsedMilliseconds > stats.MaxProcessingTime)
            {
                stats.MaxProcessingTime = elapsedMilliseconds;
            }
        }
    }
    
    /// <summary>
    /// 获取消息统计信息
    /// </summary>
    public Dictionary<string, MessageStats> GetMessageStats()
    {
        lock (MessageStats)
        {
            return MessageStats.ToDictionary(kv => kv.Key.Name, kv => kv.Value);
        }
    }
    
    /// <summary>
    /// 清除统计信息
    /// </summary>
    public void ClearStats()
    {
        lock (MessageStats)
        {
            MessageStats.Clear();
        }
        
        LoggerService.Info($"[{BusName}] 统计信息已清除");
    }
    
    /// <summary>
    /// 取消订阅
    /// </summary>
    protected static void Unsubscribe(SubscriptionEntry entry, object subscription)
    {
        entry.RemoveSubscription(subscription);
    }

    /// <summary>
    /// 创建订阅对象
    /// </summary>
    protected abstract object CreateSubscription<TMessage>(Action<TMessage> handler);

    /// <summary>
    /// 获取有效的消息处理器列表
    /// </summary>
    protected abstract List<Action<TMessage>> GetValidHandlers<TMessage>();

    /// <summary>
    /// 查找要移除的订阅
    /// </summary>
    protected abstract List<object> FindSubscriptionsToRemove<TMessage>(
        SubscriptionEntry entry,
        Action<TMessage> handler
    );
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public virtual void Dispose()
    {
        Subscriptions.Clear();
        MessageStats.Clear();
        GC.SuppressFinalize(this);
    }
    
    
    /// <summary>
    ///     订阅令牌，用于取消订阅
    /// </summary>
    private class SubscriptionToken(Action unsubscribeAction) : IDisposable
    {
        private bool _isDisposed;

        public void Dispose()
        {
            if (_isDisposed)
                return;

            unsubscribeAction();
            _isDisposed = true;
        }
    }
}