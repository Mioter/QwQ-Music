using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QwQ_Music.Utilities;

namespace QwQ_Music.Services;

public static class LoggerService
{
    // 配置项
    public static readonly string SavePath = EnsureExists.Path(Path.Combine(Environment.CurrentDirectory, "log"));
    public static bool IsKeepOpen { get; set; } = false;
    public static LogLevel Level { get; set; } = LogLevel.Debug;
    public static int RetryCount { get; set; } = 3;

    // 日志级别枚举
    public enum LogLevel
    {
        Off = -1,
        Debug,
        Info,
        Warning,
        Error,
        Fatal,
    }

    // 内部状态
    private static DateTime _currentDay = DateTime.Today;
    private static string LogFile => Path.Combine(SavePath, $"{_currentDay:yyyy-MM-dd}.QwQ.log");
    private static FileStream? _fileStream;
    private static readonly SemaphoreSlim AsyncLock = new(1, 1);

    /// <summary>
    /// 获取或创建日志文件流
    /// </summary>
    private static FileStream GetLogFile()
    {
        if (_fileStream is { CanWrite: true })
            return _fileStream;

        _fileStream?.Dispose();
        _fileStream = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
        return _fileStream;
    }

    /// <summary>
    /// 格式化日志消息
    /// </summary>
    private static string FormatLogMessage(string status, string message, int line, string? function, string? filename)
    {
        string lineInfo = line > 0 ? $"line {line}" : "unknown line";
        string functionInfo = !string.IsNullOrEmpty(function) ? $"<{function}>" : "<unknown>";
        string fileInfo = !string.IsNullOrEmpty(filename) ? Path.GetFileName(filename) : "unknown";

        return $"{DateTime.Now:HH:mm:ss.fff} [{status}] {functionInfo} at {fileInfo}, {lineInfo}: {message}";
    }

    /// <summary>
    /// 异步写入日志，支持重试机制
    /// </summary>
    private async static Task WriteLogAsync(string logMessage)
    {
        await AsyncLock.WaitAsync();
        try
        {
            // 检查是否需要切换日志文件
            var today = DateTime.Today;
            if (today != _currentDay)
            {
                _currentDay = today;
                await (_fileStream?.DisposeAsync() ?? ValueTask.CompletedTask); // 修复 CA2012
                _fileStream = null;
            }
            await AttemptWriteWithRetry(logMessage);
        }
        finally
        {
            AsyncLock.Release();
        }
    }

    /// <summary>
    /// 尝试写入日志，支持重试机制
    /// </summary>
    private async static Task AttemptWriteWithRetry(string logMessage)
    {
        int attempts = 0;
        while (attempts < RetryCount)
        {
            try
            {
                await using var writer = CreateStreamWriter();
                await writer.WriteLineAsync(logMessage);
                return;
            }
            catch
            {
                attempts++;
                if (attempts >= RetryCount)
                    throw;
                await Task.Delay(100);
            }
        }
    }

    /// <summary>
    /// 创建 StreamWriter
    /// </summary>
    private static StreamWriter CreateStreamWriter()
    {
        var stream = IsKeepOpen
            ? GetLogFile()
            : new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
        return new StreamWriter(stream, Encoding.UTF8, leaveOpen: IsKeepOpen);
    }

    /// <summary>
    /// 记录日志
    /// </summary>
    private static void Log(LogLevel level, string status, string message, int line, string? function, string? filename)
    {
        if (level < Level)
            return;

        string logMessage = FormatLogMessage(status, message, line, function, filename);
        _ = WriteLogAsync(logMessage);
    }

    /// <summary>
    /// 关闭日志服务，释放资源
    /// </summary>
    public static void Shutdown()
    {
        _fileStream?.Dispose();
        _fileStream = null;
    }

    // 公共日志方法
    public static void Info(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => Log(LogLevel.Info, "INFO", message, line, function, filename);

    public static void Warning(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => Log(LogLevel.Warning, "WARN", message, line, function, filename);

    public static void Error(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => Log(LogLevel.Error, "ERROR", message, line, function, filename);

    public static void Fatal(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => Log(LogLevel.Fatal, "FATAL", message, line, function, filename);

    public static void Custom(
        string message,
        string status,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => Log(LogLevel.Fatal, status.ToUpper(), message, line, function, filename);
}
