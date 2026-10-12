using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Audio.SoundModifier;
using QwQ_Music.Models.SoundEffectModels;

namespace QwQ_Music.Models.ConfigModels;

public class SoundModifierConfig : ObservableObject {
    public PlayComponent PlayComponent { get; set; } = new();

    public SoundEffectConfig SoundEffectConfig { get; set; } = new();
}

public class PlayComponent {
    public ReplayGainModifier ReplayGainModifier { get; set; } = new();

    public FadeModifier FadeModifier { get; set; } = new();
}

public class SoundEffectConfig {
    public Dictionary<string, bool> BuiltInSoundEffects { get; set; } = [];

    public AlgorithmicReverbModel AlgorithmicReverb { get; set; } = new();

    public BassBoosterModel BassBooster { get; set; } = new();

    public ChorusModel Chorus { get; set; } = new();

    public CompressorModel Compressor { get; set; } = new();

    public DelayModel Delay { get; set; } = new();
}