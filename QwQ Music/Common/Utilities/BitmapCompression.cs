using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;

namespace QwQ_Music.Common.Utilities;

/// <summary>
///     图片压缩工具类
/// </summary>
public static class BitmapCompression
{
    public static int MinWidth { get; set; } = 200;

    public static int MinHeight { get; set; } = 200;

    public static double ScaleFactor { get; set; } = 0.85;

    /// <summary>
    ///     压缩位图到指定大小以内（大小压缩）
    /// </summary>
    /// <param name="bitmap">原始位图</param>
    /// <param name="maxSizeInBytes">最大文件大小（字节）</param>
    /// <returns>压缩后的位图</returns>
    public static async Task<Bitmap?> CompressBitmapAsync(Bitmap bitmap, long maxSizeInBytes)
    {
        long originalSize = GetBitmapSize(bitmap);

        if (originalSize <= maxSizeInBytes)
            return bitmap;

        // 尝试通过缩放尺寸压缩
        var scaledBitmap = await CompressByScalingAsync(bitmap, maxSizeInBytes);

        return scaledBitmap ?? null;
    }

    /// <summary>
    ///     通过逐步缩小尺寸进行压缩
    /// </summary>
    private static async Task<Bitmap?> CompressByScalingAsync(Bitmap bitmap, long maxSizeInBytes)
    {
        var originalSize = bitmap.PixelSize;
        int width = originalSize.Width;
        int height = originalSize.Height;

        while (width > MinWidth && height > MinHeight)
        {
            width = Math.Max(MinWidth, (int)(width * ScaleFactor));
            height = Math.Max(MinHeight, (int)(height * ScaleFactor));

            try
            {
                var scaledBitmap = bitmap.CreateScaledBitmap(new PixelSize(width, height));
                long scaledSize = GetBitmapSize(scaledBitmap);

                if (scaledSize <= maxSizeInBytes)
                    return scaledBitmap;
            }
            catch
            {
                // ignored
            }

            // 避免阻塞 UI 线程
            await Task.Yield();
        }

        return null;
    }

    /// <summary>
    ///     获取位图在内存中的大小（字节）
    /// </summary>
    /// <param name="bitmap">位图对象</param>
    /// <returns>文件大小（字节）</returns>
    public static long GetBitmapSize(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms);

        return ms.Length;
    }
}
