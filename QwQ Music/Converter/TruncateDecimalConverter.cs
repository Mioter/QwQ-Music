using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QwQ_Music.Converter;

public class TruncateDecimalConverter : IValueConverter
{

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string duration || string.IsNullOrWhiteSpace(duration)) return value;
        int dotIndex = duration.IndexOf('.');
        return dotIndex != -1
            ?
            // 切割掉小数点及其后面的内容
            duration[..dotIndex]
            : duration;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
