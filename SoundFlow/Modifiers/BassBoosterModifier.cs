using System;
using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 低音增强器（使用共振低通滤波器）<br />
/// Boosts bass frequencies using a resonant low-pass filter.
/// </summary>
public class BassBoosterModifier : SoundModifier
{
    private float _boostGainDb;
    private readonly float[] _lpState;
    private readonly float[] _resonanceState;

    /// <summary>
    /// 创建低音增强器实例<br />
    /// Initializes a new instance of the <see cref="BassBoosterModifier"/> class.
    /// </summary>
    public BassBoosterModifier()
    {
        Cutoff = 150f;
        BoostGainDb = 6f;
        _lpState = new float[AudioEngine.Channels];
        _resonanceState = new float[AudioEngine.Channels];
    }

    /// <summary>
    /// 截止频率（赫兹）<br />
    /// The cutoff frequency in Hertz.
    /// </summary>
    /// <value>
    /// 有效范围：20Hz - 20kHz<br />
    /// Valid range: 20Hz - 20kHz
    /// </value>
    public float Cutoff { get; set; }

    /// <summary>
    /// 增益量（分贝）<br />
    /// The boost gain in decibels.
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
        float alpha = dt / (rc + dt);

        // 低通滤波处理
        // Low-pass filtering
        _lpState[channel] += alpha * (sample - _lpState[channel]);

        // 共振反馈处理
        // Resonance feedback processing
        float feedbackFactor = 0.5f * linearGain;
        feedbackFactor = Math.Min(0.95f, feedbackFactor); // 防止振荡 / Prevent oscillation

        // 更新共振状态
        // Update resonance state
        _resonanceState[channel] = _lpState[channel] + 
                                  _resonanceState[channel] * feedbackFactor;

        // 混合原始信号和增强信号
        // Mix original and boosted signal
        return sample + _resonanceState[channel];
    }
}