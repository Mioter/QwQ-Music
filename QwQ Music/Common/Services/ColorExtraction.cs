using System.Numerics;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Impressionist.Abstractions;
using Impressionist.Implementations;
using Color = Avalonia.Media.Color;

namespace QwQ_Music.Common.Services;

/// <summary>
///     颜色提取算法枚举
/// </summary>
public enum ColorExtractionAlgorithm {
    /// <summary>
    ///     K-means 聚类算法 —— 精确取色
    /// </summary>
    KMeans,

    /// <summary>
    ///     八叉树算法 —— 快速取色
    /// </summary>
    OctTree
}

/// <summary>
///     颜色提取服务类
/// </summary>
public static class ColorExtraction {
    /// <summary>
    ///     从位图对象获取调色板
    /// </summary>
    /// <param name="bitmap">位图对象</param>
    /// <param name="colorCount">要提取的颜色数量，默认为5</param>
    /// <param name="algorithm">颜色提取算法，默认为KMeans</param>
    /// <param name="ignoreWhite">忽略白色</param>
    /// <param name="toLab">转化为Lab矢量</param>
    /// <param name="useKMeansPp">使用KMeansPp</param>
    /// <returns>提取的颜色列表</returns>
    /// <remarks>
    ///     注意：<c>toLab</c> 与 <c>useKMeansPp</c> 仅在 <c>KMeans</c> 下有效
    /// </remarks>
    public static async Task<Color[]> GetColorPaletteFromBitmapAsync(
        Bitmap bitmap,
        int colorCount = 5,
        ColorExtractionAlgorithm algorithm = ColorExtractionAlgorithm.KMeans,
        bool ignoreWhite = true,
        bool toLab = true,
        bool useKMeansPp = true) {
        // 从位图采样颜色
        Dictionary<Vector3, int> sampledColors = SampleColorsFromBitmap(bitmap);

        // 根据选择的算法生成调色板
        PaletteResult? paletteResult = algorithm switch {
            ColorExtractionAlgorithm.KMeans => await PaletteGenerators.KMeansPaletteGenerator
                                                                      .CreatePalette(
                                                                          sampledColors,
                                                                          colorCount,
                                                                          ignoreWhite,
                                                                          toLab,
                                                                          useKMeansPp)
                                                                      .ConfigureAwait(false),


            ColorExtractionAlgorithm.OctTree => await PaletteGenerators.OctTreePaletteGenerator
                                                                       .CreatePalette(
                                                                           sampledColors,
                                                                           colorCount,
                                                                           ignoreWhite)
                                                                       .ConfigureAwait(false),


            _ => throw new ArgumentException("不支持的颜色提取算法", nameof(algorithm))
        };

        // 将结果转换回Avalonia颜色格式
        return paletteResult.Palette.Select(v => Color.FromRgb((byte)v.X, (byte)v.Y, (byte)v.Z)).ToArray();
    }

    /// <summary>
    ///     从位图采样颜色
    /// </summary>
    /// <param name="bitmap">位图对象</param>
    /// <returns>颜色频率字典</returns>
    private static Dictionary<Vector3, int> SampleColorsFromBitmap(Bitmap bitmap) {
        if (bitmap.Format != PixelFormat.Bgra8888)
            throw new FormatException("requiring a Bgra8888 format writeable bitmap.");

        Dictionary<Vector3, int> colorFrequencies = new();
        int width = bitmap.PixelSize.Width;
        int height = bitmap.PixelSize.Height;

        // 计算采样步长
        int stride = (width * height) switch {
            // 根据图像大小动态调整采样率
            >= 1024 * 1024 => 64, // 大图像使用较大步长
            >= 512 * 512   => 16, // 中等图像使用中等步长
            _              => 4   // 小图像使用较小步长
        };

        // 使用WriteableBitmap来访问像素数据
        using var writeableBitmap = new WriteableBitmap(
            bitmap.PixelSize,
            bitmap.Dpi,
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using ILockedFramebuffer buffer = writeableBitmap.Lock();
        bitmap.CopyPixels(buffer);

        // 遍历像素采样颜色
        for (int y = 0; y < height; y += stride)
        for (int x = 0; x < width; x += stride) {
            uint pixel = buffer.GetPixel(x, y);
            var vector = new Vector3(
                (byte)(((pixel >> 16) & 255) / 16 * 16),
                (byte)(((pixel >> 8) & 255) / 16 * 16),
                (byte)((pixel & 255) / 16 * 16));
            // 更新颜色频率
            colorFrequencies[vector] = colorFrequencies.TryGetValue(vector, out int v) ? v + 1 : 1;
        }

        return colorFrequencies;
    }

    private static unsafe uint GetPixel(this ILockedFramebuffer framebuffer, int x, int y) {
        byte* zero = (byte*)framebuffer.Address;
        int offset = y * framebuffer.RowBytes + x * 4;
        return *(uint*)(zero + offset);
    }
}