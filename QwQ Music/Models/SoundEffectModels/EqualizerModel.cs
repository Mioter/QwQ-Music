using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models.ModelBase;

namespace QwQ_Music.Models.SoundEffectModels;

public partial class EqualizerModel() : EffectModelBase("Equalizer")
{
    private ObservableCollection<EqualizerConfigModel> _equalizerConfigs =
    [
        new() { FrequencyBand = 31.25f, GainValue = 0 },
        new() { FrequencyBand = 62.5f, GainValue = 0 },
        new() { FrequencyBand = 125f, GainValue = 0 }, // 低频段
        new() { FrequencyBand = 250f, GainValue = 0 },
        new() { FrequencyBand = 500f, GainValue = 0 },
        new() { FrequencyBand = 1000f, GainValue = 0 },
        new() { FrequencyBand = 2000f, GainValue = 0 }, // 中频段
        new() { FrequencyBand = 4000f, GainValue = 0 },
        new() { FrequencyBand = 8000f, GainValue = 0 },
        new() { FrequencyBand = 16000f, GainValue = 0 }, // 高频段
    ];

    public ObservableCollection<EqualizerConfigModel> EqualizerConfigs
    {
        get => _equalizerConfigs;
        set
        {
            if (SetProperty(ref _equalizerConfigs, value))
            {
                UpdateEqualizerEffectConfig();
            }
        }
    }

    private void UpdateEqualizerEffectConfig()
    {
        var bands = EqualizerConfigs
            .Select(config => (frequency: config.FrequencyBand, gain: (float)config.GainValue))
            .ToArray();
        UpdateParameter("Bands", bands);
    }

    [RelayCommand]
    private void RestoreDefaultEqualizer()
    {
        foreach (var config in EqualizerConfigs)
        {
            config.GainValue = 0.0f;
        }

        UpdateEqualizerEffectConfig();
    }

    [RelayCommand]
    private void EqualizerValueChanged() => UpdateEqualizerEffectConfig();

    protected override void UpdateAllParameter()
    {
        base.UpdateAllParameter();
        UpdateEqualizerEffectConfig();
    }
}

public class EqualizerConfigModel : ObservableObject
{
    public float FrequencyBand { get; init; }

    private double _gainValue;
    public double GainValue
    {
        get => _gainValue;
        set => SetProperty(ref _gainValue, value);
    }
}
