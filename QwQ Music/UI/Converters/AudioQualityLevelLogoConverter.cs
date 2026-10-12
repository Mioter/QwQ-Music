using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using QwQ_Music.Common.Managers;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.UI.Converters;

public class AudioQualityLevelLogoConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value is not AudioQualityLevel level ?
            null :
            CacheManager.AudioQualityLevelLogo.GetValueOrDefault(level);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return AvaloniaProperty.UnsetValue;
    }
}