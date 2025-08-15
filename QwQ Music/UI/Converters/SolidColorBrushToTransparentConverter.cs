using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace QwQ_Music.UI.Converters;

public class SolidColorBrushToTransparentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 检查输入值是否为 SolidColorBrush
        if (value is not SolidColorBrush brush)
            return AvaloniaProperty.UnsetValue;

        // 获取原始颜色
        var originalColor = brush.Color;

        double transparency;

        if (parameter is string str && double.TryParse(str, out double d))
            transparency = d;
        else
            transparency = 0.2; // 默认透明度设置为 20% (0.2f)

        // 创建一个新的颜色
        var newColor = Color.FromArgb((byte)(transparency * 255), originalColor.R, originalColor.G, originalColor.B);

        // 返回一个新的 SolidColorBrush
        return new SolidColorBrush(newColor);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 通常不需要反向转换，直接返回未设置值
        return AvaloniaProperty.UnsetValue;
    }
}
