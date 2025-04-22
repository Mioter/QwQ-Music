namespace QwQ_Music.Utilities.MessageBus;

/// <summary>
/// 消息统计信息，用于记录消息处理的性能和错误数据
/// </summary>
public class MessageStats
{
    /// <summary>
    /// 消息名称
    /// </summary>
    public string MessageName { get; }
    
    /// <summary>
    /// 消息处理次数
    /// </summary>
    public int MessageCount { get; set; }
    
    /// <summary>
    /// 错误次数
    /// </summary>
    public int ErrorCount { get; set; }
    
    /// <summary>
    /// 处理器总数（累计）
    /// </summary>
    public int TotalHandlerCount { get; set; }
    
    /// <summary>
    /// 总处理时间（毫秒）
    /// </summary>
    public long TotalProcessingTime { get; set; }
    
    /// <summary>
    /// 最长处理时间（毫秒）
    /// </summary>
    public long MaxProcessingTime { get; set; }
    
    /// <summary>
    /// 平均处理时间（毫秒）
    /// </summary>
    public double AverageProcessingTime => MessageCount > 0 
        ? (double)TotalProcessingTime / MessageCount 
        : 0;
    
    /// <summary>
    /// 平均每条消息的处理器数量
    /// </summary>
    public double AverageHandlersPerMessage => MessageCount > 0 
        ? (double)TotalHandlerCount / MessageCount 
        : 0;
    
    /// <summary>
    /// 创建消息统计信息实例
    /// </summary>
    /// <param name="messageName">消息名称</param>
    public MessageStats(string messageName)
    {
        MessageName = messageName;
    }
    
    /// <summary>
    /// 重置统计信息
    /// </summary>
    public void Reset()
    {
        MessageCount = 0;
        ErrorCount = 0;
        TotalHandlerCount = 0;
        TotalProcessingTime = 0;
        MaxProcessingTime = 0;
    }
}
