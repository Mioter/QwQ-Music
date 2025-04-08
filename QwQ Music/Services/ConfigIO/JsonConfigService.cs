using System;
using System.IO;
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

    /// <summary>
    /// 同步保存方法
    /// </summary>
    public static void Save<T>(T data, string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);

        try
        {
            PathEnsurer.EnsureFileAndDirectoryExist(fullPath);
            // 使用调用者提供的 JsonSerializerContext 进行序列化
            string json = JsonSerializer.Serialize(data, typeof(T), jsonSerializerContext);
            File.WriteAllText(fullPath, json);
        }
        catch (Exception ex)
        {
            LoggerService.Error($"保存配置文件失败: {fullPath}, 错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 异步保存方法
    /// </summary>
    public static async Task SaveAsync<T>(T data, string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);

        try
        {
            PathEnsurer.EnsureFileAndDirectoryExist(fullPath);

            // 使用调用者提供的 JsonSerializerContext 进行序列化
            string json = JsonSerializer.Serialize(data, typeof(T), jsonSerializerContext);
            await File.WriteAllTextAsync(fullPath, json);
        }
        catch (Exception ex)
        {
            LoggerService.Error($"异步保存配置文件失败: {fullPath}, 错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 同步读取方法
    /// </summary>
    public static T? Load<T>(string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);
        if (!File.Exists(fullPath))
        {
            LoggerService.Error($"配置文件未找到: {fullPath}");
            return default;
        }

        try
        {
            // 使用调用者提供的 JsonSerializerContext 进行反序列化
            string json = File.ReadAllText(fullPath);
            var jsonTypeInfo = (JsonTypeInfo<T>)jsonSerializerContext.GetTypeInfo(typeof(T))!;
            return JsonSerializer.Deserialize(json, jsonTypeInfo);
        }
        catch (Exception ex)
        {
            LoggerService.Error($"读取配置文件失败: {fullPath}, 错误: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// 异步读取方法
    /// </summary>
    public static async Task<T?> LoadAsync<T>(string fileName, JsonSerializerContext jsonSerializerContext)
    {
        string fullPath = GetFullPath(fileName);
        if (!File.Exists(fullPath))
        {
            LoggerService.Error($"配置文件未找到: {fullPath}");
            return default;
        }

        try
        {
            // 使用调用者提供的 JsonSerializerContext 进行反序列化
            string json = await File.ReadAllTextAsync(fullPath);
            var jsonTypeInfo = (JsonTypeInfo<T>)jsonSerializerContext.GetTypeInfo(typeof(T))!;
            return JsonSerializer.Deserialize(json, jsonTypeInfo);
        }
        catch (Exception ex)
        {
            LoggerService.Error($"异步读取配置文件失败: {fullPath}, 错误: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// 获取完整文件路径
    /// </summary>
    private static string GetFullPath(string fileName)
    {
        return Path.Combine(SavePath, $"{fileName}{FileExtension}");
    }
}
