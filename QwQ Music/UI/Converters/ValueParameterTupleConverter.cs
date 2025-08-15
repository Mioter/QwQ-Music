using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QwQ_Music.UI.Converters;

public class ValueParameterTupleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null
            ? parameter ?? null
            : parameter == null
                ? value
                : (value, parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}
