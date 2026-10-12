using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Interfaces;
using SoundFlow.Abstracts;
using SoundFlow.Modifiers;
using SoundFlow.Structs;

namespace QwQ_Music.Models.SoundEffectModels;

public partial class ChorusModel : ObservableObject, ISoundModifierModel<ChorusModifier> {
    /// <summary>
    ///     合唱效果的深度（毫秒）。
    /// </summary>
    public float DepthMs {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.DepthMs = value;
        }
    } = 3f;

    /// <summary>
    ///     LFO 调制速率（Hz）。
    /// </summary>
    public float RateHz {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.RateHz = value;
        }
    } = 1f;

    /// <summary>
    ///     反馈量（0.0 - 1.0）。
    /// </summary>
    [ObservableProperty]
    public partial float Feedback { get; set; } = 0.5f;

    /// <summary>
    ///     湿/干混合比例（0.0 - 1.0）。
    /// </summary>
    public float WetDryMix {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.WetDryMix = value;
        }
    } = 0.5f;

    [JsonIgnore]
    public ChorusModifier? Modifier { get; private set; }

    [JsonIgnore]
    SoundModifier? ISoundModifierModel.Modifier => Modifier;

    [JsonIgnore]
    public string Name { get; } = "合唱效果";

    public void Initialize(AudioFormat audioFormat) {
        var modifier = new ChorusModifier(audioFormat, DepthMs, RateHz, Feedback, WetDryMix) { Enabled = Enabled };

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