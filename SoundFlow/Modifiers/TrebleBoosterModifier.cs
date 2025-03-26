using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 高音增强器（使用共振高通滤波器）<br />
/// Boosts treble frequencies using a resonant high-pass filter.
/// </summary>
public class TrebleBoosterModifier : SoundModifier
{
    private readonly float[] _hpState;
    private readonly float[] _previousInput;
    private float _boostGainDb;

    /// <summary>
    /// 创建高音增强器实例<br />
    /// Initializes a new instance of the <see cref="TrebleBoosterModifier"/> class.
    /// </summary>
    public TrebleBoosterModifier()
    {
        Cutoff = 4000f;
        BoostGainDb = 6f;
        _hpState = new float[AudioEngine.Channels];
        _previousInput = new float[AudioEngine.Channels];
    }

    /// <summary>
    /// 截止频率（赫兹）<br />
    /// The cutoff frequency of the high-pass filter.
    /// </summary>
    /// <value>
    /// 有效范围：20Hz - 20kHz<br />
    /// Valid range: 20Hz - 20kHz
    /// </value>
    public float Cutoff { get; set; }

    /// <summary>
    /// 增益量（分贝）<br />
    /// The gain of the treble boost.
    /// </summary>
    /// <value>
    /// 建议范围：0dB - 12dB<br />
    /// Recommended range: 0dB - 12dB
    /// </value>
    public float BoostGainDb
    {
        get => _boostGainDb;
        set => _boostGainDb = Math.Clamp(value, 0f, 12f);
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        // 参数验证和转换
        // Parameter validation and conversion
        float validCutoff = Math.Clamp(Cutoff, 20f, 20000f);
        float linearGain = MathF.Pow(10, BoostGainDb / 20f);

        // 计算滤波器系数
        // Calculate filter coefficients
        float dt = AudioEngine.Instance.InverseSampleRate;
        float rc = 1f / (2 * MathF.PI * validCutoff);
        float alpha = rc / (rc + dt);

        // 高通滤波处理
        // High-pass filtering
        float hp = alpha * (_hpState[channel] + sample - _previousInput[channel]);
        _hpState[channel] = hp;
        _previousInput[channel] = sample;

        // 混合增强信号
        // Mix boosted signal
        return sample + hp * linearGain;
    }
}