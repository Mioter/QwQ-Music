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

public class DataBaseConfig
{
    /// <summary>
    ///     是否启用详细日志记录
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = true;

    /// <summary>
    ///     是否启用性能监控
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    ///     慢查询阈值（毫秒）
    /// </summary>
    public int SlowQueryThreshold { get; set; } = 500;

    /// <summary>
    ///     命令超时时间（秒）
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    ///     最大重试次数
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    ///     重试延迟（毫秒）
    /// </summary>
    public int RetryDelay { get; set; } = 100;

    /// <summary>
    ///     连接池大小
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 10;
}
