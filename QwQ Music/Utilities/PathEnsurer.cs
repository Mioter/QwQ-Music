using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace QwQ_Music.Utilities;

public static class PathEnsurer
{
    /// <summary>
    /// 确保指定目录路径存在，如果不存在则创建它。
    /// </summary>
    /// <param name="directoryPath">要确保存在的目录路径。</param>
    public static string EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        return directoryPath;
    }

    /// <summary>
    /// 确保指定文件路径及其所在目录存在。如果目录不存在则创建，如果文件不存在则创建空文件。
    /// </summary>
    /// <param name="filePath">要确保存在的文件路径。</param>
    public static string EnsureFileAndDirectoryExist(string filePath)
    {
        EnsureDirectoryExists(Path.GetDirectoryName(filePath)!);

        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
        }

        return filePath;
    }

    /// <summary>
    /// 清理文件名中的非法字符。
    /// </summary>
    /// <param name="fileName">原始文件名。</param>
    /// <returns>清理后的文件名。</returns>
    public static string CleanFileName(string fileName) =>
        Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c, '&'));

    /// <summary>
    /// 在系统默认文件管理器中打开指定文件或目录
    /// </summary>
    /// <param name="path">文件或目录路径</param>
    public static void OpenInFileManager(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: 使用 explorer.exe
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true,
                }
            );
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: 使用 open 命令
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-R \"{path}\"",
                    UseShellExecute = true,
                }
            );
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: 尝试使用 xdg-open
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{Path.GetDirectoryName(path)}\"",
                    UseShellExecute = true,
                }
            );
        }
        else
        {
            throw new PlatformNotSupportedException("当前操作系统不支持此操作");
        }
    }

    /// <summary>
    /// 确保字符串是完整路径。如果不是，则拼接基础路径。
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="basePath">基础路径</param>
    /// <returns>完整路径</returns>
    public static string EnsureFullPath(string input, string basePath)
    {
        // 检查是否包含目录信息
        string? directory = Path.GetDirectoryName(input);
        return string.IsNullOrEmpty(directory)
            ?
            // 不包含目录信息，拼接基础路径
            Path.Combine(basePath, input)
            :
            // 已经是完整路径，直接返回
            Path.GetFullPath(input);
    }
}
