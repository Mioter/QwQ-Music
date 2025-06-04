using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Controls;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.ViewModels.ViewModelBases;
using SoundFlow.Modifiers;

namespace QwQ_Music.ViewModels;

public partial class SoundEffectConfigViewModel() : NavigationViewModel("音效")
{
    public AudioModifierConfig AudioModifierConfig { get; } = ConfigInfoModel.AudioModifierConfig;

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

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
