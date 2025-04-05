using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Controls;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using SoundFlow.Modifiers;

namespace QwQ_Music.ViewModels;

public partial class SoundEffectConfigViewModel() : NavigationViewModel("音效")
{
    public SoundEffectConfig SoundEffectConfig { get; } = ConfigInfoModel.SoundEffectConfig;

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public float SpatialAngle
    {
        get => SoundEffectConfig.SpatialModifier.Angle;
        set
        {
            SoundEffectConfig.SpatialModifier.Angle = value;
            OnPropertyChanged();
        }
    }

    public float SpatialDistance
    {
        get => SoundEffectConfig.SpatialModifier.Distance;
        set
        {
            SoundEffectConfig.SpatialModifier.Distance = value;
            OnPropertyChanged();
        }
    }

    public List<EqualizerBand> ParametricEqualizerBands
    {
        get => SoundEffectConfig.ParametricEqualizer.Bands;
        set
        {
            SoundEffectConfig.ParametricEqualizer.Bands = value;
            OnPropertyChanged();
        }
    }

    public int NoiseReductionFftSize
    {
        get => (int)Math.Log(SoundEffectConfig.NoiseReductionModifier.FftSize, 2);
        set
        {
            SoundEffectConfig.NoiseReductionModifier.FftSize = (int)Math.Pow(2, value);
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private void OnSpeakerPositionChanged(PositionChangedEventArgs e)
    {
        SpatialAngle = (float)e.Angle;
        SpatialDistance = (float)e.Distance;
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
    }
}
