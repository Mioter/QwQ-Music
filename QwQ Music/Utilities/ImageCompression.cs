using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using QwQ_Music.Services;

namespace QwQ_Music.Utilities;

/// <summary>
/// 图片压缩工具类
/// </summary>
public static class ImageCompression
{
    /// <summary>
    /// 压缩位图到指定大小以内
    /// </summary>
    /// <param name="bitmap">原始位图</param>
    /// <param name="maxSizeInBytes">最大文件大小（字节）</param>
    /// <param name="quality">JPEG质量（0-100），默认85</param>
    /// <returns>压缩后的位图</returns>
    public static Bitmap? CompressBitmapAsync(Bitmap bitmap, long maxSizeInBytes, int quality = 85)
    {
        try
        {
            // 首先检查原始大小
            long originalSize = GetBitmapSize(bitmap);
            if (originalSize <= maxSizeInBytes)
            {
                return bitmap; // 已经满足大小要求
            }

            // 尝试通过调整尺寸来压缩
            var compressedBitmap = CompressBySizeAsync(bitmap, maxSizeInBytes);
            if (compressedBitmap != null)
            {
                return compressedBitmap;
            }

            // 如果还是太大，返回最小尺寸的版本
            return CreateMinimalBitmapAsync(bitmap);
        }
        catch (Exception ex)
        {
            LoggerService.Error($"压缩图片时发生错误: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 通过调整尺寸来压缩图片
    /// </summary>
    private static Bitmap? CompressBySizeAsync(Bitmap bitmap, long maxSizeInBytes)
    {
        var originalSize = bitmap.PixelSize;
        var currentWidth = originalSize.Width;
        var currentHeight = originalSize.Height;

        // 逐步缩小尺寸，每次缩小15%
        while (currentWidth > 200 && currentHeight > 200) // 设置最小尺寸限制
        {
            currentWidth = (int)(currentWidth * 0.85);
            currentHeight = (int)(currentHeight * 0.85);

            try
            {
                // 创建缩放后的位图
                var scaledBitmap = bitmap.CreateScaledBitmap(new PixelSize(currentWidth, currentHeight));

                // 检查缩放后的大小
                long scaledSize = GetBitmapSize(scaledBitmap);

                if (scaledSize <= maxSizeInBytes)
                {
                    return scaledBitmap;
                }
            }
            catch
            {
                // ignored
            }
        }

        return null; // 无法通过尺寸调整达到目标大小
    }

    /// <summary>
    /// 创建最小尺寸的位图
    /// </summary>
    private static Bitmap? CreateMinimalBitmapAsync(Bitmap bitmap)
    {
        try
        {
            var minSize = new PixelSize(200, 200);
            var minimalBitmap = bitmap.CreateScaledBitmap(minSize);
            return minimalBitmap;
        }
        catch (Exception ex)
        {
            LoggerService.Error($"创建最小尺寸位图时发生错误: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取图片文件大小（字节）
    /// </summary>
    /// <param name="bitmap">位图</param>
    /// <returns>文件大小</returns>
    public static long GetBitmapSize(Bitmap bitmap)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream);
            return memoryStream.Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 将字节大小转换为可读格式
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>可读格式的大小字符串</returns>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
