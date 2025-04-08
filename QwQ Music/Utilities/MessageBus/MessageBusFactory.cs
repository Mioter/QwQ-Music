namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 消息总线工厂，提供获取不同类型消息总线的方法
/// </summary>
public static class MessageBusFactory
{
    /// <summary>
    /// 获取默认的消息总线实例（弱引用）
    /// </summary>
    public static IMessageBus Default => WeakMessageBus.Instance;

    /// <summary>
    /// 获取强引用消息总线实例
    /// </summary>
    public static IMessageBus Strong => StrongMessageBus.Instance;

    /// <summary>
    /// 获取弱引用消息总线实例
    /// </summary>
    public static IMessageBus Weak => WeakMessageBus.Instance;
}
