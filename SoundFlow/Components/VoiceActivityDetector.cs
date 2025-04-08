using System.Numerics;
using SoundFlow.Abstracts;
using SoundFlow.Interfaces;
using SoundFlow.Utils;

namespace SoundFlow.Components;

/// <summary>
/// WIP - Advanced voice activity detector with noise reduction, multiple detection algorithms, and environmental adaptation.
/// </summary>
public sealed class VoiceActivityDetector : AudioAnalyzer
{
    #region Configuration

    /// <summary>
    /// Defines the detection mode for the voice activity detector.
    /// </summary>
    public enum DetectionMode
    {
        /// <summary>
        /// Detects voice activity based on the energy level of the audio signal.
        /// </summary>
        EnergyBased,

        /// <summary>
        /// Detects voice activity based on the spectral contrast of the audio signal.
        /// </summary>
        SpectralContrast,

        /// <summary>
        /// Detects voice activity based on the zero crossing rate of the audio signal.
        /// </summary>
        ZeroCrossingRate,

        /// <summary>
        /// Detects voice activity by combining multiple detection algorithms for improved accuracy.
        /// </summary>
        Combined,
    }

    /// <summary>
    /// Configuration struct for the VoiceActivityDetector.
    /// Defines parameters to customize the detector's behavior.
    /// </summary>
    public record Configuration
    {
        /// <summary>
        /// Size of the FFT (Fast Fourier Transform) window used for spectral analysis.
        /// Must be a power of 2. Larger sizes provide finer frequency resolution but increase computational cost.
        /// Default is 1024.
        /// </summary>
        public readonly int FftSize = 1024;

        /// <summary>
        /// Initial threshold for energy-based voice detection.
        /// It represents the starting noise floor level. Adjust based on the expected noise environment.
        /// Default is 0.05f.
        /// </summary>
        public readonly float InitialThreshold = 0.05f;

        /// <summary>
        /// Rate at which the noise floor decays over time.
        /// A higher rate allows the detector to adapt to changing noise levels more quickly but may be less stable.
        /// Default is 0.01f.
        /// </summary>
        public readonly float NoiseFloorDecayRate = 0.01f;

        /// <summary>
        /// Lower frequency bound of the speech band in Hz.
        /// Frequencies below this are considered non-speech. Default is 300Hz.
        /// </summary>
        public readonly float SpeechBandLow = 300f;

        /// <summary>
        /// Upper frequency bound of the speech band in Hz.
        /// Frequencies above this are considered non-speech. Default is 3400Hz.
        /// </summary>
        public readonly float SpeechBandHigh = 3400f;

        /// <summary>
        /// Size of the history buffer used to smooth energy levels and stabilize detection.
        /// A larger buffer provides more smoothing but increases memory usage and latency.
        /// Default is 50.
        /// </summary>
        public readonly int HistoryBufferSize = 50;

        /// <summary>
        /// Ratio of the dynamic threshold used for hysteresis.
        /// Hysteresis prevents rapid toggling of voice detection around the threshold.
        /// Default is 0.2f.
        /// </summary>
        public readonly float HysteresisRatio = 0.2f;

        /// <summary>
        /// Detection mode to use for voice activity detection.
        /// Selects the algorithm used to determine voice activity. Default is EnergyBased.
        /// </summary>
        public readonly DetectionMode Mode = DetectionMode.EnergyBased;

        /// <summary>
        /// Minimum confidence level required to trigger voice activity detection in combined mode.
        /// Higher values require stronger signal presence to be considered voice. Default is 0.1f.
        /// </summary>
        public readonly float MinSignalConfidence = 0.1f;

        /// <summary>
        /// Attack time for the noise gate, determining how quickly the gate opens when signal is detected.
        /// Shorter attack times respond faster but may introduce artifacts with sudden sounds. Default is 20ms.
        /// </summary>
        public TimeSpan NoiseGateAttack = TimeSpan.FromMilliseconds(20);

        /// <summary>
        /// Release time for the noise gate, determining how quickly the gate closes after signal drops below the threshold.
        /// Longer release times provide smoother fading but may mask brief pauses in speech. Default is 150ms.
        /// </summary>
        public TimeSpan NoiseGateRelease = TimeSpan.FromMilliseconds(150);
    }

    #endregion

