using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QwQ_Music.Services;

public static class LoggerService {
    public static readonly string SavePath = Environment.CurrentDirectory + "Logs";
    public static bool IsKeepOpen = false;

    public enum LogLevel {
        Off = -1,
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    public static LogLevel Level = LogLevel.Debug;

    // 当IsKeepFileOpen为true时，在此处保存文件流. false时暂存当前文件流
    private static FileStream? _fileStream;

    private static DateTime _currentDay = DateTime.Today;
    private static readonly string LogFile = $"{SavePath}/{_currentDay:MM-DD}.QwQLog";

    private static FileStream LogStream =>
        _fileStream ??= File.Exists(LogFile) ?
            new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read) :
            new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);

    private static async void LoggerBaseAsync(
        string status,
        string data,
        int? line,
        string? function,
        string? filename,
        bool retry = false) {
        try {
            await using var writer = new StreamWriter(
                IsKeepOpen ? LogStream :
                File.Exists(LogFile) ? new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.Read) :
                new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read));
            if (line is null || function is null || filename is null) {
                await writer.WriteLineAsync(
                    $"Error:Line Number '{line}' or Function '{function}' or Filename '{filename}' is null.");
            }

            await writer.WriteLineAsync(
                $"{DateTime.Today.TimeOfDay:HH:MM:SS.FF} [{status}] <{function}> at {filename}, line {line}: {data}");
        } catch (IOException ex) {
            //TODO
        } catch (ObjectDisposedException ex) {
            _fileStream?.DisposeAsync();
            _fileStream = null;
            if (!retry) LoggerBaseAsync(status, data, line, function, filename, true);
        } catch (InvalidOperationException ex) {
            //TODO
        } catch (Exception) {
            //TODO
        }
    }

    public static void Info(
        string message,
        [CallerLineNumber] int? line = null,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) =>
        LoggerBaseAsync("INFO", message, line, function, filename);

    public static void Warning(
        string message,
        [CallerLineNumber] int? line = null,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) =>
        LoggerBaseAsync("WARNING", message, line, function, filename);

    public static void Error(
        string message,
        [CallerLineNumber] int? line = null,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) =>
        LoggerBaseAsync("ERROR", message, line, function, filename);

    public static void Fatal(
        string message,
        [CallerLineNumber] int? line = null,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) =>
        LoggerBaseAsync("FATAL", message, line, function, filename);

    public static void Custom(
        string status,
        string message,
        [CallerLineNumber] int? line = null,
        [CallerMemberName] string? function = null,
        [CallerFilePath] string? filename = null) =>
        LoggerBaseAsync(status.ToUpper(), message, line, function, filename);
}