using System;
using System.Globalization;
using Avalonia.Data.Converters;
using QwQ_Music.Common;

namespace QwQ_Music.Converter;

public class AlbumImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string albumImageIndex)
        {
            return MusicExtractor.LoadAlbumImageFromCache(albumImageIndex);
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
