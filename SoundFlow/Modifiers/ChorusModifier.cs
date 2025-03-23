using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// A sound modifier that implements a chorus effect.
/// </summary>
public sealed class ChorusModifier : SoundModifier
{
    private float _maxDelayMs = 50f;
    private int _maxDelaySamples;
    private readonly List<float[]> _delayLines = [];
    private readonly int[] _delayIndices;
    private readonly float[] _lfoPhases;
    

    /// <summary>
    /// The maximum delay time in milliseconds (controls buffer size) <br />
    /// 最大延迟时间（毫秒，控制缓冲区大小）
    /// </summary>
    public float MaxDelayMs
    {
        get => _maxDelayMs;
        set
        {
            if (Math.Abs(_maxDelayMs - value) < 0.0001) return;
        
            _maxDelayMs = value;
            UpdateDelayBuffers();
        }
    }

    /// <summary>
    /// The depth of the chorus effect in milliseconds
    /// </summary>
    public float DepthMs { get; set; } = 2f;

    /// <summary>
    /// The rate of the LFO modulation in Hz
    /// </summary>
    public float RateHz { get; set; } = 0.5f;

    /// <summary>
    /// The feedback amount (0.0 - 1.0)
    /// </summary>
    public float Feedback { get; set; } = 0.7f;

    /// <summary>
    /// The wet/dry mix (0.0 - 1.0)
    /// </summary>
    public float WetDryMix { get; set; } = 0.5f;

    /// <summary>
    /// Constructs a new instance of <see cref="ChorusModifier"/>
    /// </summary>
    public ChorusModifier()
    {
        _delayIndices = new int[AudioEngine.Channels];
        _lfoPhases = new float[AudioEngine.Channels];
        UpdateDelayBuffers();
    }

    private void UpdateDelayBuffers()
    {
        _maxDelaySamples = Math.Max(1, (int)(_maxDelayMs * AudioEngine.Instance.SampleRate / 1000f));
        
        _delayLines.Clear();
        for (int i = 0; i < AudioEngine.Channels; i++)
        {
            _delayLines.Add(new float[_maxDelaySamples]);
        }
        
        Array.Clear(_delayIndices, 0, _delayIndices.Length);
        Array.Clear(_lfoPhases, 0, _lfoPhases.Length);
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        if (channel >= _delayLines.Count) return sample;
        
        float[] delayLine = _delayLines[channel];
        ref float phase = ref _lfoPhases[channel];
        ref int delayIndex = ref _delayIndices[channel];

        // Calculate modulated delay time
        float lfo = MathF.Sin(phase) * DepthMs * AudioEngine.Instance.SampleRate / 1000f;
        float delayTimeSamples = Math.Clamp(
            _maxDelaySamples / 2f + lfo,
            1f, 
            _maxDelaySamples - 1f
        );

        // Get delayed sample
        int readIndex = (delayIndex - (int)delayTimeSamples + _maxDelaySamples) % _maxDelaySamples;
        float delayed = delayLine[readIndex];

        // Update delay line
        delayLine[delayIndex] = sample + delayed * Feedback;
        delayIndex = (delayIndex + 1) % _maxDelaySamples;

        // Update LFO phase
        phase += MathF.Tau * RateHz / AudioEngine.Instance.SampleRate;
        if (phase >= MathF.Tau) phase -= MathF.Tau;

        return sample * (1 - WetDryMix) + delayed * WetDryMix;
    }
}