using System.Numerics;
using SoundFlow.Abstracts;
using SoundFlow.Interfaces;

namespace SoundFlow.Visualization;

/// <summary>
/// Analyzes audio data to provide level (peak, RMS) information.
/// </summary>
public class LevelMeterAnalyzer : AudioAnalyzer
{
    /// <inheritdoc />
    public override string Name { get; set; } = "Level Meter";

    /// <summary>
    /// Initializes a new instance of the <see cref="LevelMeterAnalyzer"/> class.
    /// </summary>
    /// <param name="visualizer">The visualizer to send data to.</param>
    public LevelMeterAnalyzer(IVisualizer? visualizer = null) : base(visualizer)
    {
    }

    /// <summary>
    /// Gets the current RMS level.
    /// </summary>
    public float Rms { get; private set; }

    /// <summary>
    /// Gets the current peak level.
    /// </summary>
    public float Peak { get; private set; }

    /// <inheritdoc/>
    protected override void Analyze(Span<float> buffer)
    {
        float peak = 0f;
        float sumSquares = 0f;

        if (!Vector.IsHardwareAccelerated || buffer.Length < Vector<float>.Count)
        {
            // Scalar processing
            foreach (float sample in buffer)
            {
                float absSample = Math.Abs(sample);
                if (absSample > peak)
                {
                    peak = absSample;
                }
                sumSquares += sample * sample;
            }
        }
        else
        {
            // SIMD processing
            int vectorSize = Vector<float>.Count;
            int i = 0;
            var sumSquaresVector = Vector<float>.Zero;

            for (; i <= buffer.Length - vectorSize; i += vectorSize)
            {
                Vector<float> vector = new(buffer.Slice(i, vectorSize));
                var absVector = Vector.Abs(vector);
                
                // Find the maximum element in absVector
                float maxInVector = absVector[0];
                for (int j = 1; j < vectorSize; j++)
                {
                    maxInVector = Math.Max(maxInVector, absVector[j]);
                }

                peak = Math.Max(peak, maxInVector);
                sumSquaresVector += vector * vector;
            }

            // Reduce the sum of squares vector
            for (int j = 0; j < vectorSize; j++)
            {
                sumSquares += sumSquaresVector[j];
            }

            // Handle remaining elements
            for (; i < buffer.Length; i++)
            {
                float sample = buffer[i];
                float absSample = Math.Abs(sample);
                if (absSample > peak)
                {
                    peak = absSample;
                }
                sumSquares += sample * sample;
            }
        }

        Peak = peak;
        Rms = MathF.Sqrt(sumSquares / buffer.Length);
    }
}