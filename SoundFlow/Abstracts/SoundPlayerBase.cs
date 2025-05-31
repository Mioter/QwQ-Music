using SoundFlow.Enums;
using SoundFlow.Interfaces;

namespace SoundFlow.Abstracts;

/// <summary>
/// Abstract base class for sound players, providing common functionality.
/// </summary>
public abstract class SoundPlayerBase : SoundComponent, ISoundPlayer
{
    private readonly ISoundDataProvider _dataProvider;
    private int _rawSamplePosition;
    private float _currentFractionalFrame;

    private float[] _resampleBuffer;
    private int _resampleBufferValidSamples;

    private float _playbackSpeed = 1.0f;

    /// <inheritdoc />
    public float PlaybackSpeed
    {
        get => _playbackSpeed;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Playback speed must be greater than zero.");
            _playbackSpeed = value;
        }
    }

    /// <inheritdoc />
    public PlaybackState State { get; private set; }

    /// <inheritdoc />
    public bool IsLooping { get; set; }

    /// <inheritdoc />
    public float Time =>
        _dataProvider.Length == 0 || AudioEngine.Channels == 0 || AudioEngine.Instance.SampleRate == 0
            ? 0
            : (float)_rawSamplePosition / AudioEngine.Channels / AudioEngine.Instance.SampleRate;

    /// <summary>
    /// Returns the current time in seconds, in normal playback speed (1.0).
    /// </summary>
    public float SourceTimeSeconds => Time / PlaybackSpeed;

    /// <inheritdoc />
    public float Duration
    {
        get
        {
            if (
                _dataProvider.Length == 0
                || PlaybackSpeed == 0
                || AudioEngine.Channels == 0
                || AudioEngine.Instance.SampleRate == 0
            )
                return 0f;
            return (float)_dataProvider.Length / AudioEngine.Channels / AudioEngine.Instance.SampleRate;
        }
    }

    /// <inheritdoc />
    public int LoopStartSamples { get; private set; }

    /// <inheritdoc />
    public int LoopEndSamples { get; private set; } = -1;

    /// <inheritdoc />
    public float LoopStartSeconds =>
        AudioEngine.Channels == 0 || AudioEngine.Instance.SampleRate == 0
            ? 0
            : (float)LoopStartSamples / AudioEngine.Channels / AudioEngine.Instance.SampleRate;

    /// <inheritdoc />
    public float LoopEndSeconds =>
        LoopEndSamples == -1 || AudioEngine.Channels == 0 || AudioEngine.Instance.SampleRate == 0
            ? -1
            : (float)LoopEndSamples / AudioEngine.Channels / AudioEngine.Instance.SampleRate;

    /// <summary>
    /// Constructor for BaseSoundPlayer.
    /// </summary>
    /// <param name="dataProvider">The sound data provider.</param>
    /// <exception cref="ArgumentNullException">Thrown if dataProvider is null.</exception>
    protected SoundPlayerBase(ISoundDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        int initialChannels = AudioEngine.Channels > 0 ? AudioEngine.Channels : 2; // Default to 2 if not yet known
        int initialSampleRate = AudioEngine.Instance.SampleRate > 0 ? AudioEngine.Instance.SampleRate : 44100; // Default
        int initialBufferSize = Math.Max(256, initialSampleRate * initialChannels / 10); // e.g., 100ms
        _resampleBuffer = new float[initialBufferSize];
    }

    /// <inheritdoc />
    protected override void GenerateAudio(Span<float> output)
    {
        if (State != PlaybackState.Playing || AudioEngine.Channels == 0)
        {
            output.Clear();
            return;
        }

        int channels = AudioEngine.Channels;
        int outputFramesTotal = output.Length / channels;
        int outputBufferOffset = 0;

        for (int i = 0; i < outputFramesTotal; i++)
        {
            int currentIntegerFrame = (int)Math.Floor(_currentFractionalFrame);
            // Need at least current frame + next frame for interpolation
            int framesRequiredInBufferForInterpolation = currentIntegerFrame + 2;
            int samplesRequiredInBufferForInterpolation = framesRequiredInBufferForInterpolation * channels;

            if (_resampleBufferValidSamples < samplesRequiredInBufferForInterpolation)
            {
                FillResampleBuffer(samplesRequiredInBufferForInterpolation);

                // Re-check after attempting to fill
                if (_resampleBufferValidSamples < samplesRequiredInBufferForInterpolation)
                {
                    // Still not enough data, means end of current data provider segment
                    Span<float> remainingOutput = output[outputBufferOffset..];
                    // HandleEndOfStream will take care of clearing or filling if looped
                    HandleEndOfStream(remainingOutput);
                    return; // Must exit, HandleEndOfStream might re-enter GenerateAudio
                }
            }

            int frameIndex0 = currentIntegerFrame;
            float t = _currentFractionalFrame - frameIndex0;

            for (int ch = 0; ch < channels; ch++)
            {
                int sampleIndex0 = frameIndex0 * channels + ch;
                int sampleIndex1 = (frameIndex0 + 1) * channels + ch;

                // Boundary checks for safety, though FillResampleBuffer should ensure enough if possible
                if (sampleIndex1 >= _resampleBufferValidSamples)
                {
                    output[outputBufferOffset + ch] =
                        (sampleIndex0 < _resampleBufferValidSamples && sampleIndex0 >= 0)
                            ? _resampleBuffer[sampleIndex0]
                            : 0f;
                    continue;
                }
                if (sampleIndex0 < 0)
                {
                    output[outputBufferOffset + ch] = 0f;
                    continue;
                }

                float s0 = _resampleBuffer[sampleIndex0];
                float s1 = _resampleBuffer[sampleIndex1];
                output[outputBufferOffset + ch] = s0 * (1.0f - t) + s1 * t;
            }

            outputBufferOffset += channels;
            _currentFractionalFrame += PlaybackSpeed;

            int framesConsumedInteger = (int)Math.Floor(_currentFractionalFrame);

            if (framesConsumedInteger > 0)
            {
                int conceptualSamplesConsumed = framesConsumedInteger * channels;
                _rawSamplePosition += conceptualSamplesConsumed;

                int actualSamplesToDiscardFromBuffer = Math.Min(conceptualSamplesConsumed, _resampleBufferValidSamples);

                if (actualSamplesToDiscardFromBuffer > 0)
                {
                    int remainingSamplesInBuffer = _resampleBufferValidSamples - actualSamplesToDiscardFromBuffer;
                    if (remainingSamplesInBuffer > 0)
                    {
                        Buffer.BlockCopy(
                            _resampleBuffer,
                            actualSamplesToDiscardFromBuffer * sizeof(float),
                            _resampleBuffer,
                            0,
                            remainingSamplesInBuffer * sizeof(float)
                        );
                    }
                    _resampleBufferValidSamples = remainingSamplesInBuffer;
                }
                _currentFractionalFrame -= framesConsumedInteger;
            }
        }
    }

    private void FillResampleBuffer(int minSamplesRequiredInTotal)
    {
        int channels = AudioEngine.Channels;
        if (channels == 0)
            return;

        // If we already have enough valid samples for the current need, return.
        if (_resampleBufferValidSamples >= minSamplesRequiredInTotal)
            return;

        // Resize _resampleBuffer if it's fundamentally too small to hold the required samples
        // or if it's smaller than a reasonable minimum processing size.
        int effectiveMinSize = Math.Max(minSamplesRequiredInTotal, Math.Max(256, channels * 4));
        if (_resampleBuffer.Length < effectiveMinSize)
        {
            int newSize = Math.Max(effectiveMinSize, _resampleBuffer.Length * 2);
            if (newSize > _resampleBuffer.Length)
                Array.Resize(ref _resampleBuffer, newSize);
        }

        // Determine how many samples to try and read: fill up to _resampleBuffer.Length
        // from the current _resampleBufferValidSamples position.
        int samplesToAttemptToRead = _resampleBuffer.Length - _resampleBufferValidSamples;

        if (samplesToAttemptToRead <= 0)
            return; // No space left in the buffer

        var writeSpan = _resampleBuffer.AsSpan(_resampleBufferValidSamples, samplesToAttemptToRead);
        int numRead = _dataProvider.ReadBytes(writeSpan);

        _resampleBufferValidSamples += numRead;
    }

    /// <summary>
    /// Handles the end-of-stream condition.
    /// </summary>
    protected virtual void HandleEndOfStream(Span<float> remainingOutputBuffer)
    {
        if (IsLooping)
        {
            // Seek also resets _rawSamplePosition, _currentFractionalFrame, and _resampleBufferValidSamples
            Seek(LoopStartSamples);

            if (!remainingOutputBuffer.IsEmpty)
            {
                // Try to fill the rest of the output buffer with the newly looped audio
                GenerateAudio(remainingOutputBuffer);
            }
        }
        else
        {
            State = PlaybackState.Stopped;
            OnPlaybackEnded();
            if (!remainingOutputBuffer.IsEmpty)
            {
                remainingOutputBuffer.Clear();
            }
        }
    }

    /// <summary>
    /// Invokes the PlaybackEnded event.
    /// </summary>
    protected virtual void OnPlaybackEnded()
    {
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
        if (!IsLooping)
        {
            Enabled = false;
            State = PlaybackState.Stopped;
        }
    }

    /// <summary>
    /// Occurs when playback ends.
    /// </summary>
    public event EventHandler<EventArgs>? PlaybackEnded;

    #region Audio Playback Control

    /// <inheritdoc />
    public void Play()
    {
        Enabled = true;
        State = PlaybackState.Playing;
    }

    /// <inheritdoc />
    public void Pause()
    {
        Enabled = false;
        State = PlaybackState.Paused;
    }

    /// <inheritdoc />
    public void Stop()
    {
        State = PlaybackState.Stopped;
        Enabled = false;
        Seek(0);
        _resampleBufferValidSamples = 0;
        _currentFractionalFrame = 0f;
    }

    /// <inheritdoc />
    public bool Seek(TimeSpan time, SeekOrigin seekOrigin = SeekOrigin.Begin)
    {
        if (
            AudioEngine.Channels == 0
            || AudioEngine.Instance.SampleRate == 0
            || time < TimeSpan.Zero && seekOrigin == SeekOrigin.End
        )
            return false;
        if (Duration <= 0)
            return time <= TimeSpan.Zero && Seek(0f);

        switch (seekOrigin)
        {
            case SeekOrigin.Current:
                return Seek((float)(Time + time.TotalSeconds));
            case SeekOrigin.End:
                if (Duration <= 0 || !double.IsNegative(time.TotalSeconds) || Duration + time.TotalSeconds > Duration)
                    return Seek(Duration);

                return Seek((float)(Duration + time.TotalSeconds));
            case SeekOrigin.Begin:
            default:
                return Seek((float)time.TotalSeconds);
        }
    }

    /// <inheritdoc />
    public bool Seek(float time)
    {
        if (AudioEngine.Channels == 0 || AudioEngine.Instance.SampleRate == 0)
            return false;
        int sampleOffset = (int)(time / Duration * _dataProvider.Length);
        return Seek(sampleOffset);
    }

    /// <inheritdoc />
    public bool Seek(int sampleOffset)
    {
        if (!_dataProvider.CanSeek || AudioEngine.Channels == 0)
            return false;

        int maxSeekableSample = _dataProvider.Length > 0 ? _dataProvider.Length - AudioEngine.Channels : 0;
        maxSeekableSample = Math.Max(0, maxSeekableSample);
        sampleOffset = Math.Clamp(sampleOffset, 0, maxSeekableSample);

        // Align to frame boundary if not already
        sampleOffset = sampleOffset / AudioEngine.Channels * AudioEngine.Channels;

        _dataProvider.Seek(sampleOffset);

        _rawSamplePosition = sampleOffset;
        _currentFractionalFrame = 0f;
        _resampleBufferValidSamples = 0;
        return true;
    }

    #endregion

    #region Loop Point Configuration Methods

    /// <inheritdoc />
    public void SetLoopPoints(float startTime, float? endTime = -1f)
    {
        if (AudioEngine.Channels == 0 || AudioEngine.Instance.SampleRate == 0)
            return;

        if (startTime < 0)
            throw new ArgumentOutOfRangeException(nameof(startTime), "Loop start time cannot be negative.");

        float effectiveEndTime = endTime ?? -1f;
        if (Math.Abs(effectiveEndTime - -1f) > 1e-6f && effectiveEndTime < startTime)
            throw new ArgumentOutOfRangeException(
                nameof(endTime),
                "Loop end time must be greater than or equal to start time, or -1."
            );

        LoopStartSamples = (int)(startTime * AudioEngine.Instance.SampleRate * AudioEngine.Channels);
        LoopEndSamples =
            Math.Abs(effectiveEndTime - -1f) < 1e-6f
                ? -1
                : (int)(effectiveEndTime * AudioEngine.Instance.SampleRate * AudioEngine.Channels);

        // Align to frame boundaries and clamp
        LoopStartSamples = (LoopStartSamples / AudioEngine.Channels) * AudioEngine.Channels;
        LoopStartSamples = Math.Clamp(LoopStartSamples, 0, _dataProvider.Length);

        if (LoopEndSamples != -1)
        {
            LoopEndSamples = LoopEndSamples / AudioEngine.Channels * AudioEngine.Channels;
            LoopEndSamples = Math.Clamp(LoopEndSamples, LoopStartSamples, _dataProvider.Length);
        }
    }

    /// <inheritdoc />
    public void SetLoopPoints(int startSample, int endSample = -1)
    {
        if (AudioEngine.Channels == 0)
            return;

        if (startSample < 0)
            throw new ArgumentOutOfRangeException(nameof(startSample), "Loop start sample cannot be negative.");
        if (endSample != -1 && endSample < startSample)
            throw new ArgumentOutOfRangeException(
                nameof(endSample),
                "Loop end sample must be greater than or equal to start sample, or -1."
            );

        LoopStartSamples = (startSample / AudioEngine.Channels) * AudioEngine.Channels;
        LoopStartSamples = Math.Clamp(LoopStartSamples, 0, _dataProvider.Length);

        if (endSample != -1)
        {
            LoopEndSamples = (endSample / AudioEngine.Channels) * AudioEngine.Channels;
            LoopEndSamples = Math.Clamp(LoopEndSamples, LoopStartSamples, _dataProvider.Length);
        }
        else
        {
            LoopEndSamples = -1;
        }
    }

    /// <inheritdoc />
    public void SetLoopPoints(TimeSpan startTime, TimeSpan? endTime = null)
    {
        SetLoopPoints((float)startTime.TotalSeconds, (float?)endTime?.TotalSeconds);
    }
    #endregion
}
