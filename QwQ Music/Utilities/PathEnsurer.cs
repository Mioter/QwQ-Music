using System.IO;

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
}
