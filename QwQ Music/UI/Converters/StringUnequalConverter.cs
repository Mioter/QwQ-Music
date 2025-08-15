using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QwQ_Music.UI.Converters;

public class StringUnequalConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string? valueStr = value?.ToString();
        string? parameterStr = parameter?.ToString();

        return valueStr != parameterStr;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}
