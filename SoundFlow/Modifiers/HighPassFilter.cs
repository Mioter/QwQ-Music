using System;
using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// A sound modifier that implements a high-pass filter.
/// </summary>
public class HighPassFilter : SoundModifier
{
    private readonly float[] _previousOutput;
    private readonly float[] _previousSample;
    private float _cutoffFrequency;

    /// <summary>
    /// Initializes a new instance of the <see cref="HighPassFilter"/> class.
    /// </summary>
    /// <param name="cutoffFrequency">The cutoff frequency of the filter.</param>
    public HighPassFilter(float cutoffFrequency)
    {
        _previousOutput = new float[AudioEngine.Channels];
        _previousSample = new float[AudioEngine.Channels];
        CutoffFrequency = cutoffFrequency;
    }

    /// <summary>
    /// Gets or sets the cutoff frequency of the filter.
    /// </summary>
    public float CutoffFrequency
    {
        get => _cutoffFrequency;
        set => _cutoffFrequency = Math.Max(20, value);
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        float dt = AudioEngine.Instance.InverseSampleRate;
        float rc = 1f / (2 * MathF.PI * _cutoffFrequency);
        float alpha = rc / (rc + dt);
        float output = alpha * (_previousOutput[channel] + sample - _previousSample[channel]);
        _previousOutput[channel] = output;
        _previousSample[channel] = sample;
        return output;
    }
}