using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Impressionist.Implementations;
using Color = Avalonia.Media.Color;

namespace QwQ_Music.Services;

/// <summary>
/// 颜色提取算法枚举
/// </summary>
public enum ColorExtractionAlgorithm
{
    /// <summary>
    /// K-means 聚类算法 —— 精确取色
    /// </summary>
    KMeans,

    /// <summary>
    /// 八叉树算法 —— 快速取色
    /// </summary>
    OctTree,
}

/// <summary>
/// 颜色提取服务类
/// </summary>
public static class ColorExtraction
{
    /// <summary>
    /// 从位图对象获取调色板
    /// </summary>
    /// <param name="bitmap">位图对象</param>
    /// <param name="colorCount">要提取的颜色数量，默认为5</param>
    /// <param name="algorithm">颜色提取算法，默认为KMeans</param>
    /// <param name="ignoreWhite">忽略白色</param>
    /// <returns>提取的颜色列表</returns>
    public static List<Color> GetColorPaletteFromBitmap(
        Bitmap bitmap,
        int colorCount = 5,
        ColorExtractionAlgorithm algorithm = ColorExtractionAlgorithm.KMeans,
        bool ignoreWhite = true
    )
    {
        // 从位图采样颜色
        var sampledColors = SampleColorsFromBitmap(bitmap);
        var vectorColors = ConvertToVectorColors(sampledColors);

        // 根据选择的算法生成调色板
        var paletteResult = algorithm switch
        {
            ColorExtractionAlgorithm.KMeans => PaletteGenerators
                .KMeansPaletteGenerator.CreatePalette(
                    vectorColors,
                    colorCount,
                    ignoreWhite,
                    toLab: true,
                    useKMeansPp: true
                )
                .GetAwaiter()
                .GetResult(),

            ColorExtractionAlgorithm.OctTree => PaletteGenerators
                .OctTreePaletteGenerator.CreatePalette(vectorColors, colorCount, ignoreWhite)
                .GetAwaiter()
                .GetResult(),

            _ => throw new ArgumentException("不支持的颜色提取算法", nameof(algorithm)),
        };

        // 将结果转换回Avalonia颜色格式
        return paletteResult.Palette.Select(v => Color.FromRgb((byte)v.X, (byte)v.Y, (byte)v.Z)).ToList();
    }

    /// <summary>
    /// 从位图采样颜色
    /// </summary>
    /// <param name="bitmap">位图对象</param>
    /// <returns>颜色频率字典</returns>
    private static Dictionary<Color, int> SampleColorsFromBitmap(Bitmap bitmap)
    {
        var colorFrequencies = new Dictionary<Color, int>();
        int width = bitmap.PixelSize.Width;
        int height = bitmap.PixelSize.Height;

        // 计算采样步长
        int sampleStep = CalculateSampleStep(width, height);

        // 使用WriteableBitmap来访问像素数据
        using var writeableBitmap = new WriteableBitmap(
            bitmap.PixelSize,
            bitmap.Dpi,
            PixelFormat.Bgra8888,
            AlphaFormat.Premul
        );

        using var fb = writeableBitmap.Lock();
        unsafe
        {
            byte* pixelData = (byte*)fb.Address;
            int stride = fb.RowBytes;

            // 将原始位图数据复制到可写位图
            bitmap.CopyPixels(new PixelRect(0, 0, width, height), new IntPtr(pixelData), stride * height, stride);

            // 遍历像素采样颜色
            for (int y = 0; y < height; y += sampleStep)
            {
                for (int x = 0; x < width; x += sampleStep)
                {
                    int pixelOffset = y * stride + x * 4; // BGRA格式

                    byte b = pixelData[pixelOffset];
                    byte g = pixelData[pixelOffset + 1];
                    byte r = pixelData[pixelOffset + 2];
                    byte a = pixelData[pixelOffset + 3];

                    // 忽略透明像素
                    if (a < 200)
                        continue;

                    // 量化颜色以减少噪点
                    var quantized = Color.FromArgb(255, (byte)(r / 16 * 16), (byte)(g / 16 * 16), (byte)(b / 16 * 16));

                    // 更新颜色频率
                    colorFrequencies[quantized] = colorFrequencies.TryGetValue(quantized, out int v) ? v + 1 : 1;
                }
            }
        }

        return colorFrequencies;
    }

    /// <summary>
    /// 计算最佳采样步长
    /// </summary>
    /// <param name="width">图像宽度</param>
    /// <param name="height">图像高度</param>
    /// <returns>采样步长</returns>
    private static int CalculateSampleStep(int width, int height)
    {
        int pixelCount = width * height;

        return pixelCount switch
        {
            // 根据图像大小动态调整采样率
            > 1000000 => 8, // 大图像使用较大步长
            > 250000 => 4, // 中等图像使用中等步长
            _ => 2, // 小图像使用较小步长
        };
    }

    /// <summary>
    /// 将Avalonia颜色字典转换为Vector3颜色字典
    /// </summary>
    /// <param name="colors">Avalonia颜色字典</param>
    /// <returns>Vector3颜色字典</returns>
    private static Dictionary<Vector3, int> ConvertToVectorColors(Dictionary<Color, int> colors)
    {
        var result = new Dictionary<Vector3, int>();

        foreach (var pair in colors)
        {
            var color = pair.Key;
            var vector = new Vector3(color.R, color.G, color.B);

            if (result.TryGetValue(vector, out int frequency))
            {
                result[vector] = frequency + pair.Value;
            }
            else
            {
                result[vector] = pair.Value;
            }
        }

        return result;
    }
}
