using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 延迟效果修饰器，实现音频延迟效果<br />
/// A sound modifier that implements a delay effect
/// </summary>
public sealed class DelayModifier : SoundModifier
{
    private readonly List<float[]> _delayLines;
    private readonly int[] _delayIndices;
    private readonly float[] _filterStates;
    private int _delayLengthSamples = 44100; // 默认为1秒的延迟（假设采样率为44.1kHz）

    /// <inheritdoc />
    public override string Name { get; set; } = "延迟效果";

    /// <summary>
    /// 获取或设置反馈量（0.0 - 1.0）<br />
    /// Gets or sets the feedback amount (0.0 - 1.0)
    /// </summary>
    public float Feedback { get; set; } = 0.5f;

    /// <summary>
    /// 获取或设置湿/干混合比例（0.0 - 1.0）<br />
    /// Gets or sets the wet/dry mix (0.0 - 1.0)
    /// </summary>
    public float WetMix { get; set; } = 0.3f;

    /// <summary>
    /// 获取或设置截止频率（赫兹）<br />
    /// Gets or sets the cutoff frequency in Hertz
    /// </summary>
    public float Cutoff { get; set; } = 5000f;

    /// <summary>
    /// 获取或设置延迟时间（毫秒）<br />
    /// Gets or sets the delay time in milliseconds
    /// </summary>
    public float DelayTimeMs
    {
        get => _delayLengthSamples * 1000f / AudioEngine.Instance.SampleRate;
        set
        {
            int newLength = (int)(value * AudioEngine.Instance.SampleRate / 1000f);
            if (newLength <= 0) newLength = 1;

            if (_delayLengthSamples == newLength) return;
            _delayLengthSamples = newLength;
            InitializeDelayLines();
        }
    }

    /// <summary>
    /// 初始化延迟效果修饰器的新实例<br />
    /// Initializes a new instance of the DelayModifier class
    /// </summary>
    public DelayModifier()
    {
        _delayLines = [];
        _delayIndices = new int[AudioEngine.Channels];
        _filterStates = new float[AudioEngine.Channels];
        InitializeDelayLines();
    }

    private void InitializeDelayLines()
    {
        for(int i = 0; i < AudioEngine.Channels; i++)
        {
            _delayLines.Add(new float[_delayLengthSamples]);
        }
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        if (channel >= _delayLines.Count)
        {
            InitializeDelayLines();
            return sample;
        }
        
        float[] delayLine = _delayLines[channel];
        int index = _delayIndices[channel];
        
        // Get delayed sample
        float delayed = delayLine[index];
        
        // Apply low-pass filter to feedback
        float rc = 1f / (2 * MathF.PI * Cutoff);
        float alpha = AudioEngine.Instance.InverseSampleRate / (rc + AudioEngine.Instance.InverseSampleRate);
        delayed = alpha * delayed + (1 - alpha) * _filterStates[channel];
        _filterStates[channel] = delayed;
        
        // Write to delay line
        delayLine[index] = sample + delayed * Feedback;
        _delayIndices[channel] = (index + 1) % delayLine.Length;

        return sample * (1 - WetMix) + delayed * WetMix;
    }
}