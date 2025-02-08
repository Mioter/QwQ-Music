using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace QwQ_Music.Tools;

/// <summary>
///     Json 配置类，提供读取和写入 JSON 的方法。
/// </summary>
/// <typeparam name="T">要处理的数据类型。</typeparam>
public class JsonConfig<T>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
    };
    public readonly string FilePath;

    /// <summary>
    ///     构造函数，初始化文件路径。
    /// </summary>
    /// <param name="filePath">文件路径。</param>
    /// <param name="paths">用于构建文件路径的路径数组。</param>
    public JsonConfig(string filePath, params string[] paths)
    {
        FilePath = Path.Combine(filePath, Path.Combine(paths));
        if (string.IsNullOrWhiteSpace(FilePath) || !Path.IsPathFullyQualified(FilePath))
        {
            throw new ArgumentException("Invalid file path.");
        }
    }

    /// <summary>
    ///     异步从 JSON 文件加载数据。
    /// </summary>
    /// <returns>加载的数据对象。</returns>
    /// <exception cref="FileNotFoundException">当 JSON 文件不存在时抛出。</exception>
    public async Task<T?> LoadFromJsonAsync()
    {
        if (!File.Exists(FilePath))
        {
            throw new FileNotFoundException("JSON file not found.", FilePath);
        }

        try
        {
            await using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // 检查文件是否为空
            if (fs.Length == 0)
            {
                return default; // 或者直接返回 null, 取决于 T 是否支持 null。
            }

            return await JsonSerializer.DeserializeAsync<T>(fs).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // 处理异常，例如记录日志
            throw new IOException("Error loading JSON file.", ex);
        }
    }

    /// <summary>
    ///     同步从 JSON 文件加载数据。
    /// </summary>
    /// <returns>加载的数据对象。</returns>
    /// <exception cref="FileNotFoundException">当 JSON 文件不存在时抛出。</exception>
    public T? LoadFromJson()
    {
        if (!File.Exists(FilePath))
        {
            throw new FileNotFoundException("JSON file not found.", FilePath);
        }

        try
        {
            string jsonContent = File.ReadAllText(FilePath);

            // 检查文件内容是否为空
            return string.IsNullOrWhiteSpace(jsonContent)
                ? default
                : // 或者直接返回 null, 取决于 T 是否支持 null。
                JsonSerializer.Deserialize<T>(jsonContent);

        }
        catch (Exception ex)
        {
            // 处理异常，例如记录日志
            throw new IOException("Error loading JSON file.", ex);
        }
    }


    /// <summary>
    ///     异步保存数据到 JSON 文件。
    /// </summary>
    /// <param name="data">要保存的数据对象。</param>
    public async Task SaveToJsonAsync(T data)
    {
        try
        {
            await using var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(fs, data, _jsonSerializerOptions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // 处理异常，例如记录日志
            throw new IOException("Error saving JSON file.", ex);
        }
    }

    /// <summary>
    ///     同步保存数据到 JSON 文件。
    /// </summary>
    /// <param name="data">要保存的数据对象。</param>
    public void SaveToJson(T data)
    {
        try
        {
            string jsonContent = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            File.WriteAllText(FilePath, jsonContent);
        }
        catch (Exception ex)
        {
            // 处理异常，例如记录日志
            throw new IOException("Error saving JSON file.", ex);
        }
    }
}
