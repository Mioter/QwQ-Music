using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using QwQ_Music.Services;

namespace QwQ_Music.Helper.Converter;

public class AlbumImageConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Bitmap? image = null;
        if (value is string coverPath && !string.IsNullOrEmpty(coverPath))
        {
            image = parameter is "NDT"
                ? MusicExtractor.LoadOriginalBitmap(coverPath)
                : MusicExtractor.LoadCompressedBitmapFromCache(coverPath);
        }
        return image ?? new Bitmap(AssetLoader.Open(new Uri("avares://QwQ Music/Assets/Images/看我.png")));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
