using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Helper;
using SoundFlow.Modifiers;

namespace QwQ_Music.Models.ConfigModel;

public partial class AudioModifierConfig : ObservableObject
{
    public ReplayGainModifier ReplayGainModifier { get; set; } = new();

    public AlgorithmicReverbModifier ReverbModifier { get; set; } = new();

    public DelayModifier DelayModifier { get; set; } = new();

    public FadeModifier FadeModifier { get; set; } = new();

    public RotatingModifier RotatingModifier { get; set; } = new();

    public SpatialModifier SpatialModifier { get; set; } = new();

    public CompressorModifier CompressorModifier { get; set; } = new();

    public StereoEnhancementModifier StereoEnhancementModifier { get; set; } = new();

    public TremoloModifier TremoloModifier { get; set; } = new();

    public DistortionModifier DistortionModifier { get; set; } = new();

    public ParametricEqualizer ParametricEqualizer { get; set; } = new();

    public NoiseReductionModifier NoiseReductionModifier { get; set; } = new();

    public static TremoloModifier.TremoloWaveform[] TremoloWaveforms { get; set; } =
        EnumHelper<TremoloModifier.TremoloWaveform>.ToArray();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AudioModifierConfig))]
internal partial class SoundEffectConfigModelJsonSerializerContext : JsonSerializerContext;
