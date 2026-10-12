using QwQ_Music.Common.Services;

namespace QwQ_Music.Models.ConfigModels;

public class LoggerServiceConfig {
    /// <summary>
    ///     重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    ///     日志过滤级别
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Basic;
}