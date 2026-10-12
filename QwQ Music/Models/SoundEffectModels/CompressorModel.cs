using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Interfaces;
using SoundFlow.Abstracts;
using SoundFlow.Modifiers;
using SoundFlow.Structs;

namespace QwQ_Music.Models.SoundEffectModels;

public class CompressorModel : ObservableObject, ISoundModifierModel<CompressorModifier> {
    /// <summary>
    ///     获取或设置阈值（dBFS）。
    /// </summary>
    public float ThresholdDb {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.ThresholdDb = value;
        }
    } = -20f;

    /// <summary>
    ///     获取或设置压缩比。
    /// </summary>
    public float Ratio {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.Ratio = value;
        }
    } = 4f;

    /// <summary>
    ///     获取或设置启动时间（毫秒）。
    /// </summary>
    public float AttackMs {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.AttackMs = value;
        }
    } = 20f;

    /// <summary>
    ///     获取或设置释放时间（毫秒）。
    /// </summary>
    public float ReleaseMs {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.ReleaseMs = value;
        }
    } = 100f;

    /// <summary>
    ///     获取或设置膝部宽度（dB）。
    /// </summary>
    public float KneeDb {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.KneeDb = value;
        }
    } = 6f;

    /// <summary>
    ///     获取或设置补偿增益（dB）。
    /// </summary>
    public float MakeupGainDb {
        get;
        set {
            if (SetProperty(ref field, value))
                Modifier?.MakeupGainDb = value;
        }
    } = 0f;

    [JsonIgnore]
    public CompressorModifier? Modifier { get; private set; }

    [JsonIgnore]
    SoundModifier? ISoundModifierModel.Modifier => Modifier;

    [JsonIgnore]
    public string Name { get; } = "压缩器";

    public void Initialize(AudioFormat audioFormat) {
        var modifier = new CompressorModifier(
            audioFormat,
            ThresholdDb,
            Ratio,
            AttackMs,
            ReleaseMs,
            KneeDb,
            MakeupGainDb) { Enabled = Enabled };

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