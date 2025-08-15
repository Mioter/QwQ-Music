using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace QwQ_Music.UI.Converters;

public class ScrollOffsetConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Vector offset) return false;

        string? param = parameter?.ToString()?.ToLower();

        return param switch
        {
            "istop" => Math.Abs(offset.Y) < 0.001,
            "nottop" => Math.Abs(offset.Y) >= 0.001 // 需要更多信息才能实现
            ,
            "isleft" => Math.Abs(offset.X) < 0.001,
            "notright" => Math.Abs(offset.X) >= 0.001,
            _ => Math.Abs(offset.Y) < 0.001,
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}
