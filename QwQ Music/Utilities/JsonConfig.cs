using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace QwQ_Music.Utilities;

/// <summary>
/// Json 配置类，提供读取和写入 JSON 的方法。
/// </summary>
/// <typeparam name="T">要处理的数据类型。</typeparam>
public class JsonConfig<T>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// 构造函数，初始化文件路径。
    /// </summary>
    /// <param name="filePath">文件路径。</param>
    /// <param name="paths">用于构建文件路径的路径数组。</param>
    public JsonConfig(string filePath, params string[] paths)
    {
        FilePath = Path.Combine(filePath, Path.Combine(paths));
        ValidateFilePath();
    }

    /// <summary>
    /// 文件路径。
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// 异步从 JSON 文件加载数据。
    /// </summary>
    /// <returns>加载的数据对象。</returns>
    /// <exception cref="FileNotFoundException">当 JSON 文件不存在时抛出。</exception>
    public async Task<T?> LoadFromJsonAsync()
    {
        return await ReadFileAsync(async fs =>
        {
            if (fs.Length == 0) return default;
            return await JsonSerializer.DeserializeAsync<T>(fs).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// 同步从 JSON 文件加载数据。
    /// </summary>
    /// <returns>加载的数据对象。</returns>
    /// <exception cref="FileNotFoundException">当 JSON 文件不存在时抛出。</exception>
    public T? LoadFromJson()
    {
        return ReadFileSync(() =>
        {
            string jsonContent = File.ReadAllText(FilePath);
            return string.IsNullOrWhiteSpace(jsonContent) ? default : JsonSerializer.Deserialize<T>(jsonContent);
        });
    }

    /// <summary>
    /// 异步保存数据到 JSON 文件。
    /// </summary>
    /// <param name="data">要保存的数据对象。</param>
    public async Task SaveToJsonAsync(T data)
    {
        await WriteFileAsync(async fs =>
        {
            await JsonSerializer.SerializeAsync(fs, data, _jsonSerializerOptions).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// 同步保存数据到 JSON 文件。
    /// </summary>
    /// <param name="data">要保存的数据对象。</param>
    public void SaveToJson(T data)
    {
        WriteFileSync(() =>
        {
            string jsonContent = JsonSerializer.Serialize(data, _jsonSerializerOptions);
            File.WriteAllText(FilePath, jsonContent);
        });
    }

    #region Helper Methods

    /// <summary>
    /// 验证文件路径是否有效。
    /// </summary>
    private void ValidateFilePath()
    {
        if (string.IsNullOrWhiteSpace(FilePath) || !Path.IsPathFullyQualified(FilePath))
        {
            throw new ArgumentException("Invalid file path.");
        }
    }

    /// <summary>
    /// 异步读取文件并执行操作。
    /// </summary>
    /// <param name="action">对文件流的操作。</param>
    /// <returns>操作结果。</returns>
    private async Task<T?> ReadFileAsync(Func<FileStream, Task<T?>> action)
    {
        EnsureFileExists();
        try
        {
            await using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await action(fs).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new IOException("Error reading JSON file.", ex);
        }
    }

    /// <summary>
    /// 同步读取文件并执行操作。
    /// </summary>
    /// <param name="action">对文件内容的操作。</param>
    /// <returns>操作结果。</returns>
    private T? ReadFileSync(Func<T?> action)
    {
        EnsureFileExists();
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            throw new IOException("Error reading JSON file.", ex);
        }
    }

    /// <summary>
    /// 异步写入文件并执行操作。
    /// </summary>
    /// <param name="action">对文件流的操作。</param>
    private async Task WriteFileAsync(Func<FileStream, Task> action)
    {
        try
        {
            await using var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await action(fs).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new IOException("Error writing JSON file.", ex);
        }
    }

    /// <summary>
    /// 同步写入文件并执行操作。
    /// </summary>
    /// <param name="action">对文件内容的操作。</param>
    private void WriteFileSync(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            throw new IOException("Error writing JSON file.", ex);
        }
    }

    /// <summary>
    /// 确保文件存在，否则抛出异常。
    /// </summary>
    private void EnsureFileExists()
    {
        if (!File.Exists(FilePath))
        {
            throw new FileNotFoundException("JSON file not found.", FilePath);
        }
    }

    #endregion
}
