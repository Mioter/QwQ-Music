using System;
using System.Globalization;
using Avalonia.Data.Converters;
using QwQ_Music.Utilities;

namespace QwQ_Music.Converter;

public class SecondsToStringConverter : IValueConverter
{

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double seconds) throw new NotSupportedException();

        if (parameter is int paramInt)
            return seconds.FormatSeconds(paramInt);

        return seconds.FormatSeconds(0);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
