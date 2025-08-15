using QwQ_Music.Common.Manager;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public class SoundEffectConfigViewModel() : NavigationViewModel("音效")
{
    public AudioModifierConfig AudioModifierConfig { get; } = ConfigManager.AudioModifierConfig;

    public static MusicPlayerViewModel MusicPlayerViewModel => MusicPlayerViewModel.Default;

    /*
    public float SpatialAngle
    {
        get => AudioModifierConfig.SpatialModifier.Angle;
        set
        {
            AudioModifierConfig.SpatialModifier.Angle = value;
            OnPropertyChanged();
        }
    }

    public float SpatialDistance
    {
        get => AudioModifierConfig.SpatialModifier.Distance;
        set
        {
            AudioModifierConfig.SpatialModifier.Distance = value;
            OnPropertyChanged();
        }
    }

    public List<EqualizerBand> ParametricEqualizerBands
    {
        get => AudioModifierConfig.ParametricEqualizer.Bands;
        set
        {
            AudioModifierConfig.ParametricEqualizer.Bands = value;
            OnPropertyChanged();
        }
    }

    public int NoiseReductionFftSize
    {
        get => (int)Math.Log(AudioModifierConfig.NoiseReductionModifier.FftSize, 2);
        set
        {
            AudioModifierConfig.NoiseReductionModifier.FftSize = (int)Math.Pow(2, value);
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private void RestoreDefaultEqualizer()
    {
        var temp = new List<EqualizerBand>(ParametricEqualizerBands);
        foreach (var equalizerBand in temp)
        {
            equalizerBand.GainDb = 0f;
        }
        ParametricEqualizerBands = temp;
    }*/
}
