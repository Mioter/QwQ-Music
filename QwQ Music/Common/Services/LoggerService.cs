using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using QwQ_Music.Common.Managers;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Common.Services;

[Flags]
public enum LogLevel {
    /// <summary>
    ///     关闭
    /// </summary>
    Off = 0,

    /// <summary>
    ///     调试
    /// </summary>
    Debug = 1,

    /// <summary>
    ///     信息
    /// </summary>
    Info = 1 << 2,

    /// <summary>
    ///     警告
    /// </summary>
    Warning = 1 << 3,

    /// <summary>
    ///     错误
    /// </summary>
    Error = 1 << 4,

    /// <summary>
    ///     崩溃
    /// </summary>
    Fatal = 1 << 5,

    /// <summary>
    ///     自定义
    /// </summary>
    Custom,

    /// <summary>
    ///     基础
    /// </summary>
    Basic = Fatal | Error | Warning,

    /// <summary>
    ///     详细
    /// </summary>
    Detail = Basic | Info,

    /// <summary>
    ///     完整
    /// </summary>
    All = Detail | Debug
}

public static class LoggerService {
    private const int _BASE_RETRY_DELAY_MS = 100;
    private const int _MAX_RETRY_COUNT = 3;

    private static readonly LoggerServiceConfig _config = ConfigManager.LoggerServiceConfig;
    private static readonly string _savePath = StaticConfig.LogSavePath;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private static DateTime _currentDay = DateTime.Today;
    private static bool _useFallbackPath;
    
    private static StreamWriter Writer {
        get {
            if (field?.BaseStream is { CanWrite: true })
                return field;

            try {
                string? dir = Path.GetDirectoryName(LogFilePath);

                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                field?.BaseStream?.Dispose();
                field?.Dispose();
                field = new StreamWriter(
                    new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite),
                    Encoding.UTF8,
                    1024,
                    false);
                return field;
            } catch (Exception ex) when (ex is UnauthorizedAccessException or IOException) {
                if (_useFallbackPath)
                    throw;

                _useFallbackPath = true;
                Console.WriteLine($"[Logger] 切换至备用路径: {LogFilePath}");

                return Writer;
            }
        }
    }

    private static string LogFilePath =>
        Path.Combine(_useFallbackPath ? Path.GetTempPath() : _savePath, $"{_currentDay:yyyy-MM-dd}.QwQ.log");

    public static LogLevel Level => _config.Level;

    public static int RetryCount => Math.Min(_config.RetryCount, _MAX_RETRY_COUNT);

    private static string FormatMessage(string level, string msg, int line, string? func, string? file) {
        return $"{DateTime.Now:HH:mm:ss.fff} [{level}] <{func ?? "unknown"}> at {Path.GetFileName(file) ?? "unknown"
        }, line {line}: {msg}";
    }

    private static async Task WriteLogAsync(string formattedMessage) {
#if DEBUG
        Console.WriteLine(formattedMessage);
#endif

        await _semaphore.WaitAsync().ConfigureAwait(false);

        try {
            if (DateTime.Today != _currentDay) {
                _currentDay = DateTime.Today;

                await Writer.DisposeAsync().ConfigureAwait(false);
            }

            int attempts = 0;

            while (attempts < RetryCount)
                try {
                    await Writer.WriteLineAsync(formattedMessage).ConfigureAwait(false);

                    return;
                } catch (IOException ex) when
                    (ex.HResult is -2147024864 or -2147024891) // Sharing violation or Access denied
                {
                    attempts++;

                    if (attempts >= RetryCount)
                        throw;

                    await Task.Delay(_BASE_RETRY_DELAY_MS * attempts).ConfigureAwait(false);
                }
        } finally {
            _semaphore.Release();
        }
    }

    private static void Log(
        LogLevel level,
        string status,
        string message,
        int line = 0,
        string? function = null,
        string? filename = null) {
        LogAsync(level, status, message, line, function, filename).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private static async Task LogAsync(
        LogLevel level,
        string status,
        string message,
        int line = 0,
        string? function = null,
        string? filename = null) {
        if (!Level.HasFlag(level))
            return;

        string formatted = FormatMessage(status, message, line, function, filename);

        await WriteLogAsync(formatted).ConfigureAwait(false);
    }

    [Conditional("DEBUG")]
    public static void Debug(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        Log(LogLevel.Debug, "DEBUG", message, line, function, filename);
    }

    public static void Info(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        Log(LogLevel.Info, "INFO", message, line, function, filename);
    }

    public static void Warning(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        Log(LogLevel.Warning, "WARN", message, line, function, filename);
    }

    public static void Error(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        Log(LogLevel.Error, "ERROR", message, line, function, filename);
    }

    public static void Error(
        string message,
        Exception ex,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        Log(
            LogLevel.Error,
            "ERROR",
            message + $"\n{ex.GetType()}: {ex.Message}\n{ex.StackTrace}",
            line,
            function,
            filename);
    }

    public static void Fatal(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        Log(LogLevel.Fatal, "FATAL", message, line, function, filename);
    }

    public static void Custom(
        string message,
        string status,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        Log(LogLevel.Custom, status.ToUpper(), message, line, function, filename);
    }

    // 异步版本
    public static async Task DebugAsync(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        await LogAsync(LogLevel.Debug, "DEBUG", message, line, function, filename).ConfigureAwait(false);
    }

    public static async Task InfoAsync(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        await LogAsync(LogLevel.Info, "INFO", message, line, function, filename).ConfigureAwait(false);
    }

    public static async Task WarningAsync(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        await LogAsync(LogLevel.Warning, "WARN", message, line, function, filename).ConfigureAwait(false);
    }

    public static async Task ErrorAsync(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        await LogAsync(LogLevel.Error, "ERROR", message, line, function, filename).ConfigureAwait(false);
    }

    public static async Task ErrorAsync(
        string message,
        Exception ex,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        await LogAsync(
                LogLevel.Error,
                "ERROR",
                message + $"\n{ex.GetType()}: {ex.Message}\n{ex.StackTrace}",
                line,
                function,
                filename)
            .ConfigureAwait(false);
    }

    public static async Task FatalAsync(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        await LogAsync(LogLevel.Fatal, "FATAL", message, line, function, filename).ConfigureAwait(false);
    }

    public static async Task CustomAsync(
        string message,
        string status,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) {
        await LogAsync(LogLevel.Custom, status.ToUpper(), message, line, function, filename).ConfigureAwait(false);
    }

    [StackTraceHidden]
    public static void HandleException(Task withException) {
        if (withException.Exception is null)
            return;
        TryHandleAggregateException(withException.Exception);

        return;

        void TryHandleAggregateException(Exception exception) {
            if (exception is AggregateException aggregate) {
                foreach (Exception inner in aggregate.InnerExceptions)
                    TryHandleAggregateException(inner);

                return;
            }

            Error($"在Task执行期间发生{exception.GetType()}：{exception.Message}\n{exception.StackTrace}");
        }
    }

    public static async Task DisposeAsync() {
        _semaphore.Dispose();
        await Writer.DisposeAsync().ConfigureAwait(false);
    }
}