using System;
using System.IO;
using System.Linq;

namespace QwQ_Music.Common.Utilities;

public static class PathEnsurer
{
    /// <summary>
    ///     确保指定目录路径存在，如果不存在则创建它。
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
    ///     确保指定文件路径及其所在目录存在。如果目录不存在则创建，如果文件不存在则创建空文件。
    /// </summary>
    /// <param name="filePath">要确保存在的文件路径。</param>
    public static string EnsureFileAndDirectoryExist(string filePath)
    {
        EnsureDirectoryExists(Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException());

        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
        }

        return filePath;
    }

    /// <summary>
    ///     清理文件名中的非法字符。
    /// </summary>
    /// <param name="fileName">原始文件名。</param>
    /// <returns>清理后的文件名。</returns>
    public static string CleanFileName(string fileName)
    {
        return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c, '&'));
    }
}
