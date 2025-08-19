using QwQ_Music.Common.Services;

namespace QwQ_Music.Models.ConfigModels;

public class JsonServiceConfig
{
    /// <summary>
    ///     启用备份
    /// </summary>
    public bool EnableBackup { get; set; }

    /// <summary>
    ///     启用性能监控
    /// </summary>
    public bool EnablePerformanceLogging { get; set; } = true;

    /// <summary>
    ///     最大备份次数
    /// </summary>
    public int MaxBackupCount { get; set; } = 3;
}

public class LoggerServiceConfig
{
    /// <summary>
    ///     保存文件打开
    /// </summary>
    public bool IsKeepOpen { get; set; } = true;

    /// <summary>
    ///     重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    ///     日志过滤级别
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Info;
}
