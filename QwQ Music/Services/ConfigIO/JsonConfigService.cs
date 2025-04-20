using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using QwQ_Music.Utilities;

namespace QwQ_Music.Services.ConfigIO;

/// <summary>
///     支持 AOT 且完全自定义结构的 JSON，需借助源生成器
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
    // 静态配置项
    public static string SavePath { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "config");
    public static string FileExtension { get; set; } = ".QwQ.json";
    public static string BackupExtension { get; set; } = ".backup";
    public static bool EnableBackup { get; set; } = false;
    public static bool EnablePerformanceLogging { get; set; } = true;
    public static int MaxBackupCount { get; set; } = 3;

    /// <summary>
    /// 同步保存方法
    /// </summary>
    public static void Save<T>(T data, string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);
        LoggerService.Debug($"开始保存配置: {fileName}");
        var stopwatch = EnablePerformanceLogging ? Stopwatch.StartNew() : null;

        try
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
            string tempPath = $"{fullPath}.temp";
            File.WriteAllText(tempPath, json);
            
            // 如果原文件存在，先删除
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            
            // 重命名临时文件
            File.Move(tempPath, fullPath);

            stopwatch?.Stop();
            LoggerService.Info(
                $"保存配置 {fileName} 完成! 文件路径: {fullPath}, 大小: {new FileInfo(fullPath).Length / 1024.0:F2} KB" + 
                (stopwatch != null ? $", 耗时: {stopwatch.ElapsedMilliseconds} ms" : "")
            );
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            LoggerService.Error($"保存配置文件失败: {fullPath}, 错误类型: {ex.GetType().Name}, 错误: {ex.Message}");
            
            // 记录详细的异常堆栈
            LoggerService.Debug($"异常详情: {ex}");
            
            // 尝试从备份恢复
            if (EnableBackup)
            {
                TryRestoreFromBackup(fullPath);
            }
        }
    }

    /// <summary>
    /// 异步保存方法
    /// </summary>
    public static async Task SaveAsync<T>(T data, string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);
        await LoggerService.DebugAsync($"开始异步保存配置: {fileName}");
        var stopwatch = EnablePerformanceLogging ? Stopwatch.StartNew() : null;

        try
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
            string tempPath = $"{fullPath}.temp";
            await File.WriteAllTextAsync(tempPath, json);
            
            // 如果原文件存在，先删除
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            
            // 重命名临时文件
            File.Move(tempPath, fullPath);

            stopwatch?.Stop();
            await LoggerService.InfoAsync(
                $"异步保存配置 {fileName} 完成! 文件路径: {fullPath}, 大小: {new FileInfo(fullPath).Length / 1024.0:F2} KB" + 
                (stopwatch != null ? $", 耗时: {stopwatch.ElapsedMilliseconds} ms" : "")
            );
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            await LoggerService.ErrorAsync($"异步保存配置文件失败: {fullPath}, 错误类型: {ex.GetType().Name}, 错误: {ex.Message}");
            
            // 记录详细的异常堆栈
            await LoggerService.DebugAsync($"异常详情: {ex}");
            
            // 尝试从备份恢复
            if (EnableBackup)
            {
                await TryRestoreFromBackupAsync(fullPath);
            }
        }
    }

    /// <summary>
    /// 同步读取方法
    /// </summary>
    public static T? Load<T>(string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);
        LoggerService.Debug($"开始读取配置: {fileName}");
        var stopwatch = EnablePerformanceLogging ? Stopwatch.StartNew() : null;

        if (!File.Exists(fullPath))
        {
            LoggerService.Warning($"配置文件未找到: {fullPath}，将返回默认值");
            
            // 尝试从备份恢复
            if (EnableBackup && TryRestoreFromBackup(fullPath))
            {
                LoggerService.Info($"已从备份恢复配置文件: {fullPath}");
            }
            else
            {
                return default;
            }
        }

        try
        {
            // 使用调用者提供的 JsonSerializerContext 进行反序列化
            string json = File.ReadAllText(fullPath);
            var jsonTypeInfo = (JsonTypeInfo<T>)jsonSerializerContext.GetTypeInfo(typeof(T))!;
            var result = JsonSerializer.Deserialize(json, jsonTypeInfo);

            stopwatch?.Stop();
            LoggerService.Info(
                $"读取配置 {fileName} 完成! 文件大小: {new FileInfo(fullPath).Length / 1024.0:F2} KB" + 
                (stopwatch != null ? $", 耗时: {stopwatch.ElapsedMilliseconds} ms" : "")
            );
            
            return result;
        }
        catch (JsonException ex)
        {
            stopwatch?.Stop();
            LoggerService.Error($"配置文件格式错误: {fullPath}, 错误: {ex.Message}");
            LoggerService.Debug($"JSON异常详情: {ex}");
            
            // 尝试从备份恢复
            if (EnableBackup && TryRestoreFromBackup(fullPath))
            {
                LoggerService.Info("已从备份恢复配置文件，重新尝试加载");
                return Load<T>(fileName, jsonSerializerContext); // 递归调用，尝试从恢复的备份中加载
            }
            
            return default;
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            LoggerService.Error($"读取配置文件失败: {fullPath}, 错误类型: {ex.GetType().Name}, 错误: {ex.Message}");
            LoggerService.Debug($"异常详情: {ex}");
            return default;
        }
    }

    /// <summary>
    /// 异步读取方法
    /// </summary>
    public static async Task<T?> LoadAsync<T>(string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);
        await LoggerService.DebugAsync($"开始异步读取配置: {fileName}");
        var stopwatch = EnablePerformanceLogging ? Stopwatch.StartNew() : null;

        if (!File.Exists(fullPath))
        {
            await LoggerService.WarningAsync($"配置文件未找到: {fullPath}，将返回默认值");
            
            // 尝试从备份恢复
            if (EnableBackup && await TryRestoreFromBackupAsync(fullPath))
            {
                await LoggerService.InfoAsync($"已从备份恢复配置文件: {fullPath}");
            }
            else
            {
                return default;
            }
        }

        try
        {
            // 使用调用者提供的 JsonSerializerContext 进行反序列化
            string json = await File.ReadAllTextAsync(fullPath);
            var jsonTypeInfo = (JsonTypeInfo<T>)jsonSerializerContext.GetTypeInfo(typeof(T))!;
            var result = JsonSerializer.Deserialize(json, jsonTypeInfo);

            stopwatch?.Stop();
            await LoggerService.InfoAsync(
                $"异步读取配置 {fileName} 完成! 文件大小: {new FileInfo(fullPath).Length / 1024.0:F2} KB" + 
                (stopwatch != null ? $", 耗时: {stopwatch.ElapsedMilliseconds} ms" : "")
            );
            
            return result;
        }
        catch (JsonException ex)
        {
            stopwatch?.Stop();
            await LoggerService.ErrorAsync($"配置文件格式错误: {fullPath}, 错误: {ex.Message}");
            await LoggerService.DebugAsync($"JSON异常详情: {ex}");
            
            // 尝试从备份恢复
            if (EnableBackup && await TryRestoreFromBackupAsync(fullPath))
            {
                await LoggerService.InfoAsync("已从备份恢复配置文件，重新尝试加载");
                return await LoadAsync<T>(fileName, jsonSerializerContext); // 递归调用，尝试从恢复的备份中加载
            }
            
            return default;
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            await LoggerService.ErrorAsync($"异步读取配置文件失败: {fullPath}, 错误类型: {ex.GetType().Name}, 错误: {ex.Message}");
            await LoggerService.DebugAsync($"异常详情: {ex}");
            return default;
        }
    }

    /// <summary>
    /// 检查配置文件是否存在
    /// </summary>
    public static bool Exists(string fileName)
    {
        string fullPath = GetFullPath(fileName);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// 删除配置文件
    /// </summary>
    public static bool Delete(string fileName)
    {
        string fullPath = GetFullPath(fileName);
        try
        {
            if (File.Exists(fullPath))
            {
                // 如果启用备份，先创建备份
                if (EnableBackup)
                {
                    CreateBackup(fullPath);
                }
                
                File.Delete(fullPath);
                LoggerService.Info($"已删除配置文件: {fullPath}");
                return true;
            }
            
            LoggerService.Warning($"尝试删除不存在的配置文件: {fullPath}");
            return false;
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
    public static async Task<bool> DeleteAsync(string fileName)
    {
        string fullPath = GetFullPath(fileName);
        try
        {
            if (File.Exists(fullPath))
            {
                // 如果启用备份，先创建备份
                if (EnableBackup)
                {
                    await CreateBackupAsync(fullPath);
                }
                
                File.Delete(fullPath);
                await LoggerService.InfoAsync($"已删除配置文件: {fullPath}");
                return true;
            }
            
            await LoggerService.WarningAsync($"尝试删除不存在的配置文件: {fullPath}");
            return false;
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"删除配置文件失败: {fullPath}, 错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取完整文件路径
    /// </summary>
    private static string GetFullPath(string fileName)
    {
        return Path.Combine(SavePath, $"{fileName}{FileExtension}");
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
                
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
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
                
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
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
            string[] backupFiles = Directory.GetFiles(directory, $"{fileNameWithoutPath}{BackupExtension}.*")
                .OrderByDescending(f => f)
                .Skip(MaxBackupCount)
                .ToArray();
                
            foreach (string oldBackup in backupFiles)
            {
                File.Delete(oldBackup);
                LoggerService.Debug($"已删除旧备份文件: {oldBackup}");
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
            string[] backupFiles = Directory.GetFiles(directory, $"{fileNameWithoutPath}{BackupExtension}.*")
                .OrderByDescending(f => f)
                .Skip(MaxBackupCount)
                .ToArray();
                
            foreach (string oldBackup in backupFiles)
            {
                File.Delete(oldBackup);
                await LoggerService.DebugAsync($"已删除旧备份文件: {oldBackup}");
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
        try
        {
            string directory = Path.GetDirectoryName(filePath) ?? "";
            string fileNameWithoutPath = Path.GetFileName(filePath);
            string[] backupFiles = Directory.GetFiles(directory, $"{fileNameWithoutPath}{BackupExtension}.*")
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
    }
    
    /// <summary>
    /// 异步尝试从备份恢复配置文件
    /// </summary>
    private static async Task<bool> TryRestoreFromBackupAsync(string filePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(filePath) ?? "";
            string fileNameWithoutPath = Path.GetFileName(filePath);
            string[] backupFiles = Directory.GetFiles(directory, $"{fileNameWithoutPath}{BackupExtension}.*")
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
    }
}