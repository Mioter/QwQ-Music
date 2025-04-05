using System;

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
    void Publish<TMessage>(TMessage message);

    /// <summary>
    /// 订阅指定类型的消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="handler">消息处理器</param>
    /// <returns>订阅令牌，用于取消订阅</returns>
    IDisposable Subscribe<TMessage>(Action<TMessage> handler);

    /// <summary>
    /// 取消订阅指定类型的消息
    /// </summary>
    /// <typeparam name="TMessage">消息类型</typeparam>
    /// <param name="handler">消息处理器</param>
    /// <returns>是否成功取消订阅</returns>
    bool Unsubscribe<TMessage>(Action<TMessage> handler);
}
