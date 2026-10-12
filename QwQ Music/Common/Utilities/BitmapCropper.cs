using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace QwQ_Music.Common.Utilities;

public static class BitmapCropper {
    /// <summary>
    ///     裁剪图片
    /// </summary>
    /// <param name="source">原始Bitmap</param>
    /// <param name="targetAspectRatio">目标宽高比（如16/9, 1.0等）</param>
    /// <param name="centerX">裁剪中心点X（0~1）</param>
    /// <param name="centerY">裁剪中心点Y（0~1）</param>
    /// <returns>裁剪后的Bitmap</returns>
    public static void Crop(ref Bitmap source, double targetAspectRatio, double centerX = 0.5, double centerY = 0.5) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetAspectRatio);
        var old = source;
        centerX = Math.Clamp(centerX, 0, 1);
        centerY = Math.Clamp(centerY, 0, 1);

        int srcWidth = old.PixelSize.Width;
        int srcHeight = old.PixelSize.Height;
        double srcAspect = (double)srcWidth / srcHeight;

        int cropWidth, cropHeight;

        // 计算裁剪区域尺寸
        if (srcAspect > targetAspectRatio) {
            // 原图更宽，裁剪宽度
            cropHeight = srcHeight;
            cropWidth = (int)Math.Round(cropHeight * targetAspectRatio);
        } else {
            // 原图更高，裁剪高度
            cropWidth = srcWidth;
            cropHeight = (int)Math.Round(cropWidth / targetAspectRatio);
        }

        // 计算裁剪区域左上角坐标
        int x = (int)Math.Round(centerX * srcWidth - cropWidth / 2.0);
        int y = (int)Math.Round(centerY * srcHeight - cropHeight / 2.0);

        // 保证不超出边界
        x = Math.Clamp(x, 0, srcWidth - cropWidth);
        y = Math.Clamp(y, 0, srcHeight - cropHeight);

        WriteableBitmap result = new(new PixelSize(cropWidth, cropHeight), old.Dpi, old.Format, old.AlphaFormat);
        using ILockedFramebuffer buffer = result.Lock();
        old.CopyPixels(new PixelRect(x, y, cropWidth, cropHeight), buffer.Address, buffer.RowBytes*cropHeight, buffer.RowBytes);
        source = result;
        old.Dispose();
    }
}