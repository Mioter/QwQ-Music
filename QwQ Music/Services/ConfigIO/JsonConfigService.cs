using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Utilities;

namespace QwQ_Music.Services.ConfigIO;

/// <summary>
///     支持 AOT 且完全自定义结构的 JSON 配置服务，需借助源生成器
/// </summary>
/// <example>
///     自定义类型
///     <code>
///     public class MyCustomType
///     {
///         public string Name { get; set; } = "Test";
///         public int Value { get; set; } = 42;
///     }
///     </code>
///     源生成器预先生成格式化内容，避免动态查找
///     <code>
///     // 配置 JsonSourceGenerationOptions
///     [JsonSourceGenerationOptions(WriteIndented = true)]
///     [JsonSerializable(typeof(MyCustomType))]
///     internal partial class MyJsonContext : JsonSerializerContext;
///     </code>
///     使用示例
///     <code>
///     var data = new MyCustomType();
///     JsonConfigService.Save(data, "config", MyJsonContext.Default);
///     </code>
/// </example>
public static class JsonConfigService
{
    // 常量定义
    private const string TEMP_FILE_EXTENSION = ".temp";
    private const string BACKUP_TIMESTAMP_FORMAT = "yyyyMMdd_HHmmss";

    // 配置项
    public static string SavePath => MainConfig.ConfigSavePath;

    public static string FileExtension { get; set; } = ".QwQ.json";
    public static string BackupExtension { get; set; } = ".backup";

    private static readonly JsonServiceConfig _jsonServiceConfig = ConfigManager.JsonServiceConfig;
    public static bool EnableBackup => _jsonServiceConfig.EnableBackup;
    public static bool EnablePerformanceLogging => _jsonServiceConfig.EnablePerformanceLogging;
    public static int MaxBackupCount => _jsonServiceConfig.MaxBackupCount;

    // 内部状态
    private static readonly Lock _restoreLock = new();
    private static bool isRestoring;

    /// <summary>
    /// 同步保存方法
    /// </summary>
    /// <param name="data">要保存的数据</param>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <param name="jsonSerializerContext">JSON序列化上下文</param>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    public static void Save<T>(T data, string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);
        LoggerService.Debug($"开始保存配置: {fileName}");
        var stopwatch = EnablePerformanceLogging ? Stopwatch.StartNew() : null;

