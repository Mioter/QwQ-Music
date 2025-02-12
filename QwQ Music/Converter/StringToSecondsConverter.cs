using System;
using System.Globalization;
using Avalonia.Data.Converters;
using QwQ_Music.Utilities;

namespace QwQ_Music.Converter;

public class StringToSecondsConverter : IValueConverter
{

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string timeString) throw new ArgumentException("Invalid time format", nameof(value));

        return timeString.ParseSeconds();

    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
