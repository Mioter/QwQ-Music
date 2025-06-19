using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Services;

#if DEBUG
using System.Net.Http;

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

/// <summary>
/// 日志级别枚举
/// </summary>
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

public static class LoggerService
{
    // 常量定义
    private const int BASE_RETRY_DELAY_MS = 100;
    private const int FILE_SHARING_RETRY_DELAY_MS = 200;

    // 配置项
    public static readonly string SavePath = MainConfig.LogSavePath;

    private static readonly LoggerServiceConfig _loggerServiceConfig = ConfigManager.LoggerServiceConfig;
    public static bool IsKeepOpen => _loggerServiceConfig.IsKeepOpen;
    public static int RetryCount => _loggerServiceConfig.RetryCount;
    public static LogLevel Level => _loggerServiceConfig.Level;

    // 内部状态
    private static DateTime currentDay = DateTime.Today;
    private static string LogFile => Path.Combine(SavePath, $"{currentDay:yyyy-MM-dd}.QwQ.log");
    private static string FallbackLogFile => Path.Combine(Path.GetTempPath(), $"QwQ_Music_{currentDay:yyyy-MM-dd}.log");
    private static FileStream? fileStream;
    private static readonly SemaphoreSlim _asyncLock = new(1, 1);
    private static bool useFallbackPath;
    private static bool isDisposed;

#if DEBUG
    private static readonly HttpClient _httpClient = new();
#endif