        try
        {
            PerformSave(data, fullPath, jsonSerializerContext);

            stopwatch?.Stop();
            LoggerService.Info(
                $"保存配置 {fileName} 完成! 文件路径: {fullPath}, 大小: {new FileInfo(fullPath).Length / 1024.0:F2} KB"
                    + (stopwatch != null ? $", 耗时: {stopwatch.ElapsedMilliseconds} ms" : "")
            );
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            HandleSaveError(ex, fullPath, fileName);
        }
    }

    /// <summary>
    /// 异步保存方法
    /// </summary>
    /// <param name="data">要保存的数据</param>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <param name="jsonSerializerContext">JSON序列化上下文</param>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    public static async Task SaveAsync<T>(T data, string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);
        await LoggerService.DebugAsync($"开始异步保存配置: {fileName}");
        var stopwatch = EnablePerformanceLogging ? Stopwatch.StartNew() : null;

        try
        {
            await PerformSaveAsync(data, fullPath, jsonSerializerContext);

            stopwatch?.Stop();
            await LoggerService.InfoAsync(
                $"异步保存配置 {fileName} 完成! 文件路径: {fullPath}, 大小: {new FileInfo(fullPath).Length / 1024.0:F2} KB"
                    + (stopwatch != null ? $", 耗时: {stopwatch.ElapsedMilliseconds} ms" : "")
            );
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            await HandleSaveErrorAsync(ex, fullPath, fileName);
        }
    }

    /// <summary>
    /// 同步读取方法
    /// </summary>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <param name="jsonSerializerContext">JSON序列化上下文</param>
    /// <returns>反序列化的数据，如果失败则返回默认值</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    public static T? Load<T>(string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);
        LoggerService.Debug($"开始读取配置: {fileName}");
        var stopwatch = EnablePerformanceLogging ? Stopwatch.StartNew() : null;

        try
        {
            if (!File.Exists(fullPath))
            {
                LoggerService.Warning($"配置文件未找到: {fullPath}，将返回默认值");

                // 尝试从备份恢复（防止无限递归）
                if (!EnableBackup || isRestoring || !TryRestoreFromBackup(fullPath))
                    return default;

                LoggerService.Info($"已从备份恢复配置文件: {fullPath}");
                // 重新尝试加载
                if (!File.Exists(fullPath))
                    return default;

                var restoredResult = PerformLoad<T>(fullPath, jsonSerializerContext);
                stopwatch?.Stop();
                LoggerService.Info(
                    $"读取配置 {fileName} 完成! 文件大小: {new FileInfo(fullPath).Length / 1024.0:F2} KB"
                        + (stopwatch != null ? $", 耗时: {stopwatch.ElapsedMilliseconds} ms" : "")
                );
                return restoredResult;
            }

            var result = PerformLoad<T>(fullPath, jsonSerializerContext);

            stopwatch?.Stop();
            LoggerService.Info(
                $"读取配置 {fileName} 完成! 文件大小: {new FileInfo(fullPath).Length / 1024.0:F2} KB"
                    + (stopwatch != null ? $", 耗时: {stopwatch.ElapsedMilliseconds} ms" : "")
            );

            return result;
        }
        catch (JsonException ex)
        {
            stopwatch?.Stop();
            return HandleJsonError<T>(ex, fullPath, fileName, jsonSerializerContext);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            return HandleLoadError<T>(ex, fullPath);
        }
    }

    /// <summary>
    /// 异步读取方法
    /// </summary>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <param name="jsonSerializerContext">JSON序列化上下文</param>
    /// <returns>反序列化的数据，如果失败则返回默认值</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    public static async Task<T?> LoadAsync<T>(string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);
        await LoggerService.DebugAsync($"开始异步读取配置: {fileName}");
        var stopwatch = EnablePerformanceLogging ? Stopwatch.StartNew() : null;

        try
        {
            if (!File.Exists(fullPath))
            {
                await LoggerService.WarningAsync($"配置文件未找到: {fullPath}，将返回默认值");

                // 尝试从备份恢复（防止无限递归）
                if (!EnableBackup || isRestoring || !await TryRestoreFromBackupAsync(fullPath))
                    return default;

                await LoggerService.InfoAsync($"已从备份恢复配置文件: {fullPath}");
                // 重新尝试加载
                if (!File.Exists(fullPath))
                    return default;

                var restoredResult = await PerformLoadAsync<T>(fullPath, jsonSerializerContext);
                stopwatch?.Stop();
                await LoggerService.InfoAsync(
                    $"异步读取配置 {fileName} 完成! 文件大小: {new FileInfo(fullPath).Length / 1024.0:F2} KB"
                        + (stopwatch != null ? $", 耗时: {stopwatch.ElapsedMilliseconds} ms" : "")
                );
                return restoredResult;
            }

            var result = await PerformLoadAsync<T>(fullPath, jsonSerializerContext);

            stopwatch?.Stop();
            await LoggerService.InfoAsync(
                $"异步读取配置 {fileName} 完成! 文件大小: {new FileInfo(fullPath).Length / 1024.0:F2} KB"
                    + (stopwatch != null ? $", 耗时: {stopwatch.ElapsedMilliseconds} ms" : "")
            );

            return result;
        }
        catch (JsonException ex)
        {
            stopwatch?.Stop();
            return await HandleJsonErrorAsync<T>(ex, fullPath, fileName, jsonSerializerContext);
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            return await HandleLoadErrorAsync<T>(ex, fullPath);
        }
    }

    /// <summary>
    /// 删除配置文件
    /// </summary>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <returns>是否成功删除</returns>
    /// <exception cref="ArgumentNullException">当文件名为null时抛出</exception>
    public static bool Delete(string fileName)
    {
        string fullPath = GetFullPath(fileName);
        try
        {
            if (!File.Exists(fullPath))
            {
                LoggerService.Warning($"尝试删除不存在的配置文件: {fullPath}");
                return false;
            }

            // 如果启用备份，先创建备份
            if (EnableBackup)
            {
                CreateBackup(fullPath);
            }

            File.Delete(fullPath);
            LoggerService.Info($"已删除配置文件: {fullPath}");
            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Error($"删除配置文件失败: {fullPath}, 错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 异步删除配置文件
    /// </summary>
    /// <param name="fileName">文件名（不含扩展名）</param>
    /// <returns>是否成功删除</returns>
    /// <exception cref="ArgumentNullException">当文件名为null时抛出</exception>
    public static async Task<bool> DeleteAsync(string fileName)
    {
        string fullPath = GetFullPath(fileName);
        try
        {
            if (!File.Exists(fullPath))
            {
                await LoggerService.WarningAsync($"尝试删除不存在的配置文件: {fullPath}");
                return false;
            }

            // 如果启用备份，先创建备份
            if (EnableBackup)
            {
                await CreateBackupAsync(fullPath);
            }

            File.Delete(fullPath);
            await LoggerService.InfoAsync($"已删除配置文件: {fullPath}");
            return true;
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"删除配置文件失败: {fullPath}, 错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取JSON配置服务状态信息
    /// </summary>
    public static string GetServiceStatus()
    {
        var status = new System.Text.StringBuilder();
        status.AppendLine($"保存路径: {SavePath}");
        status.AppendLine($"文件扩展名: {FileExtension}");
        status.AppendLine($"备份扩展名: {BackupExtension}");
        status.AppendLine($"启用备份: {EnableBackup}");
        status.AppendLine($"启用性能日志: {EnablePerformanceLogging}");
        status.AppendLine($"最大备份数量: {MaxBackupCount}");
        status.AppendLine($"正在恢复: {isRestoring}");
        status.AppendLine("服务状态: 运行中");

        return status.ToString();
    }

    // 私有辅助方法


    /// <summary>
    /// 执行保存操作
    /// </summary>
    private static void PerformSave<T>(T data, string fullPath, JsonSerializerContext jsonSerializerContext)
    {
        // 确保目录存在
        PathEnsurer.EnsureFileAndDirectoryExist(fullPath);

        // 如果启用备份且文件存在，先创建备份
        if (EnableBackup && File.Exists(fullPath))
        {
            CreateBackup(fullPath);
        }

        // 使用调用者提供的 JsonSerializerContext 进行序列化
        string json = JsonSerializer.Serialize(data, typeof(T), jsonSerializerContext);

        // 写入临时文件，成功后再替换原文件，避免写入过程中出错导致配置文件损坏
        string tempPath = $"{fullPath}{TEMP_FILE_EXTENSION}";
        File.WriteAllText(tempPath, json);

        // 原子性替换文件
        ReplaceFileAtomically(tempPath, fullPath);
    }

    /// <summary>
    /// 异步执行保存操作
    /// </summary>
    private static async Task PerformSaveAsync<T>(T data, string fullPath, JsonSerializerContext jsonSerializerContext)
    {
        // 确保目录存在
        PathEnsurer.EnsureFileAndDirectoryExist(fullPath);

        // 如果启用备份且文件存在，先创建备份
        if (EnableBackup && File.Exists(fullPath))
        {
            await CreateBackupAsync(fullPath);
        }

        // 使用调用者提供的 JsonSerializerContext 进行序列化
        string json = JsonSerializer.Serialize(data, typeof(T), jsonSerializerContext);

        // 写入临时文件，成功后再替换原文件，避免写入过程中出错导致配置文件损坏
        string tempPath = $"{fullPath}{TEMP_FILE_EXTENSION}";
        await File.WriteAllTextAsync(tempPath, json);

        // 原子性替换文件
        ReplaceFileAtomically(tempPath, fullPath);
    }

    /// <summary>
    /// 处理保存错误
    /// </summary>
    private static void HandleSaveError(Exception ex, string fullPath, string fileName)
    {
        LoggerService.Error(
            $"保存配置文件失败: 文件名: {fileName}, 路径: {fullPath}, 错误类型: {ex.GetType().Name}, 错误: {ex.Message}"
        );
        LoggerService.Debug($"异常详情: {ex}");

        // 清理临时文件
        CleanupTempFile($"{fullPath}{TEMP_FILE_EXTENSION}");

        // 尝试从备份恢复
        if (EnableBackup)
        {
            TryRestoreFromBackup(fullPath);
        }
    }

    /// <summary>
    /// 异步处理保存错误
    /// </summary>
    private static async Task HandleSaveErrorAsync(Exception ex, string fullPath, string fileName)
    {
        await LoggerService.ErrorAsync(
            $"异步保存配置文件失败: {fullPath}, 错误类型: {ex.GetType().Name}, 错误: {ex.Message}"
        );
        await LoggerService.DebugAsync($"异常详情: {ex}");

        // 清理临时文件
        CleanupTempFile($"{fullPath}{TEMP_FILE_EXTENSION}");

        // 尝试从备份恢复
        if (EnableBackup)
        {
            await TryRestoreFromBackupAsync(fullPath);
        }
    }

    /// <summary>
    /// 执行加载操作
    /// </summary>
    private static T? PerformLoad<T>(string fullPath, JsonSerializerContext jsonSerializerContext)
    {
        string json = File.ReadAllText(fullPath);
        var jsonTypeInfo = (JsonTypeInfo<T>)jsonSerializerContext.GetTypeInfo(typeof(T))!;
        return JsonSerializer.Deserialize(json, jsonTypeInfo);
    }

    /// <summary>
    /// 异步执行加载操作
    /// </summary>
    private static async Task<T?> PerformLoadAsync<T>(string fullPath, JsonSerializerContext jsonSerializerContext)
    {
        string json = await File.ReadAllTextAsync(fullPath);
        var jsonTypeInfo = (JsonTypeInfo<T>)jsonSerializerContext.GetTypeInfo(typeof(T))!;
        return JsonSerializer.Deserialize(json, jsonTypeInfo);
    }

    /// <summary>
    /// 处理JSON错误
    /// </summary>
    private static T? HandleJsonError<T>(
        JsonException ex,
        string fullPath,
        string fileName,
        JsonSerializerContext jsonSerializerContext
    )
    {
        LoggerService.Error($"配置文件格式错误: {fullPath}, 错误: {ex.Message}");
        LoggerService.Debug($"JSON异常详情: {ex}");

        // 尝试从备份恢复（防止无限递归）
        if (!EnableBackup || isRestoring || !TryRestoreFromBackup(fullPath))
            return default;

        LoggerService.Info("已从备份恢复配置文件，重新尝试加载");
        return Load<T>(fileName, jsonSerializerContext); // 递归调用，尝试从恢复的备份中加载
    }

    /// <summary>
    /// 异步处理JSON错误
    /// </summary>
    private static async Task<T?> HandleJsonErrorAsync<T>(
        JsonException ex,
        string fullPath,
        string fileName,
        JsonSerializerContext jsonSerializerContext
    )
    {
        await LoggerService.ErrorAsync($"配置文件格式错误: {fullPath}, 错误: {ex.Message}");
        await LoggerService.DebugAsync($"JSON异常详情: {ex}");

        // 尝试从备份恢复（防止无限递归）
        if (!EnableBackup || isRestoring || !await TryRestoreFromBackupAsync(fullPath))
            return default;

        await LoggerService.InfoAsync("已从备份恢复配置文件，重新尝试加载");
        return await LoadAsync<T>(fileName, jsonSerializerContext); // 递归调用，尝试从恢复的备份中加载
    }

    /// <summary>
    /// 处理加载错误
    /// </summary>
    private static T? HandleLoadError<T>(Exception ex, string fullPath)
    {
        LoggerService.Error($"读取配置文件失败: {fullPath}, 错误类型: {ex.GetType().Name}, 错误: {ex.Message}");
        LoggerService.Debug($"异常详情: {ex}");
        return default;
    }

    /// <summary>
    /// 异步处理加载错误
    /// </summary>
    private static async Task<T?> HandleLoadErrorAsync<T>(Exception ex, string fullPath)
    {
        await LoggerService.ErrorAsync(
            $"异步读取配置文件失败: {fullPath}, 错误类型: {ex.GetType().Name}, 错误: {ex.Message}"
        );
        await LoggerService.DebugAsync($"异常详情: {ex}");
        return default;
    }

    /// <summary>
    /// 获取完整文件路径
    /// </summary>
    private static string GetFullPath(string fileName)
    {
        return Path.Combine(SavePath, $"{fileName}{FileExtension}");
    }

    /// <summary>
    /// 原子性替换文件
    /// </summary>
    private static void ReplaceFileAtomically(string tempPath, string targetPath)
    {
        // 如果原文件存在，先删除
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        // 重命名临时文件
        File.Move(tempPath, targetPath);
    }

    /// <summary>
    /// 清理临时文件
    /// </summary>
    private static void CleanupTempFile(string tempPath)
    {
        try
        {
            if (!File.Exists(tempPath))
                return;

            File.Delete(tempPath);
            LoggerService.Debug($"已清理临时文件: {tempPath}");
        }
        catch (Exception ex)
        {
            LoggerService.Warning($"清理临时文件失败: {tempPath}, 错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建配置文件备份
    /// </summary>
    private static void CreateBackup(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return;

            string timestamp = DateTime.Now.ToString(BACKUP_TIMESTAMP_FORMAT);
            string backupPath = $"{filePath}{BackupExtension}.{timestamp}";
            File.Copy(filePath, backupPath, true);
            LoggerService.Debug($"已创建配置文件备份: {backupPath}");

            // 清理旧备份
            CleanupOldBackups(filePath);
        }
        catch (Exception ex)
        {
            LoggerService.Warning($"创建配置文件备份失败: {filePath}, 错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 异步创建配置文件备份
    /// </summary>
    private static async Task CreateBackupAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return;

            string timestamp = DateTime.Now.ToString(BACKUP_TIMESTAMP_FORMAT);
            string backupPath = $"{filePath}{BackupExtension}.{timestamp}";
            File.Copy(filePath, backupPath, true);
            await LoggerService.DebugAsync($"已创建配置文件备份: {backupPath}");

            // 清理旧备份
            await CleanupOldBackupsAsync(filePath);
        }
        catch (Exception ex)
        {
            await LoggerService.WarningAsync($"创建配置文件备份失败: {filePath}, 错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理旧备份文件
    /// </summary>
    private static void CleanupOldBackups(string filePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(filePath) ?? "";
            string fileNameWithoutPath = Path.GetFileName(filePath);
            var backupFiles = Directory
                .GetFiles(directory, $"{fileNameWithoutPath}{BackupExtension}.*")
                .OrderByDescending(f => f)
                .Skip(MaxBackupCount);

            foreach (string oldBackup in backupFiles)
            {
                try
                {
                    File.Delete(oldBackup);
                    LoggerService.Debug($"已删除旧备份文件: {oldBackup}");
                }
                catch (Exception ex)
                {
                    LoggerService.Warning($"删除旧备份文件失败: {oldBackup}, 错误: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            LoggerService.Warning($"清理旧备份文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 异步清理旧备份文件
    /// </summary>
    private static async Task CleanupOldBackupsAsync(string filePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(filePath) ?? "";
            string fileNameWithoutPath = Path.GetFileName(filePath);
            var backupFiles = Directory
                .GetFiles(directory, $"{fileNameWithoutPath}{BackupExtension}.*")
                .OrderByDescending(f => f)
                .Skip(MaxBackupCount);

            foreach (string oldBackup in backupFiles)
            {
                try
                {
                    File.Delete(oldBackup);
                    await LoggerService.DebugAsync($"已删除旧备份文件: {oldBackup}");
                }
                catch (Exception ex)
                {
                    await LoggerService.WarningAsync($"删除旧备份文件失败: {oldBackup}, 错误: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            await LoggerService.WarningAsync($"清理旧备份文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 尝试从备份恢复配置文件
    /// </summary>
    private static bool TryRestoreFromBackup(string filePath)
    {
        lock (_restoreLock)
        {
            if (isRestoring)
            {
                LoggerService.Warning("正在恢复中，跳过重复的恢复操作");
                return false;
            }

            isRestoring = true;
        }

        try
        {
            string directory = Path.GetDirectoryName(filePath) ?? "";
            string fileNameWithoutPath = Path.GetFileName(filePath);
            string[] backupFiles = Directory
                .GetFiles(directory, $"{fileNameWithoutPath}{BackupExtension}.*")
                .OrderByDescending(f => f)
                .ToArray();

            if (backupFiles.Length == 0)
            {
                LoggerService.Warning($"未找到可用的备份文件: {filePath}");
                return false;
            }

            string latestBackup = backupFiles[0];
            File.Copy(latestBackup, filePath, true);
            LoggerService.Info($"已从备份 {latestBackup} 恢复配置文件: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Error($"从备份恢复配置文件失败: {filePath}, 错误: {ex.Message}");
            return false;
        }
        finally
        {
            lock (_restoreLock)
            {
                isRestoring = false;
            }
        }
    }

    /// <summary>
    /// 异步尝试从备份恢复配置文件
    /// </summary>
    private static async Task<bool> TryRestoreFromBackupAsync(string filePath)
    {
        lock (_restoreLock)
        {
            if (isRestoring)
            {
                LoggerService.Warning("正在恢复中，跳过重复的恢复操作");
                return false;
            }

            isRestoring = true;
        }

        try
        {
            string directory = Path.GetDirectoryName(filePath) ?? "";
            string fileNameWithoutPath = Path.GetFileName(filePath);
            string[] backupFiles = Directory
                .GetFiles(directory, $"{fileNameWithoutPath}{BackupExtension}.*")
                .OrderByDescending(f => f)
                .ToArray();

            if (backupFiles.Length == 0)
            {
                await LoggerService.WarningAsync($"未找到可用的备份文件: {filePath}");
                return false;
            }

            string latestBackup = backupFiles[0];
            File.Copy(latestBackup, filePath, true);
            await LoggerService.InfoAsync($"已从备份 {latestBackup} 恢复配置文件: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"从备份恢复配置文件失败: {filePath}, 错误: {ex.Message}");
            return false;
        }
        finally
        {
            lock (_restoreLock)
            {
                isRestoring = false;
            }
        }
    }
}
