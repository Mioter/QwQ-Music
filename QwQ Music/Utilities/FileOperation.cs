using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using QwQ_Music.Services;

namespace QwQ_Music.Utilities;

public static class FileOperation
{
    /// <summary>
    /// 异步保存图片。
    /// </summary>
    /// <param name="cover">专辑封面图片。</param>
    /// <param name="filePath">专辑封面文路径。</param>
    public static async Task<bool> SaveImageAsync(Bitmap cover, string filePath)
    {
        // 确保目录存在
        // 注意：如果 MusicCoverSavePath 本身就不存在，CreateDirectory 需要能创建多级目录
        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        else
        {
            // 如果无法获取目录路径（例如 coverFileName 是根路径下的文件名），记录错误或采取其他措施
            await LoggerService.ErrorAsync($"Invalid cover file path generated: {filePath}");
            return false;
        }

        // 检查文件是否存在
        if (File.Exists(filePath))
        {
            /*LoggerService.Info($"Cover already exists: {filePath}. Skipping save.");*/
            return true;
        }

        try
        {
            // 将文件流的创建和保存操作移至 Task.Run 内部
            await Task.Run(() =>
                {
                    // 在后台线程中创建、使用和释放文件流
                    using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    cover.Save(fs); // Bitmap.Save 是同步操作
                })
                .ConfigureAwait(false); // 继续使用 ConfigureAwait(false)

            return true;
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync(
                $"Failed to save cover {filePath}: {ex.Message}, TypeName: {ex.GetType().Name}"
            );
            return false;
        }
    }

    /// <summary>
    /// 将存储项目转换为路径字符串列表
    /// </summary>
    public static List<string> ConvertStorageItemsToPathStrings(IEnumerable<IStorageItem> items)
    {
        return items
            .Select(item =>
            {
                try
                {
                    return item.Path.IsAbsoluteUri ? item.Path.LocalPath : null;
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is UriFormatException)
                {
                    Console.WriteLine($"无法解析路径: {item.Path} (错误: {ex.Message})");
                    return null;
                }
            })
            .Where(path => !string.IsNullOrEmpty(path))
            .ToList()!;
    }

    /// <summary>
    /// 从路径列表获取所有文件路径（包括子目录）
    /// </summary>
    /// <param name="paths">路径列表</param>
    /// <returns>所有文件路径</returns>
    public static List<string> GetAllFilePaths(IReadOnlyList<string> paths)
    {
        var allFilePaths = new List<string>();

        foreach (string path in paths)
        {
            if (string.IsNullOrEmpty(path))
                continue;

            if (Directory.Exists(path))
            {
                try
                {
                    string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                    allFilePaths.AddRange(files);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法访问目录 {path}: {ex.Message}");
                }
            }
            else if (File.Exists(path))
            {
                allFilePaths.Add(path);
            }
            else
            {
                Console.WriteLine($"路径不存在: {path}");
            }
        }

        return allFilePaths;
    }

    /// <summary>
    /// 格式化文件大小为人类可读的形式。
    /// </summary>
    /// <param name="bytes">文件大小（字节）。</param>
    /// <returns>格式化后的文件大小。</returns>
    public static string FormatFileSize(long bytes)
    {
        const int scale = 1024;
        string[] orders = ["GiB", "MiB", "KiB", "Bytes"];
        double len = bytes;
        int order = orders.Length - 1;

        while (len >= scale && order > 0)
        {
            order--;
            len /= scale;
        }

        return $"{len:0.0}{orders[order]}";
    }
}
