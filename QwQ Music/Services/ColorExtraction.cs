using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Avalonia.Media.Color;

namespace QwQ_Music.Services;

public static class ColorExtraction
{
    // 新增枚举类型，用于控制提取颜色类型
    public enum ColorTone
    {
        Any, // 任意颜色
        Bright, // 仅提取亮色
        Dark, // 仅提取暗色
    }

    public static List<Color> GetColorPalette(
        string imagePath,
        int colorCount = 5,
        ColorTone tone = ColorTone.Any,
        double minColorDistance = 50.0
    ) // 新增颜色差异阈值参数（基于RGB欧氏距离）
    {
        using var image = Image.Load<Rgba32>(imagePath);
        var frequencies = new Dictionary<Color, int>();

        // 采样像素并量化颜色
        for (int x = 0; x < image.Width; x += 3)
        {
            for (int y = 0; y < image.Height; y += 3)
            {
                var pixel = image[x, y];
                if (pixel.A < 200)
                    continue;

                // 量化颜色以减少噪点
                var quantized = Color.FromArgb(
                    255,
                    (byte)(pixel.R / 32 * 32),
                    (byte)(pixel.G / 32 * 32),
                    (byte)(pixel.B / 32 * 32)
                );

                // 根据亮度过滤
                if (IsColorMatchTone(quantized, tone))
                {
                    frequencies[quantized] = frequencies.TryGetValue(quantized, out int v) ? v + 1 : 1;
                }
            }
        }

        // 按频率排序并去重相似颜色
        return frequencies
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .Distinct(new ColorComparer(minColorDistance))
            .Take(colorCount)
            .ToList();
    }

    // 判断颜色是否符合亮度要求
    private static bool IsColorMatchTone(Color color, ColorTone tone)
    {
        if (tone == ColorTone.Any)
            return true;

        // 计算颜色亮度（公式：0.299*R + 0.587*G + 0.114*B）
        double brightness = (color.R * 0.299 + color.G * 0.587 + color.B * 0.114) / 255.0;

        return tone switch
        {
            ColorTone.Bright => brightness >= 0.7, // 亮度≥70%为亮色
            ColorTone.Dark => brightness <= 0.3, // 亮度≤30%为暗色
            _ => true,
        };
    }

    // 颜色比较器（用于去重相似颜色）
    private class ColorComparer(double minDistance) : IEqualityComparer<Color>
    {
        public bool Equals(Color a, Color b) => CalculateColorDistance(a, b) < minDistance;

        public int GetHashCode(Color color) => color.GetHashCode();

        // 计算RGB颜色差异（欧氏距离）
        private static double CalculateColorDistance(Color c1, Color c2)
        {
            double dr = c1.R - c2.R;
            double dg = c1.G - c2.G;
            double db = c1.B - c2.B;
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }
    }
}
