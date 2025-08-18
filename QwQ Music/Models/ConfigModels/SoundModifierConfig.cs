using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Audio.SoundModifier;

namespace QwQ_Music.Models.ConfigModels;

public class SoundModifierConfig : ObservableObject
{
    public ReplayGainModifier ReplayGainModifier { get; set; } = new();
    
    public FadeModifier FadeModifier { get; set; } = new();
}