    /// <summary>
    /// 获取当前日志文件路径
    /// </summary>
    private static string GetCurrentLogPath()
    {
        return useFallbackPath ? FallbackLogFile : LogFile;
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private static bool EnsureDirectoryExists(string path)
    {
        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 创建文件流，支持备用路径
    /// </summary>
    private static FileStream CreateFileStream(string path, bool isFallback = false)
    {
        try
        {
            if (EnsureDirectoryExists(path))
            {
                // 使用更宽松的文件共享模式，允许其他进程读取和写入
                return new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            }

            if (!isFallback)
            {
                // 尝试备用路径
                return CreateFileStream(FallbackLogFile, true);
            }
            throw new DirectoryNotFoundException($"无法创建目录: {Path.GetDirectoryName(path)}");
        }
        catch (UnauthorizedAccessException)
        {
            if (isFallback)
                throw;

            // 权限不足，尝试备用路径
            useFallbackPath = true;
            Console.WriteLine($"警告: 无法写入日志目录 {SavePath}，权限不足。将使用备用路径: {FallbackLogFile}");
            return CreateFileStream(FallbackLogFile, true);
        }
        catch (IOException ex) when (ex.HResult == -2147024864) // ERROR_SHARING_VIOLATION
        {
            if (isFallback)
                throw;

            // 文件被占用，尝试备用路径
            Console.WriteLine($"警告: 日志文件被占用，将使用备用路径: {FallbackLogFile}");
            return CreateFileStream(FallbackLogFile, true);
        }
    }

    /// <summary>
    /// 获取或创建日志文件流
    /// </summary>
    private static FileStream GetLogFile()
    {
        ObjectDisposedException.ThrowIf(isDisposed, nameof(LoggerService));

        if (fileStream is { CanWrite: true })
            return fileStream;

        try
        {
            fileStream?.Dispose();
            fileStream = null;

            string logPath = GetCurrentLogPath();
            fileStream = CreateFileStream(logPath, useFallbackPath);
            return fileStream;
        }
        catch (IOException ex) when (ex.HResult == -2147024864) // ERROR_SHARING_VIOLATION
        {
            // 如果主路径文件被占用，尝试切换到备用路径
            if (useFallbackPath)
                throw; // 如果已经是备用路径还失败，则抛出异常

            useFallbackPath = true;
            Console.WriteLine($"主日志文件被占用，切换到备用路径: {FallbackLogFile}");
            return GetLogFile(); // 递归调用
        }
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
        if (isDisposed)
            return;

        await _asyncLock.WaitAsync();
        try
        {
            // 检查是否需要切换日志文件
            var today = DateTime.Today;
            if (today != currentDay)
            {
                currentDay = today;
                await (fileStream?.DisposeAsync() ?? ValueTask.CompletedTask);
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
        Exception? lastException = null;

        while (attempts < RetryCount && !isDisposed)
        {
            try
            {
                await using var writer = CreateStreamWriter();
                await writer.WriteLineAsync(logMessage);
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                lastException = ex;
                attempts++;

                // 权限不足时，立即切换到备用路径
                if (!useFallbackPath)
                {
                    useFallbackPath = true;
                    Console.WriteLine($"权限不足，切换到备用日志路径: {FallbackLogFile}");
                    attempts = 0; // 重置重试次数
                    continue;
                }

                if (attempts < RetryCount)
                    continue;

                LogErrorToConsole("无法写入日志文件，权限不足", ex);
                return;
            }
            catch (IOException ex) when (ex.HResult == -2147024891) // ERROR_ACCESS_DENIED
            {
                lastException = ex;
                attempts++;

                if (attempts >= RetryCount)
                {
                    LogErrorToConsole("文件访问被拒绝", ex);
                    return;
                }

                await Task.Delay(BASE_RETRY_DELAY_MS * attempts);
            }
            catch (IOException ex) when (ex.HResult == -2147024864) // ERROR_SHARING_VIOLATION
            {
                lastException = ex;
                attempts++;

                // 文件被占用时，尝试切换到备用路径
                if (!useFallbackPath)
                {
                    useFallbackPath = true;
                    Console.WriteLine($"主日志文件被占用，切换到备用路径: {FallbackLogFile}");
                    attempts = 0; // 重置重试次数
                    continue;
                }

                if (attempts >= RetryCount)
                {
                    LogErrorToConsole("文件被其他进程占用", ex);
                    return;
                }

                await Task.Delay(FILE_SHARING_RETRY_DELAY_MS * attempts);
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempts++;

                if (attempts >= RetryCount)
                {
                    LogErrorToConsole("写入日志文件失败", ex);
                    return;
                }

                await Task.Delay(BASE_RETRY_DELAY_MS);
            }
        }

        // 如果所有重试都失败，输出最后一次异常信息
        if (lastException != null)
        {
            Console.WriteLine(
                $"所有重试都失败，最后一次异常: {lastException.GetType().Name} - {lastException.Message}"
            );
        }
    }

    /// <summary>
    /// 输出错误信息到控制台
    /// </summary>
    private static void LogErrorToConsole(string message, Exception ex)
    {
        Console.WriteLine($"严重错误: {message}。路径: {GetCurrentLogPath()}");
        Console.WriteLine($"错误详情: {ex.Message}");
    }

    /// <summary>
    /// 创建 StreamWriter
    /// </summary>
    private static StreamWriter CreateStreamWriter()
    {
        ObjectDisposedException.ThrowIf(isDisposed, nameof(LoggerService));

        string logPath = GetCurrentLogPath();

        try
        {
            var stream = IsKeepOpen ? GetLogFile() : CreateFileStream(logPath, useFallbackPath);
            return new StreamWriter(stream, Encoding.UTF8, leaveOpen: IsKeepOpen);
        }
        catch (UnauthorizedAccessException)
        {
            // 如果主路径权限不足，尝试备用路径
            if (useFallbackPath)
                throw;

            useFallbackPath = true;
            Console.WriteLine($"权限不足，切换到备用日志路径: {FallbackLogFile}");
            return CreateStreamWriter(); // 递归调用
        }
        catch (IOException ex) when (ex.HResult == -2147024864) // ERROR_SHARING_VIOLATION
        {
            // 文件被占用，尝试备用路径
            if (useFallbackPath)
                throw;

            useFallbackPath = true;
            Console.WriteLine($"主日志文件被占用，切换到备用路径: {FallbackLogFile}");
            return CreateStreamWriter(); // 递归调用
        }
    }

    /// <summary>
    /// 记录日志
    /// </summary>
    private static void Log(LogLevel level, string status, string message, int line, string? function, string? filename)
    {
        if (level < Level || isDisposed)
            return;

        string logMessage = FormatLogMessage(status, message, line, function, filename);
        _ = WriteLogAsync(logMessage);

#if DEBUG
        /*_ = SendLog(status, message, line, function, filename);*/
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
        if (level < Level || isDisposed)
            return Task.CompletedTask;

        string logMessage = FormatLogMessage(status, message, line, function, filename);
#if DEBUG
        /*SendLog(status, message, line, function, filename).ConfigureAwait(false);*/
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
    /// 获取当前日志服务状态信息
    /// </summary>
    public static string GetLogStatus()
    {
        if (isDisposed)
            return "日志服务已关闭";

        string currentPath = GetCurrentLogPath();
        var status = new StringBuilder();
        status.AppendLine($"日志级别: {Level}");
        status.AppendLine($"重试次数: {RetryCount}");
        status.AppendLine($"保持连接: {IsKeepOpen}");
        status.AppendLine($"当前日志文件: {currentPath}");
        status.AppendLine($"主日志目录: {SavePath}");
        status.AppendLine($"备用日志目录: {Path.GetTempPath()}");
        status.AppendLine($"使用备用路径: {useFallbackPath}");
        status.AppendLine($"文件流状态: {(fileStream?.CanWrite == true ? "正常" : "未打开或已关闭")}");
        status.AppendLine($"服务状态: {(isDisposed ? "已关闭" : "运行中")}");

        return status.ToString();
    }

    /// <summary>
    /// 重置日志服务状态，强制重新检查路径
    /// </summary>
    public static void ResetLogService()
    {
        if (isDisposed)
            return;

        useFallbackPath = false;

        // 关闭当前文件流
        try
        {
            fileStream?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"关闭文件流时发生异常: {ex.Message}");
        }
        finally
        {
            fileStream = null;
        }

        Info("日志服务状态已重置，将重新检查路径");
    }

    /// <summary>
    /// 强制切换到备用日志路径
    /// </summary>
    public static void ForceUseFallbackPath()
    {
        if (isDisposed)
            return;

        useFallbackPath = true;

        // 关闭当前文件流
        try
        {
            fileStream?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"关闭文件流时发生异常: {ex.Message}");
        }
        finally
        {
            fileStream = null;
        }

        Info($"已强制切换到备用日志路径: {FallbackLogFile}");
    }

    /// <summary>
    /// 关闭日志服务，释放资源
    /// </summary>
    public static void Shutdown()
    {
        if (isDisposed)
            return;

        isDisposed = true;
        fileStream?.Dispose();
        fileStream = null;
        Info("日志服务已退出~");
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