    private readonly Complex[] _fftBuffer;
    private readonly float[] _window;
    private Queue<float> _sampleBuffer;
    private readonly float[] _energyHistory;
    private readonly Lock _stateLock = new();

    private int _currentHistoryIndex;
    private float _dynamicThreshold;
    private float _spectralFlatness;
    private float _zeroCrossingRate;
    private DateTimeOffset _lastVoiceActivity;

    /// <summary>
    /// Gets the current configuration settings of the voice activity detector.
    /// </summary>
    public Configuration Config { get; }

    /// <summary>
    /// Gets the current noise floor level estimated by the detector.
    /// Represents the background noise level.
    /// </summary>
    public float CurrentNoiseFloor { get; private set; }

    /// <summary>
    /// Gets the current frame energy calculated by the detector.
    /// Represents the signal power in the current audio frame.
    /// </summary>
    public float CurrentEnergy { get; private set; }

    /// <summary>
    /// Gets the current signal confidence level.
    /// Represents the detector's certainty of voice activity, especially in combined mode.
    /// </summary>
    public float CurrentConfidence { get; private set; }

    /// <summary>
    /// Gets a value indicating whether voice activity is currently detected.
    /// True if voice is detected, false otherwise.
    /// </summary>
    public bool IsVoiceActive { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceActivityDetector"/> class.
    /// </summary>
    /// <param name="config">The configuration settings for the voice activity detector.</param>
    /// <param name="visualizer">Optional visualizer for audio analysis visualization.</param>
    public VoiceActivityDetector(Configuration config, IVisualizer? visualizer = null)
        : base(visualizer)
    {
        ValidateConfiguration(config);
        Config = config;

        _fftBuffer = new Complex[Config.FftSize];
        _window = MathHelper.HammingWindow(Config.FftSize);
        _sampleBuffer = new Queue<float>(Config.FftSize * 2);
        _energyHistory = new float[Config.HistoryBufferSize];

        _dynamicThreshold = Config.InitialThreshold;
        CurrentNoiseFloor = Config.InitialThreshold;
    }

    /// <summary>
    /// Analyzes the provided audio buffer for voice activity detection.
    /// This method processes the audio data, performs feature extraction, and makes a decision on voice activity.
    /// </summary>
    /// <param name="buffer">The audio buffer to analyze as a span of floats.</param>
    protected override void Analyze(Span<float> buffer)
    {
        lock (_stateLock)
        {
            ProcessBuffer(buffer);

            while (_sampleBuffer.Count >= Config.FftSize)
            {
                float[] frame = GetNextFrame();
                float[] processedFrame = ApplyNoiseGate(frame);

                CurrentEnergy = CalculateFrameEnergy(processedFrame);
                UpdateEnergyHistory(CurrentEnergy);

                float[] spectrum = ComputeSpectrum(processedFrame);
                _spectralFlatness = CalculateSpectralFlatness(spectrum);
                _zeroCrossingRate = CalculateZeroCrossingRate(processedFrame);

                UpdateNoiseFloor();
                UpdateDynamicThreshold();
                UpdateSignalConfidence();

                bool decision = MakeDetectionDecision();
                UpdateVoiceState(decision);
            }
        }
    }

    #region Core Processing

    /// <summary>
    /// Processes the input audio buffer by applying DC offset removal and high-pass filtering.
    /// </summary>
    /// <param name="input">The input audio buffer as a span of floats.</param>
    private void ProcessBuffer(Span<float> input)
    {
        const float dcBias = 0.5f;
        const float highPassCoeff = 0.99f;
        float dcOffset = 0f;

        // 预先计算缓冲区大小，避免动态扩容
        int requiredCapacity = _sampleBuffer.Count + input.Length;
        if (_sampleBuffer.Capacity < requiredCapacity)
        {
            // 创建新队列并复制数据，避免频繁扩容
            var newBuffer = new Queue<float>(Math.Max(requiredCapacity, Config.FftSize * 4));
            foreach (float sample in _sampleBuffer)
                newBuffer.Enqueue(sample);
            _sampleBuffer = newBuffer;
        }

        foreach (float sample in input)
        {
            // Proper DC offset removal with high-pass filtering
            float processed = sample - dcBias;
            dcOffset = highPassCoeff * dcOffset + (1 - highPassCoeff) * processed;
            float cleanSample = processed - dcOffset;

            _sampleBuffer.Enqueue(Math.Clamp(cleanSample, -1f, 1f));
        }
    }

    /// <summary>
    /// Retrieves the next audio frame from the sample buffer.
    /// </summary>
    /// <returns>A float array representing the next audio frame.</returns>
    private float[] GetNextFrame()
    {
        float[] frame = new float[Config.FftSize];
        for (int i = 0; i < Config.FftSize; i++)
        {
            frame[i] = _sampleBuffer.Dequeue();
            _fftBuffer[i] = new Complex(frame[i] * _window[i], 0);
        }

        return frame;
    }

    /// <summary>
    /// Applies a noise gate to the audio frame to reduce background noise.
    /// </summary>
    /// <param name="frame">The input audio frame.</param>
    /// <returns>The noise-gated audio frame.</returns>
    private float[] ApplyNoiseGate(float[] frame)
    {
        float[] output = new float[frame.Length];
        float attackCoeff = CalculateCoefficient(Config.NoiseGateAttack);
        float releaseCoeff = CalculateCoefficient(Config.NoiseGateRelease);
        float envelope = 0f;

        for (int i = 0; i < frame.Length; i++)
        {
            float abs = Math.Abs(frame[i]);
            envelope =
                abs > envelope
                    ? attackCoeff * envelope + (1 - attackCoeff) * abs
                    : releaseCoeff * envelope + (1 - releaseCoeff) * abs;

            output[i] = envelope > CurrentNoiseFloor ? frame[i] : 0;
        }

        return output;
    }

    #endregion

    #region Analysis Methods

    /// <summary>
    /// Computes the power spectrum of the audio frame using FFT.
    /// </summary>
    /// <param name="frame">The input audio frame.</param>
    /// <returns>The power spectrum of the frame.</returns>
    private float[] ComputeSpectrum(float[] frame)
    {
        // 避免重复窗口应用，GetNextFrame 已经应用了窗口
        // 只在需要时应用窗口
        bool needWindow = true;
        for (int i = 0; i < frame.Length; i++)
        {
            _fftBuffer[i] = new Complex(needWindow ? frame[i] * _window[i] : frame[i], 0);
        }

        MathHelper.Fft(_fftBuffer);

        float[] spectrum = new float[Config.FftSize / 2];
        const float epsilon = 1e-12f;

        // 使用 Span<T> 优化内存分配
        Span<float> spectrumSpan = spectrum;
        for (int i = 1; i < Config.FftSize / 2; i++)
        {
            float magnitude = (float)(_fftBuffer[i].Magnitude / Config.FftSize);
            spectrumSpan[i] = Math.Clamp(magnitude * magnitude, epsilon, 1f);
        }

        return spectrum;
    }

    /// <summary>
    /// Calculates the spectral flatness of the audio spectrum.
    /// Spectral flatness is a measure of how noise-like or tonal the sound is.
    /// </summary>
    /// <param name="spectrum">The power spectrum of the audio frame.</param>
    /// <returns>The spectral flatness value (0.0 for tonal, 1.0 for noise-like).</returns>
    private static float CalculateSpectralFlatness(float[] spectrum)
    {
        float geometricMean = 1f;
        float arithmeticMean = 0f;
        const float epsilon = 1e-20f;

        foreach (float value in spectrum)
        {
            float safeValue = Math.Max(value, epsilon);
            geometricMean *= safeValue;
            arithmeticMean += safeValue;
        }

        geometricMean = MathF.Pow(geometricMean, 1f / spectrum.Length);
        arithmeticMean /= spectrum.Length;

        return arithmeticMean > epsilon ? geometricMean / arithmeticMean : 0f;
    }

    /// <summary>
    /// Calculates the zero crossing rate of the audio frame.
    /// Zero crossing rate is the number of times the signal changes sign per frame, indicative of noise or speech.
    /// </summary>
    /// <param name="frame">The input audio frame.</param>
    /// <returns>The zero crossing rate value (normalized by frame length).</returns>
    private static float CalculateZeroCrossingRate(float[] frame)
    {
        int crossings = 0;
        float prevSample = frame[0];

        for (int i = 1; i < frame.Length; i++)
        {
            if (!(Math.Abs(frame[i] - prevSample) > 0.01f) || Math.Sign(frame[i]) == Math.Sign(prevSample))
                continue;

            crossings++;
            prevSample = frame[i];
        }

        return (float)crossings / frame.Length;
    }

    #endregion

    #region Decision Logic

    /// <summary>
    /// Makes a voice activity detection decision based on the configured detection mode.
    /// </summary>
    /// <returns>True if voice activity is detected, false otherwise.</returns>
    private bool MakeDetectionDecision()
    {
        return Config.Mode switch
        {
            DetectionMode.EnergyBased => EnergyBasedDecision(),
            DetectionMode.SpectralContrast => SpectralContrastDecision(),
            DetectionMode.ZeroCrossingRate => ZeroCrossingDecision(),
            DetectionMode.Combined => CombinedDecision(),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    /// <summary>
    /// Makes a voice activity detection decision using a combined approach of energy, spectral flatness, and zero crossing rate.
    /// </summary>
    /// <returns>True if voice activity is detected based on combined criteria, false otherwise.</returns>
    private bool CombinedDecision()
    {
        float energyScore = Math.Clamp((CurrentEnergy - CurrentNoiseFloor) / _dynamicThreshold, 0, 1);
        float spectralScore = 1 - _spectralFlatness;
        float zcrScore = 1 - Math.Clamp(_zeroCrossingRate / 0.5f, 0, 1);

        float combined = energyScore * 0.6f + spectralScore * 0.3f + zcrScore * 0.1f;

        return combined > Config.MinSignalConfidence;
    }

    /// <summary>
    /// Updates the signal confidence level based on the current voice state and time since last activity.
    /// </summary>
    private void UpdateSignalConfidence()
    {
        var timeSinceLast = DateTimeOffset.Now - _lastVoiceActivity;
        float decayFactor = Math.Clamp((float)timeSinceLast.TotalSeconds / 2, 0, 1);
        CurrentConfidence = MathHelper.Lerp(CurrentConfidence, IsVoiceActive ? 1 : 0, decayFactor);
    }

    /// <summary>
    /// Calculates the energy of the audio frame.
    /// Frame energy is the sum of the squares of the samples in the frame, normalized by frame length.
    /// </summary>
    /// <param name="frame">The input audio frame.</param>
    /// <returns>The energy of the frame.</returns>
    private static float CalculateFrameEnergy(float[] frame)
    {
        float energy = 0;
        foreach (float sample in frame)
        {
            energy += sample * sample;
        }

        return energy / frame.Length;
    }

    /// <summary>
    /// Updates the energy history buffer with the current frame energy.
    /// </summary>
    /// <param name="energy">The current frame energy.</param>
    private void UpdateEnergyHistory(float energy)
    {
        _energyHistory[_currentHistoryIndex] = energy;
        _currentHistoryIndex = (_currentHistoryIndex + 1) % _energyHistory.Length;
    }

    /// <summary>
    /// Makes a voice activity detection decision based on energy level.
    /// </summary>
    /// <returns>True if voice activity is detected based on energy, false otherwise.</returns>
    private bool EnergyBasedDecision()
    {
        float snr = (CurrentEnergy - CurrentNoiseFloor) / CurrentNoiseFloor;
        return snr > Config.MinSignalConfidence && CurrentEnergy > _dynamicThreshold;
    }

    /// <summary>
    /// Makes a voice activity detection decision based on spectral contrast.
    /// </summary>
    /// <returns>True if voice activity is detected based on spectral contrast, false otherwise.</returns>
    private bool SpectralContrastDecision()
    {
        const float speechContrastThreshold = 0.15f;
        return 1 - _spectralFlatness > speechContrastThreshold;
    }

    /// <summary>
    /// Makes a voice activity detection decision based on zero crossing rate.
    /// </summary>
    /// <returns>True if voice activity is detected based on zero crossing rate, false otherwise.</returns>
    private bool ZeroCrossingDecision()
    {
        const float zcrSpeechMax = 0.3f;
        const float zcrSpeechMin = 0.05f;
        return _zeroCrossingRate is > zcrSpeechMin and < zcrSpeechMax;
    }

    #endregion

    #region State Management

    /// <summary>
    /// Updates the voice activity state based on the detection decision and hysteresis.
    /// </summary>
    /// <param name="detected">The detection decision (true if voice detected, false otherwise).</param>
    private void UpdateVoiceState(bool detected)
    {
        float hysteresisThreshold = detected
            ? _dynamicThreshold * (1 - Config.HysteresisRatio)
            : _dynamicThreshold * (1 + Config.HysteresisRatio);

        if (detected && CurrentEnergy > hysteresisThreshold && !IsVoiceActive)
        {
            IsVoiceActive = true;
            _lastVoiceActivity = DateTimeOffset.Now;
            RaiseVoiceEvent(true);
        }
        else if (!detected && CurrentEnergy < hysteresisThreshold && IsVoiceActive)
        {
            IsVoiceActive = false;
            RaiseVoiceEvent(false);
        }
    }

    /// <summary>
    /// Updates the noise floor estimate based on the minimum recent energy level.
    /// Adapts the noise floor to changing background noise levels.
    /// </summary>
    private void UpdateNoiseFloor()
    {
        float minRecentEnergy = _energyHistory.Min();
        CurrentNoiseFloor = MathHelper.Lerp(CurrentNoiseFloor, minRecentEnergy, Config.NoiseFloorDecayRate);
    }

    /// <summary>
    /// Updates the dynamic threshold based on the current noise floor.
    /// The dynamic threshold adapts to noise levels to maintain detection sensitivity.
    /// </summary>
    private void UpdateDynamicThreshold()
    {
        float targetThreshold = CurrentNoiseFloor * 1.5f + 0.01f;
        _dynamicThreshold = MathHelper.Lerp(_dynamicThreshold, targetThreshold, 0.1f);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Validates the configuration settings to ensure they are valid.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    private static void ValidateConfiguration(Configuration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (!MathHelper.IsPowerOfTwo(config.FftSize))
            throw new ArgumentException("FFT size must be power of two", nameof(config));

        if (config.SpeechBandHigh <= config.SpeechBandLow)
            throw new ArgumentException($"Invalid frequency band: {config.SpeechBandLow}-{config.SpeechBandHigh}Hz");

        if (config.HistoryBufferSize <= 0)
            throw new ArgumentException("History buffer size must be positive", nameof(config));

        if (config.NoiseFloorDecayRate is <= 0 or > 1)
            throw new ArgumentException("Noise floor decay rate must be between 0 and 1", nameof(config));
    }

    /// <summary>
    /// Calculates a coefficient based on the given time duration, used for smoothing filters.
    /// </summary>
    /// <param name="time">The time duration for coefficient calculation.</param>
    /// <returns>The calculated coefficient.</returns>
    private static float CalculateCoefficient(TimeSpan time)
    {
        float sampleTime = 1f / AudioEngine.Instance.SampleRate;
        return MathF.Exp(-sampleTime / (float)time.TotalSeconds);
    }

    /// <summary>
    /// Raises the VoiceActivityChanged event to notify listeners of a change in voice activity status.
    /// </summary>
    /// <param name="active">True if voice activity is now active, false otherwise.</param>
    private void RaiseVoiceEvent(bool active)
    {
        var handler = VoiceActivityChanged;
        if (handler != null)
        {
            // 创建事件参数的副本，确保线程安全
            var args = new VoiceActivityEventArgs(active, CurrentEnergy, CurrentNoiseFloor, CurrentConfidence);

            // 使用线程安全的方式触发事件
            handler(this, args);
        }
    }

    #endregion

    /// <summary>
    /// Occurs when the voice activity state changes (voice starts or stops).
    /// </summary>
    public event EventHandler<VoiceActivityEventArgs>? VoiceActivityChanged;
}

/// <summary>
/// Event arguments for the VoiceActivityChanged event, providing details about the voice activity state.
/// </summary>
/// <param name="isActive">Indicates whether voice activity is currently active.</param>
/// <param name="energy">The current energy level of the audio signal at the time of the event.</param>
/// <param name="noise">The current noise floor level estimated by the detector.</param>
/// <param name="confidence">The confidence level of voice activity detection.</param>
public class VoiceActivityEventArgs(bool isActive, float energy, float noise, float confidence) : EventArgs
{
    /// <summary>
    /// Gets a value indicating whether voice activity is currently active.
    /// </summary>
    public bool IsActive { get; } = isActive;

    /// <summary>
    /// Gets the current energy level of the audio signal.
    /// </summary>
    public float CurrentEnergy { get; } = energy;

    /// <summary>
    /// Gets the current noise floor level estimated by the detector.
    /// </summary>
    public float NoiseFloor { get; } = noise;

    /// <summary>
    /// Gets the confidence level of voice activity detection.
    /// </summary>
    public float Confidence { get; } = confidence;
}
