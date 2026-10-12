using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Interfaces;
using SoundFlow.Abstracts;
using SoundFlow.Modifiers;
using SoundFlow.Structs;

namespace QwQ_Music.Models.SoundEffectModels;

public class DelayModel : ObservableObject, ISoundModifierModel<DelayModifier> {
    /// <summary>
    ///     获取或设置延迟时间（毫秒） (>= 0)。
    /// </summary>
    public float DelayTimeMs {
        get;
        set => SetProperty(ref field, Math.Max(0, value));
    } = 500f;

    /// <summary>
    ///     获取或设置反馈量（0.0 - 1.0）。
    /// </summary>
    public float Feedback {
        get;
        set {
            float clampedValue = Math.Clamp(value, 0f, 1f);

            if (SetProperty(ref field, clampedValue))
                Modifier?.Feedback = clampedValue;
        }
    } = 0.5f;

    /// <summary>
    ///     获取或设置湿声混合量（0.0 - 1.0）。
    /// </summary>
    public float WetMix {
        get;
        set {
            float clampedValue = Math.Clamp(value, 0f, 1f);

            if (SetProperty(ref field, clampedValue))
                Modifier?.WetMix = clampedValue;
        }
    } = 0.3f;

    /// <summary>
    ///     获取或设置截止频率（Hz）。
    /// </summary>
    public float Cutoff {
        get;
        set {
            float clampedValue = Math.Max(20f, value); // 最小20Hz

            if (SetProperty(ref field, clampedValue))
                Modifier?.Cutoff = clampedValue;
        }
    } = 5000f;

    [JsonIgnore]
    public DelayModifier? Modifier { get; private set; }

    [JsonIgnore]
    SoundModifier? ISoundModifierModel.Modifier => Modifier;

    [JsonIgnore]
    public string Name { get; } = "延迟效果";

    public void Initialize(AudioFormat audioFormat) {
        var modifier = new DelayModifier(
            audioFormat,
            (int)(DelayTimeMs * 0.001f * audioFormat.SampleRate), // 将毫秒转换为采样数
            Feedback,
            WetMix,
            Cutoff) { Enabled = Enabled };

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