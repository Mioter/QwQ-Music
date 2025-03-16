using System;
using Avalonia.Media;

namespace QwQ_Music.Utilities;

public static class ColorCalculator
{
    public static Color AdjustColorForTheme(Color color, bool isNightMode)
    {
        // 将 Avalonia 的 Color 转换为 HSL 值
        (double h, double s, double l) = RgbToHsl(color.R, color.G, color.B);

        // 根据主题调整亮度
        l = isNightMode
            ? Math.Min(l, 0.3) // 夜间模式：亮度不超过30%
            : Math.Max(l, 0.8); // 日间模式：亮度不低于80%

        // 转换回 RGB 并保留原始透明度
        (byte r, byte g, byte b) = HslToRgb(h, s, l);
        return Color.FromArgb(color.A, r, g, b);
    }

    // RGB 转 HSL 的算法实现
    private static (double h, double s, double l) RgbToHsl(byte red, byte green, byte blue)
    {
        double r = red / 255.0;
        double g = green / 255.0;
        double b = blue / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double h = 0,
            s = 0,
            l = (max + min) / 2;

        if (delta != 0)
        {
            s = l < 0.5 ? delta / (max + min) : delta / (2 - max - min);

            const double epsilon = 1e-6;
            if (Math.Abs(max - r) < epsilon)
            {
                h = (g - b) / delta + (g < b ? 6 : 0);
            }
            else if (Math.Abs(max - g) < epsilon)
            {
                h = (b - r) / delta + 2;
            }
            else if (Math.Abs(max - b) < epsilon)
            {
                h = (r - g) / delta + 4;
            }

            h /= 6;
        }

        return (h, s, l);
    }

    // HSL 转 RGB 的算法实现
    private static (byte r, byte g, byte b) HslToRgb(double h, double s, double l)
    {
        double r,
            g,
            b;

        if (s == 0)
        {
            r = g = b = l; // 灰色
        }
        else
        {
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;

            r = HueToRgb(p, q, h + 1.0 / 3);
            g = HueToRgb(p, q, h);
            b = HueToRgb(p, q, h - 1.0 / 3);
        }

        return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0)
            t += 1;
        if (t > 1)
            t -= 1;

        if (t < 1.0 / 6)
            return p + (q - p) * 6 * t;
        if (t < 1.0 / 2)
            return q;
        if (t < 2.0 / 3)
            return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }
}
