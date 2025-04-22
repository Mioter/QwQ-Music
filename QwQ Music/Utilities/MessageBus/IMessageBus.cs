using System;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 消息总线接口，定义消息发布和订阅的基本操作
/// </summary>
public interface IMessageBus : IDisposable
{
    /// <summary>
    /// 发布消息到总线
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="message">消息实例</param>
    /// <returns>消息总线实例，支持链式调用</returns>
    IMessageBus Publish<TMessage>(TMessage message);
    
    /// <summary>
    /// 异步发布消息到总线
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="message">消息实例</param>
    /// <returns>异步任务，完成后返回消息总线实例</returns>
    Task<IMessageBus> PublishAsync<TMessage>(TMessage message);

    /// <summary>
    /// 订阅指定类型的消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="handler">消息处理器</param>
    /// <returns>订阅令牌，用于取消订阅</returns>
    IDisposable? Subscribe<TMessage>(Action<TMessage> handler);

    /// <summary>
    /// 取消订阅指定类型的消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="handler">消息处理器</param>
    /// <returns>消息总线实例，支持链式调用</returns>
    IMessageBus Unsubscribe<TMessage>(Action<TMessage> handler);
}
