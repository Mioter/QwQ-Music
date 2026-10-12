using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace QwQ_Music.UI.Converters;

public class SoundEffectsNameConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value switch {
            "AlgorithmicReverb"   => "混响",
            "BassBooster"         => "低音增强",
            "Chorus"              => "合唱",
            "Compressor"          => "压缩器",
            "Delay"               => "延迟",
            "FrequencyBand"       => "限频器",
            "ParametricEqualizer" => "均衡器",
            "TrebleBooster"       => "高音增强",
            _                     => value
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return AvaloniaProperty.UnsetValue;
    }
}