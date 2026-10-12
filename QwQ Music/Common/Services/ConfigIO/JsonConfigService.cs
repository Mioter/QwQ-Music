using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace QwQ_Music.Common.Services.ConfigIO;

/// <summary>
///     简化版 JSON 配置服务，支持 AOT 和自定义上下文。
/// </summary>
public class JsonConfigService(JsonSerializerContext jsonContext, string savePath) {
    public string SavePath { get; } = savePath;

    public string FileExtension { get; set; } = ".QwQ.json";

    // 获取完整路径
    private string GetFullPath(string fileName) { return Path.Combine(SavePath, $"{fileName}{FileExtension}"); }

    // 确保路径存在
    private void EnsureDirectoryExists(string fullPath) {
        string? dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    // 同步保存
    public void Save<T>(T data, string fileName) {
        string fullPath = GetFullPath(fileName);
        EnsureDirectoryExists(fullPath);

        string json = JsonSerializer.Serialize(data, typeof(T), jsonContext);
        File.WriteAllText(fullPath, json);
    }

    // 异步保存
    public async Task SaveAsync<T>(T data, string fileName) {
        string fullPath = GetFullPath(fileName);
        EnsureDirectoryExists(fullPath);

        string json = JsonSerializer.Serialize(data, typeof(T), jsonContext);
        await File.WriteAllTextAsync(fullPath, json).ConfigureAwait(false);
    }

    // 同步加载
    public T? Load<T>(string fileName) {
        string fullPath = GetFullPath(fileName);
        if (!File.Exists(fullPath))
            return default;

        string json = File.ReadAllText(fullPath);
        return jsonContext.GetTypeInfo(typeof(T)) is JsonTypeInfo<T> info ?
            JsonSerializer.Deserialize(json, info) :
            default;
    }

    // 异步加载
    public async Task<T?> LoadAsync<T>(string fileName) {
        string fullPath = GetFullPath(fileName);
        if (!File.Exists(fullPath))
            return default;

        string json = await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
        return jsonContext.GetTypeInfo(typeof(T)) is JsonTypeInfo<T> info ?
            JsonSerializer.Deserialize(json, info) :
            default;
    }
}