using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Interfaces;
using SoundFlow.Abstracts;
using SoundFlow.Modifiers;
using SoundFlow.Structs;

namespace QwQ_Music.Models.SoundEffectModels;

public class FrequencyBandModel : ObservableObject, ISoundModifierModel<FrequencyBandModifier> {
    private float _sampleRate = 44100f; // 默认采样率

    /// <summary>
    ///     获取或设置低频截止频率（Hz）。
    /// </summary>
    public float LowCutoffFrequency {
        get;
        set {
            float clampedValue = Math.Clamp(value, 0f, _sampleRate);

            if (SetProperty(ref field, clampedValue))
                Modifier?.LowCutoffFrequency = clampedValue;
        }
    } = 200f;

    /// <summary>
    ///     获取或设置高频截止频率（Hz）。
    /// </summary>
    public float HighCutoffFrequency {
        get;
        set {
            float clampedValue = Math.Clamp(value, 0f, _sampleRate);

            if (SetProperty(ref field, clampedValue))
                Modifier?.HighCutoffFrequency = clampedValue;
        }
    } = 5000f;

    [JsonIgnore]
    public FrequencyBandModifier? Modifier { get; private set; }

    [JsonIgnore]
    SoundModifier? ISoundModifierModel.Modifier => Modifier;

    [JsonIgnore]
    public string Name { get; } = "频率带通";

    public void Initialize(AudioFormat audioFormat) {
        _sampleRate = audioFormat.SampleRate;

        var modifier =
            new FrequencyBandModifier(audioFormat, LowCutoffFrequency, HighCutoffFrequency) { Enabled = Enabled };

        Modifier = modifier;
    }

    public void Revoke() { Modifier = null; }

    /// <summary>
    ///     获取或设置是否启用效果器。
    /// </summary>
    public bool Enabled {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.Enabled = value;
        }
    } = true;
}