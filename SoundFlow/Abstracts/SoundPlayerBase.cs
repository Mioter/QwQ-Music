using System;
using System.Buffers;
using System.IO;
using SoundFlow.Enums;
using SoundFlow.Interfaces;

namespace SoundFlow.Abstracts;

/// <summary>
/// Abstract base class for sound players, providing common functionality.
/// </summary>
public abstract class SoundPlayerBase : SoundComponent, ISoundPlayer
{
    private readonly ISoundDataProvider _dataProvider;
    private int _samplePosition;
    private float _currentFrame;
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
        (float)_samplePosition / AudioEngine.Channels / AudioEngine.Instance.SampleRate / PlaybackSpeed;

    /// <inheritdoc />
    public float Duration => (float)_dataProvider.Length / AudioEngine.Channels / AudioEngine.Instance.SampleRate /
                             PlaybackSpeed;

    /// <inheritdoc />
    public int LoopStartSamples { get; private set; }

    /// <inheritdoc />
    public int LoopEndSamples { get; private set; } = -1;

    /// <inheritdoc />
    public float LoopStartSeconds => (float)LoopStartSamples / AudioEngine.Channels / AudioEngine.Instance.SampleRate;

    /// <inheritdoc />
    public float LoopEndSeconds => LoopEndSamples == -1
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
    }

    /// <inheritdoc />
    protected override void GenerateAudio(Span<float> output)
    {
        if (State != PlaybackState.Playing)
            return;

        if (IsLooping)
        {
            int loopEnd = LoopEndSamples == -1 ? _dataProvider.Length : LoopEndSamples;
            if (loopEnd > 0 && _samplePosition >= loopEnd)
            {
                Seek(LoopStartSamples);
                _currentFrame = 0f;
                return;
            }
        }

        int channels = AudioEngine.Channels;
        float speed = PlaybackSpeed;
        int outputSampleCount = output.Length;
        int outputFrameCount = outputSampleCount / channels;

        // Calculate the number of source frames required
        int requiredSourceFrames = (int)Math.Ceiling(outputFrameCount * speed) + 2;
        int requiredSourceSamples = requiredSourceFrames * channels;

        float[] sourceSamples = ArrayPool<float>.Shared.Rent(requiredSourceSamples);
        var sourceSpan = sourceSamples.AsSpan(0, requiredSourceSamples);
        int sourceSamplesRead = _dataProvider.ReadBytes(sourceSpan);

        if (sourceSamplesRead == 0)
        {
            ArrayPool<float>.Shared.Return(sourceSamples);
            HandleEndOfStream(output);
            return;
        }

        int sourceFramesRead = sourceSamplesRead / channels;
        int outputFrameIndex = 0;

        // Process output frames with linear interpolation
        while (outputFrameIndex < outputFrameCount && _currentFrame < sourceFramesRead - 1)
        {
            float sourceFrame = _currentFrame;
            int frameIndex0 = (int)sourceFrame;
            float t = sourceFrame - frameIndex0;

            for (int ch = 0; ch < channels; ch++)
            {
                int sampleIndex0 = frameIndex0 * channels + ch;
                int sampleIndex1 = (frameIndex0 + 1) * channels + ch;

                if (sampleIndex1 >= sourceSamplesRead)
                    break;

                float sample0 = sourceSamples[sampleIndex0];
                float sample1 = sourceSamples[sampleIndex1];
                output[outputFrameIndex * channels + ch] = sample0 * (1 - t) + sample1 * t;
            }

            outputFrameIndex++;
            _currentFrame += speed;
        }

        // Clear any remaining output if underflow occurred.
        if (outputFrameIndex < outputFrameCount)
        {
            output.Slice(outputFrameIndex * channels, (outputFrameCount - outputFrameIndex) * channels).Clear();
        }

        // Update playback position.
        int framesConsumed = (int)_currentFrame;
        _samplePosition += framesConsumed * channels;
        _currentFrame -= framesConsumed;

        ArrayPool<float>.Shared.Return(sourceSamples);

        if (framesConsumed >= sourceFramesRead - 1)
            HandleEndOfStream(output[(outputFrameIndex * channels)..]);
    }

    /// <summary>
    /// Handles the end-of-stream condition.
    /// </summary>
    protected virtual void HandleEndOfStream(Span<float> buffer)
    {
        if (IsLooping)
        {
            int loopStart = LoopStartSamples;
            
            // Fallback to loop start if something unexpected
            Seek(loopStart); // Seek to the loop start point

            _currentFrame = 0f;
            GenerateAudio(buffer); // Process the buffer again after seeking.
        }
        else
        {
            State = PlaybackState.Stopped;
            OnPlaybackEnded();
            buffer.Clear();
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
        Pause();
        Seek(0);
    }

    /// <inheritdoc />
    public bool Seek(TimeSpan time, SeekOrigin seekOrigin = SeekOrigin.Begin)
    {
        if (time < TimeSpan.Zero)
            return false;
        
        if (Duration <= 0)
            return time <= TimeSpan.Zero && Seek(0f);
        
        switch (seekOrigin)
        {
            case SeekOrigin.Current:
                return Seek((float)(Time + time.TotalSeconds));
            case SeekOrigin.End:
                if (Duration <= 0 || !double.IsNegative(time.TotalSeconds))
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
        int sampleOffset = (int)(time / Duration * _dataProvider.Length);
        return Seek(sampleOffset);
    }

    /// <inheritdoc />
    public bool Seek(int sampleOffset)
    {
        if (!_dataProvider.CanSeek || sampleOffset < 0 || sampleOffset >= _dataProvider.Length)
            return false;

        _dataProvider.Seek(sampleOffset);
        _samplePosition = sampleOffset;

        // Reset the fractional frame index for interpolation relative to the new stream position.
        _currentFrame = 0f;
        return true;
    }

    #endregion

    #region Loop Point Configuration Methods

    /// <inheritdoc />
    public void SetLoopPoints(float startTime, float? endTime = -1f)
    {
        if (startTime < 0)
            throw new ArgumentOutOfRangeException(nameof(startTime), "Loop start time cannot be negative.");
        if (endTime.HasValue && Math.Abs(endTime.Value - -1f) < 1e-6 && endTime < startTime)
            throw new ArgumentOutOfRangeException(nameof(endTime),
                "Loop end time must be greater than or equal to start time, or -1.");

        LoopStartSamples = (int)(startTime * AudioEngine.Instance.SampleRate * AudioEngine.Channels);
        LoopEndSamples = endTime.HasValue
            ? Math.Abs(endTime.Value - -1) < 1e-6
                ? -1
                : (int)(endTime.Value * AudioEngine.Instance.SampleRate * AudioEngine.Channels)
            : -1;


        // Clamp to valid sample range
        LoopStartSamples = Math.Clamp(LoopStartSamples, 0, _dataProvider.Length);
        LoopEndSamples = LoopEndSamples == -1 ? -1 : Math.Clamp(LoopEndSamples, -1, _dataProvider.Length);
    }

    /// <inheritdoc />
    public void SetLoopPoints(int startSample, int endSample = -1)
    {
        if (startSample < 0)
            throw new ArgumentOutOfRangeException(nameof(startSample), "Loop start sample cannot be negative.");
        if (endSample != -1 && endSample < startSample)
            throw new ArgumentOutOfRangeException(nameof(endSample),
                "Loop end sample must be greater than or equal to start sample, or -1.");

        LoopStartSamples = startSample;
        LoopEndSamples = endSample;

        // Clamp to valid sample range
        LoopStartSamples = Math.Clamp(LoopStartSamples, 0, _dataProvider.Length);
        LoopEndSamples = LoopEndSamples == -1 ? -1 : Math.Clamp(LoopEndSamples, -1, _dataProvider.Length);
    }

    /// <inheritdoc />
    public void SetLoopPoints(TimeSpan startTime, TimeSpan? endTime = null)
    {
        if (startTime < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(startTime), "Loop start time cannot be negative.");
        if (endTime < startTime)
            throw new ArgumentOutOfRangeException(nameof(endTime),
                "Loop end time must be greater than or equal to start time, or null.");

        float startTimeSeconds = (float)startTime.TotalSeconds;
        double? endTimeSeconds = endTime?.TotalSeconds;

        SetLoopPoints(startTimeSeconds, (float?)endTimeSeconds);
    }
    #endregion
}