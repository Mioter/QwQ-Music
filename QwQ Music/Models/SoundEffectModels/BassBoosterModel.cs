using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Interfaces;
using SoundFlow.Abstracts;
using SoundFlow.Modifiers;
using SoundFlow.Structs;

namespace QwQ_Music.Models.SoundEffectModels;

public class BassBoosterModel : ObservableObject, ISoundModifierModel<BassBoosterModifier> {
    /// <summary>
    ///     获取或设置截止频率（Hz）。
    /// </summary>
    public float Cutoff {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.Cutoff = value;
        }
    }

    /// <summary>
    ///     获取或设置以增益（dB）。
    /// </summary>
    public float BoostGainDb {
        get;
        set {
            if (SetProperty(ref field, value))
                // Modifier?.BoostGain = value;
                Modifier?.BoostGainDb = value;
        }
    }

    [JsonIgnore]
    public BassBoosterModifier? Modifier { get; private set; }

    [JsonIgnore]
    SoundModifier? ISoundModifierModel.Modifier => Modifier;

    [JsonIgnore]
    public string Name { get; } = "低音增强";

    public void Initialize(AudioFormat audioFormat) {
        var modifier = new BassBoosterModifier(audioFormat) {
            Cutoff = Cutoff, BoostGainDb = BoostGainDb
            // BoostGain = BoostGainDb
        };

        Modifier = modifier;
    }

    public void Revoke() { Modifier = null; }

    public bool Enabled {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.Enabled = value;
        }
    }
}