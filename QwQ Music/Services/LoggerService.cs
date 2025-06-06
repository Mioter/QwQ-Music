﻿using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using QwQ_Music.Utilities;

namespace QwQ_Music.Services;

#if DEBUG
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(LogData))]
internal partial class LoggerJsonContext : JsonSerializerContext;

internal record LogData(
    string Timestamp,
    string Level,
    string? Action,
    string? Filename,
    int LineNumber,
    string Message
);
#endif

public static class LoggerService
{
    // 配置项
    public static readonly string SavePath = PathEnsurer.EnsureDirectoryExists(
        Path.Combine(AppContext.BaseDirectory, "logs")
    );
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
        Custom,
    }

    // 内部状态
    private static DateTime currentDay = DateTime.Today;
    private static string LogFile => Path.Combine(SavePath, $"{currentDay:yyyy-MM-dd}.QwQ.log");
    private static FileStream? fileStream;
    private static readonly SemaphoreSlim _asyncLock = new(1, 1);
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// 获取或创建日志文件流
    /// </summary>
    private static FileStream GetLogFile()
    {
        if (fileStream is { CanWrite: true })
            return fileStream;

        fileStream?.Dispose();
        fileStream = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
        return fileStream;
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
    private static async Task WriteLogAsync(string logMessage)
    {
        await _asyncLock.WaitAsync();
        try
        {
            // 检查是否需要切换日志文件
            var today = DateTime.Today;
            if (today != currentDay)
            {
                currentDay = today;
                await (fileStream?.DisposeAsync() ?? ValueTask.CompletedTask); // 修复 CA2012
                fileStream = null;
            }
            await AttemptWriteWithRetry(logMessage);
        }
        finally
        {
            _asyncLock.Release();
        }
    }

    /// <summary>
    /// 尝试写入日志，支持重试机制
    /// </summary>
    private static async Task AttemptWriteWithRetry(string logMessage)
    {
        int attempts = 0;
        while (attempts < RetryCount)
        {
            try
            {
                await using var writer = CreateStreamWriter();
                await writer.WriteLineAsync(logMessage);
                /*await writer.FlushAsync();*/
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

#if DEBUG
        _ = SendLog(status, message, line, function, filename);
#endif
    }

    /// <summary>
    /// 异步记录日志
    /// </summary>
    private static Task LogAsync(
        LogLevel level,
        string status,
        string message,
        int line,
        string? function,
        string? filename
    )
    {
        if (level < Level)
            return Task.CompletedTask;

        string logMessage = FormatLogMessage(status, message, line, function, filename);
#if DEBUG
        SendLog(status, message, line, function, filename).ConfigureAwait(false);
#endif
        return WriteLogAsync(logMessage);
    }

#if DEBUG
    /// <summary>
    /// 发送日志到本地服务器
    /// </summary>
    private static async Task SendLog(string status, string message, int lineNumber, string? function, string? filename)
    {
        var logData = new LogData(
            DateTime.Now.ToString("HH:mm:ss.fff"),
            status,
            function,
            filename,
            lineNumber,
            message
        );

        try
        {
            string postData = JsonSerializer.Serialize(logData, LoggerJsonContext.Default.LogData);
            var content = new StringContent(postData, Encoding.UTF8, "application/json");

            // 使用超时防止阻塞
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var response = await _httpClient.PostAsync("http://localhost:8081/log", content, cts.Token);

            // 不再输出状态码到控制台，避免过多输出
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"日志服务器响应错误: {response.StatusCode}");
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            // 静默处理网络错误，避免影响应用性能
        }
        catch (Exception e)
        {
            Console.WriteLine($"发送日志时发生未预期错误: {e.Message}");
        }
    }
#endif

    /// <summary>
    /// 关闭日志服务，释放资源
    /// </summary>
    public static void Shutdown()
    {
        fileStream?.Dispose();
        Info("日志服务已退出~");
        fileStream = null;
    }

    // 公共日志方法 - 同步版本
    public static void Debug(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => Log(LogLevel.Debug, "DEBUG", message, line, function, filename);

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
    ) => Log(LogLevel.Custom, status.ToUpper(), message, line, function, filename);

    // 公共日志方法 - 异步版本
    public static Task DebugAsync(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => LogAsync(LogLevel.Debug, "DEBUG", message, line, function, filename);

    public static Task InfoAsync(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => LogAsync(LogLevel.Info, "INFO", message, line, function, filename);

    public static Task WarningAsync(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => LogAsync(LogLevel.Warning, "WARN", message, line, function, filename);

    public static Task ErrorAsync(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => LogAsync(LogLevel.Error, "ERROR", message, line, function, filename);

    public static Task FatalAsync(
        string message,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => LogAsync(LogLevel.Fatal, "FATAL", message, line, function, filename);

    public static Task CustomAsync(
        string message,
        string status,
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null
    ) => LogAsync(LogLevel.Custom, status.ToUpper(), message, line, function, filename);
}
