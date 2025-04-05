using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 频段处理器（带通滤波）<br />
/// A sound modifier that implements a frequency band modifier.
/// </summary>
public class FrequencyBandModifier : SoundModifier
{
    private readonly LowPassFilter _lowPass;
    private readonly HighPassFilter _highPass;

    /// <summary>
    /// 创建频段处理器实例<br />
    /// Constructs a new instance of <see cref="FrequencyBandModifier"/>
    /// </summary>
    public FrequencyBandModifier()
    {
        // 默认设置：20Hz - 20kHz
        // Default settings: 20Hz - 20kHz
        _highPass = new HighPassFilter(20f);
        _lowPass = new LowPassFilter(20000f);
    }

    /// <summary>
    /// 高频截止频率（赫兹）<br />
    /// The high cutoff frequency in Hertz.
    /// </summary>
    /// <value>
    /// 取值范围：0.0 至 <see cref="AudioEngine.SampleRate"/><br />
    /// This value ranges from 0.0 to <see cref="AudioEngine.SampleRate"/>
    /// </value>
    public float HighCutoffFrequency
    {
        get => _lowPass.CutoffFrequency;
        set
        {
            // 确保高频不低于低频
            // Ensure high frequency is not lower than low frequency
            if (value > LowCutoffFrequency)
            {
                _lowPass.CutoffFrequency = Math.Min(value, AudioEngine.Instance.SampleRate / 2.0f);
            }
        }
    }

    /// <summary>
    /// 低频截止频率（赫兹）<br />
    /// The low cutoff frequency in Hertz.
    /// </summary>
    /// <value>
    /// 取值范围：0.0 至 <see cref="AudioEngine.SampleRate"/><br />
    /// This value ranges from 0.0 to <see cref="AudioEngine.SampleRate"/>
    /// </value>
    public float LowCutoffFrequency
    {
        get => _highPass.CutoffFrequency;
        set
        {
            // 确保低频不高于高频
            // Ensure low frequency is not higher than high frequency
            if (value < HighCutoffFrequency)
            {
                _highPass.CutoffFrequency = Math.Max(
                    value,
                    20f // 最低有效频率
                );
            }
        }
    }

    /// <inheritdoc/>
    public override float ProcessSample(float sample, int channel)
    {
        // 先进行高通滤波再进行低通滤波
        // Apply high-pass first then low-pass
        sample = _highPass.ProcessSample(sample, channel);
        return _lowPass.ProcessSample(sample, channel);
    }
}
