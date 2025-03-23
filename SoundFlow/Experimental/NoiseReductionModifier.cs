using System.Numerics;
using SoundFlow.Abstracts;
using SoundFlow.Utils;

namespace SoundFlow.Experimental;

/// <summary>
/// A sound modifier that implements a noise reduction algorithm.
/// </summary>
public class NoiseReductionModifier : SoundModifier
{
    private readonly int _fftSize;
    private readonly int _hopSize;
    private readonly float _alpha;
    private readonly float _beta;
    private readonly float _smoothingFactor;
    private readonly float _gain;
    private readonly float[] _window;
    private readonly float _windowSumSq;
    private readonly Complex[][] _fftBuffers;
    private readonly float[][] _noisePsd;
    private readonly float[][] _inputBuffers;
    private readonly float[][] _outputOverlapBuffers;
    private readonly int _noiseFrames;
    private readonly int _channels;
    private int _noiseFramesCollected;
    private bool _noiseEstimationDone;

    /// <inheritdoc />
    public override string Name { get; set; } = "Noise Reducer";

    /// <summary>
    /// WIP - Initializes a new instance of the <see cref="NoiseReductionModifier"/> class.
    /// </summary>
    /// <param name="fftSize">The size of the FFT. Must be a power of 2.</param>
    /// <param name="alpha">The over-subtraction factor. Typical values are between 1 and 5.</param>
    /// <param name="beta">The spectral flooring parameter. Typical values are between 0 and 0.1.</param>
    /// <param name="smoothingFactor">The smoothing factor for residual noise suppression.</param>
    /// <param name="gain">Post-processing gain multiplier.</param>
    /// <param name="noiseFrames">The number of initial frames to use for noise estimation.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <remarks>Exceptionally Not working yet.</remarks>
    public NoiseReductionModifier(int fftSize = 2048, float alpha = 2.0f, float beta = 0.01f, 
        float smoothingFactor = 0.9f, float gain = 1.2f, int noiseFrames = 10)
    {
        if ((fftSize & fftSize - 1) != 0)
            throw new ArgumentException("FFT size must be a power of 2.");
        
        _fftSize = fftSize;
        _hopSize = fftSize / 2;
        _alpha = alpha;
        _beta = beta;
        _smoothingFactor = smoothingFactor;
        _gain = gain;
        _channels = AudioEngine.Channels;
        _window = MathHelper.HanningWindow(fftSize);
        _windowSumSq = CalculateWindowSumSq();
        
        _fftBuffers = new Complex[_channels][];
        _noisePsd = new float[_channels][];
        _inputBuffers = new float[_channels][];
        _outputOverlapBuffers = new float[_channels][];

        for (int c = 0; c < _channels; c++)
        {
            _fftBuffers[c] = new Complex[_fftSize];
            _noisePsd[c] = new float[_fftSize / 2 + 1];
            _inputBuffers[c] = new float[_fftSize * 2]; // Ring buffer
            _outputOverlapBuffers[c] = new float[_hopSize];
        }
        _noiseFrames = noiseFrames;
    }

    private float CalculateWindowSumSq()
    {
        float sum = 0;
        for (int i = 0; i < _fftSize; i++)
            sum += _window[i] * _window[i];
        return sum;
    }

    private void EstimateNoise(int channel)
    {
        float[] noisePsd = _noisePsd[channel];
        Array.Clear(noisePsd, 0, noisePsd.Length);

        // Process noise frames with 50% overlap
        for (int i = 0; i < _noiseFrames; i++)
        {
            int offset = i * _hopSize;
            
            // Apply window
            for (int j = 0; j < _fftSize; j++)
                _fftBuffers[channel][j] = new Complex(_inputBuffers[channel][j + offset] * _window[j], 0);

            MathHelper.Fft(_fftBuffers[channel]);

            // Accumulate PSD
            for (int j = 0; j <= _fftSize / 2; j++)
                noisePsd[j] += (float)Math.Pow(_fftBuffers[channel][j].Magnitude, 2);
        }

        // Average and smooth
        for (int j = 0; j <= _fftSize / 2; j++)
            noisePsd[j] = noisePsd[j] / _noiseFrames * _smoothingFactor;
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel) => 
        throw new NotSupportedException("NoiseReducer operates on buffers");

    /// <inheritdoc />
    public override void Process(Span<float> buffer)
    {
        for (int c = 0; c < _channels; c++)
        {
            ProcessChannel(
                buffer: buffer,
                channel: c,
                channelOffset: c,
                stride: _channels
            );
        }
    }

    private void ProcessChannel(Span<float> buffer, int channel, int channelOffset, int stride)
    {
        float[] inputBuffer = _inputBuffers[channel];
        float[] outputOverlap = _outputOverlapBuffers[channel];
        var fftBuffer = _fftBuffers[channel];
        float[] noisePsd = _noisePsd[channel];

        // Copy new samples into ring buffer
        int samplesToCopy = buffer.Length / _channels;
        for (int i = 0; i < samplesToCopy; i++)
        {
            inputBuffer[(i + _hopSize) % inputBuffer.Length] = 
                buffer[channelOffset + i * stride];
        }

        int totalProcessed = 0;
        while (totalProcessed + _fftSize <= samplesToCopy + _hopSize)
        {
            // Noise estimation phase
            if (!_noiseEstimationDone)
            {
                _noiseFramesCollected++;
                if (_noiseFramesCollected >= _noiseFrames)
                {
                    for (int c = 0; c < _channels; c++)
                        EstimateNoise(c);
                    _noiseEstimationDone = true;
                }
                continue;
            }

            // Copy frame to FFT buffer with windowing
            for (int j = 0; j < _fftSize; j++)
                fftBuffer[j] = new Complex(inputBuffer[j] * _window[j], 0);

            MathHelper.Fft(fftBuffer);

            // Spectral subtraction
            for (int j = 0; j <= _fftSize / 2; j++)
            {
                float power = (float)Math.Pow(fftBuffer[j].Magnitude, 2);
                float noiseEstimate = _alpha * noisePsd[j];
                float gain = (power - noiseEstimate) / (power + _beta * noiseEstimate + float.Epsilon);
                gain = Math.Max(gain, 0);

                fftBuffer[j] *= gain;
                if (j > 0 && j < _fftSize / 2)
                    fftBuffer[_fftSize - j] = Complex.Conjugate(fftBuffer[j]);
            }

            // Handle Nyquist bin
            if (_fftSize % 2 == 0)
                fftBuffer[_fftSize / 2] = new Complex(fftBuffer[_fftSize / 2].Real, 0);

            MathHelper.InverseFft(fftBuffer);

            // Overlap-add with COLA normalization
            for (int j = 0; j < _fftSize; j++)
            {
                float outputSample = (float)(fftBuffer[j].Real * _window[j]) / _windowSumSq * _gain;
                
                if (j < _hopSize)
                {
                    // Add overlap from previous frame
                    outputSample += outputOverlap[j];
                    outputOverlap[j] = 0;
                }

                if (j + totalProcessed < buffer.Length / _channels)
                {
                    buffer[channelOffset + (totalProcessed + j) * stride] = outputSample;
                }
                else
                {
                    // Store overlap for next frame
                    outputOverlap[j - _hopSize] += outputSample;
                }
            }

            // Shift ring buffer
            Array.Copy(inputBuffer, _hopSize, inputBuffer, 0, inputBuffer.Length - _hopSize);
            totalProcessed += _hopSize;
        }
    }
}