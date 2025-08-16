using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace QwQ_Music.Common.Services;

public static class FileOperationService
{
    /// <summary>
    ///     异步保存图片到指定路径。
    /// </summary>
    public static async Task<bool> SaveImageAsync(Bitmap cover, string filePath, bool overwrite = false)
    {
        string? directory = Path.GetDirectoryName(filePath);

        if (string.IsNullOrEmpty(directory))
        {
            await LoggerService.ErrorAsync($"无效的文件路径：{filePath}");
            return false;
        }

        Directory.CreateDirectory(directory);

        if (!overwrite && File.Exists(filePath))
            return false;

        try
        {
            await Task.Run(() =>
            {
                var mode = overwrite ? FileMode.Create : FileMode.CreateNew;
                using var fs = new FileStream(filePath, mode, FileAccess.Write, FileShare.Read);
                cover.Save(fs);
            });
        }
        catch (IOException) when (!overwrite && File.Exists(filePath))
        {
            // 文件已存在，忽略冲突
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"保存图片失败 {filePath}: {ex.Message}, 类型: {ex.GetType().Name}, 异常堆栈: {ex.StackTrace}");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     将存储项集合转换为本地绝对路径字符串列表。
    /// </summary>
    public static List<string> ConvertStorageItemsToPathStrings(IEnumerable<IStorageItem> items)
    {
        return items
            .Where(item => item.Path.IsAbsoluteUri)
            .Select(item => item.Path.LocalPath)
            .ToList();
    }

    /// <summary>
    ///     获取路径列表中所有文件（递归搜索目录）。
    /// </summary>
    public static List<string> GetAllFilePaths(IReadOnlyList<string> paths)
    {
        var result = new List<string>();

        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            try
            {
                if (Directory.Exists(path))
                {
                    result.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                }
                else if (File.Exists(path))
                {
                    result.Add(path);
                }
                else
                {
                    LoggerService.Warning($"路径不存在: {path}");
                }
            }
            catch (Exception ex)
            {
                LoggerService.Error($"访问路径失败 {path}: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    ///     在系统默认文件管理器中打开指定文件或目录。
    /// </summary>
    public static void OpenInFileManager(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("路径不能为空", nameof(path));

        if (!File.Exists(path) && !Directory.Exists(path))
            throw new FileNotFoundException("指定的文件或目录不存在", path);

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true,
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-R \"{path}\"",
                    UseShellExecute = true,
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string dir = Path.GetDirectoryName(path) ?? "/";

                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{dir}\"",
                    UseShellExecute = true,
                });
            }
            else
            {
                NotificationService.Error("当前操作系统不支持该操作");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Error($"打开文件管理器失败: \n{ex.Message}");
            LoggerService.Error($"打开文件管理器失败: \n{ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    ///     显示保存文件对话框并返回用户选择的路径。
    /// </summary>
    public static async Task<string?> GetSavePathAsync(
        TopLevel topLevel,
        string? title = null,
        IEnumerable<FilePickerFileType>? fileTypes = null,
        string? suggestedFileName = null
        )
    {
        ArgumentNullException.ThrowIfNull(topLevel);

        var options = new FilePickerSaveOptions
        {
            Title = title ?? "保存文件",
            FileTypeChoices = fileTypes?.ToList(),
            SuggestedFileName = suggestedFileName,
        };

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);

        return file?.Path.LocalPath;
    }

    /// <summary>
    ///     从文件系统打开图片
    /// </summary>
    /// <returns>获取的位图</returns>
    public static async Task<Bitmap?> OpenImageFile(TopLevel topLevel)
    {
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "选择图片",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("图片文件")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp"],
                    },
                ],
            }
        );

        if (files.Count <= 0)
            return null;

        try
        {
            var file = files[0];
            await using var stream = await file.OpenReadAsync();

            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            NotificationService.Error($"打开文件失败了！\n{ex.Message}");

            return null;
        }
    }
}
