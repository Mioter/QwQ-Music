using SoundFlow.Components;
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
    private bool _loopingSeekPending;
    private readonly WsolaTimeStretcher _timeStretcher;
    private readonly float[] _timeStretcherInputBuffer;
    private int _timeStretcherInputBufferValidSamples;
    private int _timeStretcherInputBufferReadOffset;

    /// <inheritdoc />
    public float PlaybackSpeed
    {
        get => _playbackSpeed;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Playback speed must be greater than zero.");
            if (Math.Abs(_playbackSpeed - value) > 1e-6f)
            {
                _playbackSpeed = value;
                _timeStretcher.SetSpeed(_playbackSpeed);
            }
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

    /// <inheritdoc />
    public float Duration =>
        _dataProvider.Length == 0 || AudioEngine.Channels == 0 || AudioEngine.Instance.SampleRate == 0
            ? 0f
            : (float)_dataProvider.Length / AudioEngine.Channels / AudioEngine.Instance.SampleRate;

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
        int initialChannels = AudioEngine.Channels > 0 ? AudioEngine.Channels : 2;
        int initialSampleRate = AudioEngine.Instance.SampleRate > 0 ? AudioEngine.Instance.SampleRate : 44100;
        int resampleBufferFrames = Math.Max(256, initialSampleRate / 10);
        _resampleBuffer = new float[resampleBufferFrames * initialChannels];
        _timeStretcher = new WsolaTimeStretcher(initialChannels, _playbackSpeed);
        _timeStretcherInputBuffer = new float[
            Math.Max(_timeStretcher.MinInputSamplesToProcess * 2, 8192 * initialChannels)
        ];
    }

    /// <inheritdoc />
    protected override void GenerateAudio(Span<float> output)
    {
        // Clear output if not playing or no channels.
        if (State != PlaybackState.Playing || AudioEngine.Channels == 0)
        {
            output.Clear();
            return;
        }

        int channels = AudioEngine.Channels;
        // Ensure time stretcher has correct channel count.
        if (_timeStretcher.GetTargetSpeed() == 0f && _playbackSpeed != 0f && channels > 0)
            _timeStretcher.SetChannels(channels);

        if (channels == 0)
        {
            output.Clear();
            return;
        }

        int outputFramesTotal = output.Length / channels;
        int outputBufferOffset = 0;
        int totalSourceSamplesAdvancedThisCall = 0; // Total samples advanced in the original source.

        for (int i = 0; i < outputFramesTotal; i++)
        {
            int currentIntegerFrame = (int)Math.Floor(_currentFractionalFrame);
            // We need 2 frames for linear interpolation (current and next).
            int samplesRequiredInBufferForInterpolation = (currentIntegerFrame + 2) * channels;

            // Fill _resampleBuffer if not enough data for interpolation.
            if (_resampleBufferValidSamples < samplesRequiredInBufferForInterpolation)
            {
                int sourceSamplesForFill = FillResampleBuffer(samplesRequiredInBufferForInterpolation);
                totalSourceSamplesAdvancedThisCall += sourceSamplesForFill;

                // If still not enough data after filling, end of stream.
                if (_resampleBufferValidSamples < samplesRequiredInBufferForInterpolation)
                {
                    _rawSamplePosition += totalSourceSamplesAdvancedThisCall;
                    _rawSamplePosition = Math.Min(_rawSamplePosition, _dataProvider.Length);
                    HandleEndOfStream(output[outputBufferOffset..]);
                    return;
                }
            }

            // Perform linear interpolation.
            int frameIndex0 = currentIntegerFrame;
            float t = _currentFractionalFrame - frameIndex0;
            for (int ch = 0; ch < channels; ch++)
            {
                int sampleIndex0 = frameIndex0 * channels + ch;
                int sampleIndex1 = (frameIndex0 + 1) * channels + ch;
                if (sampleIndex1 >= _resampleBufferValidSamples)
                {
                    // If next sample is out of bounds, use current or 0.
                    output[outputBufferOffset + ch] =
                        sampleIndex0 < _resampleBufferValidSamples && sampleIndex0 >= 0
                            ? _resampleBuffer[sampleIndex0]
                            : 0f;
                    continue;
                }

                // If current sample is out of bounds, use 0.
                if (sampleIndex0 < 0)
                {
                    output[outputBufferOffset + ch] = 0f;
                    continue;
                }

                // Interpolate sample value.
                output[outputBufferOffset + ch] =
                    _resampleBuffer[sampleIndex0] * (1.0f - t) + _resampleBuffer[sampleIndex1] * t;
            }

            outputBufferOffset += channels;
            _currentFractionalFrame += 1.0f;

            // Discard consumed samples from the resample buffer.
            int framesConsumedFromResampleBuffer = (int)Math.Floor(_currentFractionalFrame);
            if (framesConsumedFromResampleBuffer > 0)
            {
                int samplesConsumedFromResampleBuf = framesConsumedFromResampleBuffer * channels;

                int actualDiscard = Math.Min(samplesConsumedFromResampleBuf, _resampleBufferValidSamples);
                if (actualDiscard > 0)
                {
                    int remaining = _resampleBufferValidSamples - actualDiscard;
                    if (remaining > 0)
                        // Shift remaining samples to the beginning.
                        Buffer.BlockCopy(
                            _resampleBuffer,
                            actualDiscard * sizeof(float),
                            _resampleBuffer,
                            0,
                            remaining * sizeof(float)
                        );
                    _resampleBufferValidSamples = remaining;
                }

                _currentFractionalFrame -= framesConsumedFromResampleBuffer;
            }
        }

        // Update raw sample position based on actual source samples advanced.
        _rawSamplePosition += totalSourceSamplesAdvancedThisCall;
        _rawSamplePosition = Math.Min(_rawSamplePosition, _dataProvider.Length);
    }

    /// <summary>
    /// Fills the internal resample buffer using the time stretcher and data provider.
    /// </summary>
    /// <param name="minSamplesRequiredInOutputBuffer">Minimum samples needed in _resampleBuffer.</param>
    /// <returns>The total number of original source samples advanced by this fill operation.</returns>
    private int FillResampleBuffer(int minSamplesRequiredInOutputBuffer)
    {
        int channels = AudioEngine.Channels;
        if (channels == 0)
            return 0;

        // Resize the resampling buffer if too small.
        if (_resampleBuffer.Length < minSamplesRequiredInOutputBuffer)
        {
            Array.Resize(ref _resampleBuffer, Math.Max(minSamplesRequiredInOutputBuffer, _resampleBuffer.Length * 2));
        }

        int totalSourceSamplesRepresented = 0;

        // Loop to fill _resampleBuffer until minimum required samples are met.
        while (_resampleBufferValidSamples < minSamplesRequiredInOutputBuffer)
        {
            int spaceAvailableInResampleBuffer = _resampleBuffer.Length - _resampleBufferValidSamples;
            if (spaceAvailableInResampleBuffer == 0)
                break;

            int availableInStretcherInput = _timeStretcherInputBufferValidSamples - _timeStretcherInputBufferReadOffset;
            bool providerHasMoreData = _dataProvider.Position < _dataProvider.Length;

            // If time stretcher input buffer needs more data and provider has it.
            if (availableInStretcherInput < _timeStretcher.MinInputSamplesToProcess && providerHasMoreData)
            {
                // Shift existing valid data to the beginning of the input buffer.
                if (_timeStretcherInputBufferReadOffset > 0 && availableInStretcherInput > 0)
                {
                    Buffer.BlockCopy(
                        _timeStretcherInputBuffer,
                        _timeStretcherInputBufferReadOffset * sizeof(float),
                        _timeStretcherInputBuffer,
                        0,
                        availableInStretcherInput * sizeof(float)
                    );
                }

                _timeStretcherInputBufferValidSamples = availableInStretcherInput;
                _timeStretcherInputBufferReadOffset = 0;

                // Read more data from the data provider into the time stretcher input buffer.
                int spaceToReadIntoInput = _timeStretcherInputBuffer.Length - _timeStretcherInputBufferValidSamples;
                if (spaceToReadIntoInput > 0)
                {
                    // Ensure we don't exceed buffer bounds
                    if (
                        _timeStretcherInputBufferValidSamples >= 0
                        && _timeStretcherInputBufferValidSamples + spaceToReadIntoInput
                            <= _timeStretcherInputBuffer.Length
                    )
                    {
                        int readFromProvider = _dataProvider.ReadBytes(
                            _timeStretcherInputBuffer.AsSpan(
                                _timeStretcherInputBufferValidSamples,
                                spaceToReadIntoInput
                            )
                        );
                        _timeStretcherInputBufferValidSamples += readFromProvider;
                        availableInStretcherInput = _timeStretcherInputBufferValidSamples;
                        providerHasMoreData = _dataProvider.Position < _dataProvider.Length;
                    }
                }
            }

            // Prepare spans for time stretcher processing.
            var inputSpanForStretcher = ReadOnlySpan<float>.Empty;
            if (availableInStretcherInput > 0)
            {
                inputSpanForStretcher = _timeStretcherInputBuffer.AsSpan(
                    _timeStretcherInputBufferReadOffset,
                    availableInStretcherInput
                );
            }

            var outputSpanForStretcher = _resampleBuffer.AsSpan(
                _resampleBufferValidSamples,
                spaceAvailableInResampleBuffer
            );
            int samplesWrittenToResample,
                samplesConsumedFromStretcherInputBuf,
                sourceSamplesForThisProcessCall;

            // Determine how to call the time stretcher (Process or Flush).
            if (inputSpanForStretcher.IsEmpty && !providerHasMoreData && !_loopingSeekPending)
            {
                samplesWrittenToResample = _timeStretcher.Flush(outputSpanForStretcher);
                samplesConsumedFromStretcherInputBuf = 0;
                sourceSamplesForThisProcessCall = 0;
            }
            else if (
                availableInStretcherInput >= _timeStretcher.MinInputSamplesToProcess
                || inputSpanForStretcher.IsEmpty && providerHasMoreData && !_loopingSeekPending
            )
            {
                // if input is empty but provider has more data, try to process what's already buffered.
                samplesWrittenToResample = _timeStretcher.Process(
                    inputSpanForStretcher,
                    outputSpanForStretcher,
                    out samplesConsumedFromStretcherInputBuf,
                    out sourceSamplesForThisProcessCall
                );
            }
            else if (_loopingSeekPending)
            {
                break;
            }
            else
            {
                break; // Not enough input and not flushing.
            }

            // Update read offset and valid samples for time stretcher input buffer.
            if (samplesConsumedFromStretcherInputBuf > 0)
            {
                _timeStretcherInputBufferReadOffset += samplesConsumedFromStretcherInputBuf;
            }

            // Update resample buffer valid samples and total source samples advanced.
            _resampleBufferValidSamples += samplesWrittenToResample;
            totalSourceSamplesRepresented += sourceSamplesForThisProcessCall;

            // Break if no progress was made and no more data is expected.
            if (
                samplesWrittenToResample == 0
                && samplesConsumedFromStretcherInputBuf == 0
                && !providerHasMoreData
                && !_loopingSeekPending
            )
            {
                if (
                    availableInStretcherInput
                    == _timeStretcherInputBufferValidSamples - _timeStretcherInputBufferReadOffset
                )
                {
                    break;
                }
            }
        }

        return totalSourceSamplesRepresented;
    }

    /// <summary>
    /// Handles the end-of-stream condition, including looping and stopping.
    /// </summary>
    protected virtual void HandleEndOfStream(Span<float> remainingOutputBuffer)
    {
        if (IsLooping)
        {
            int targetLoopStart = Math.Max(0, LoopStartSamples);
            int actualLoopEnd =
                LoopEndSamples == -1 ? _dataProvider.Length : Math.Min(LoopEndSamples, _dataProvider.Length);

            if (targetLoopStart < actualLoopEnd && targetLoopStart < _dataProvider.Length)
            {
                _loopingSeekPending = true;
                Seek(targetLoopStart);
                _loopingSeekPending = false;
                if (!remainingOutputBuffer.IsEmpty)
                    GenerateAudio(remainingOutputBuffer);
                return;
            }
        }

        // If not looping or loop points are invalid, fill remaining buffer with what's left and stop.
        if (!remainingOutputBuffer.IsEmpty)
        {
            int spaceToFill = remainingOutputBuffer.Length;
            int currentlyValidInResample = _resampleBufferValidSamples;

            // Attempt one last fill of the resample buffer.
            if (currentlyValidInResample < spaceToFill)
            {
                int sourceSamplesFromFinalFill = FillResampleBuffer(Math.Max(currentlyValidInResample, spaceToFill));
                _rawSamplePosition += sourceSamplesFromFinalFill;
                _rawSamplePosition = Math.Min(_rawSamplePosition, _dataProvider.Length);
            }

            // Copy remaining valid samples to output and clear the rest.
            int toCopy = Math.Min(spaceToFill, _resampleBufferValidSamples);
            if (toCopy > 0)
            {
                _resampleBuffer.AsSpan(0, toCopy).CopyTo(remainingOutputBuffer.Slice(0, toCopy));
                int remainingInResampleAfterCopy = _resampleBufferValidSamples - toCopy;
                if (remainingInResampleAfterCopy > 0)
                {
                    // Shift remaining samples in resample buffer.
                    Buffer.BlockCopy(
                        _resampleBuffer,
                        toCopy * sizeof(float),
                        _resampleBuffer,
                        0,
                        remainingInResampleAfterCopy * sizeof(float)
                    );
                }

                _resampleBufferValidSamples = remainingInResampleAfterCopy;
                if (toCopy < spaceToFill)
                {
                    remainingOutputBuffer.Slice(toCopy).Clear(); // Clear any unfilled part.
                }
            }
            else
            {
                remainingOutputBuffer.Clear(); // No valid samples, clear entire buffer.
            }
        }

        State = PlaybackState.Stopped;
        OnPlaybackEnded();
    }

    /// <summary>
    /// Invokes the PlaybackEnded event.
    /// </summary>
    protected virtual void OnPlaybackEnded()
    {
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
        bool isEffectivelyLooping =
            IsLooping
            && (LoopEndSamples == -1 || LoopStartSamples < LoopEndSamples)
            && LoopStartSamples < _dataProvider.Length;
        // If not effectively looping, disable the component.
        if (!isEffectivelyLooping)
            Enabled = false;
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
        _timeStretcher.Reset();
        _resampleBufferValidSamples = 0;
        Array.Clear(_resampleBuffer, 0, _resampleBuffer.Length);
        _timeStretcherInputBufferValidSamples = 0;
        _timeStretcherInputBufferReadOffset = 0;
        Array.Clear(_timeStretcherInputBuffer, 0, _timeStretcherInputBuffer.Length);
        _currentFractionalFrame = 0f;
    }

    /// <inheritdoc />
    public bool Seek(TimeSpan time, SeekOrigin seekOrigin = SeekOrigin.Begin)
    {
        if (AudioEngine.Channels == 0 || AudioEngine.Instance.SampleRate == 0)
            return false;
        float targetTimeSeconds;
        float currentDuration = Duration;
        switch (seekOrigin)
        {
            case SeekOrigin.Begin:
                targetTimeSeconds = (float)time.TotalSeconds;
                break;
            case SeekOrigin.Current:
                targetTimeSeconds = Time + (float)time.TotalSeconds;
                break;
            case SeekOrigin.End:
                // If duration is 0, treat as seeking relative to 0.
                targetTimeSeconds = (currentDuration > 0 ? currentDuration : 0) + (float)time.TotalSeconds;
                break;
            default:
                return false;
        }

        // Clamp target time within valid duration.
        targetTimeSeconds =
            currentDuration > 0 ? Math.Clamp(targetTimeSeconds, 0, currentDuration) : Math.Max(0, targetTimeSeconds);
        return Seek(targetTimeSeconds);
    }

    /// <inheritdoc />
    public bool Seek(float timeInSeconds)
    {
        if (AudioEngine.Channels == 0 || AudioEngine.Instance.SampleRate == 0)
            return false;
        timeInSeconds = Math.Max(0, timeInSeconds);
        // Convert time in seconds to sample offset in source data.
        int sampleOffset = (int)(timeInSeconds / Duration * _dataProvider.Length);
        return Seek(sampleOffset);
    }

    /// <inheritdoc />
    public bool Seek(int sampleOffset)
    {
        if (!_dataProvider.CanSeek || AudioEngine.Channels == 0)
            return false;

        int maxSeekableSample = _dataProvider.Length > 0 ? _dataProvider.Length - AudioEngine.Channels : 0;
        maxSeekableSample = Math.Max(0, maxSeekableSample);
        // Align sample offset to frame boundary.
        sampleOffset = sampleOffset / AudioEngine.Channels * AudioEngine.Channels;
        sampleOffset = Math.Clamp(sampleOffset, 0, maxSeekableSample);
        _dataProvider.Seek(sampleOffset);
        _rawSamplePosition = sampleOffset;
        _currentFractionalFrame = 0f;
        _resampleBufferValidSamples = 0;
        _timeStretcher.Reset();
        _timeStretcherInputBufferValidSamples = 0;
        _timeStretcherInputBufferReadOffset = 0;
        return true;
    }

    #endregion

    #region Loop Point Configuration Methods

    /// <inheritdoc />
    public void SetLoopPoints(float startTime, float? endTime = null)
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

        // Convert seconds to samples.
        LoopStartSamples = (int)(startTime * AudioEngine.Instance.SampleRate * AudioEngine.Channels);
        LoopEndSamples =
            Math.Abs(effectiveEndTime - -1f) < 1e-6f
                ? -1
                : (int)(effectiveEndTime * AudioEngine.Instance.SampleRate * AudioEngine.Channels);

        // Align to frame boundaries and clamp within data provider length.
        LoopStartSamples = LoopStartSamples / AudioEngine.Channels * AudioEngine.Channels;
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

        // Align to frame boundaries and clamp.
        LoopStartSamples = startSample / AudioEngine.Channels * AudioEngine.Channels;
        LoopStartSamples = Math.Clamp(LoopStartSamples, 0, _dataProvider.Length);

        if (endSample != -1)
        {
            endSample = Math.Max(startSample, endSample);
            LoopEndSamples = endSample / AudioEngine.Channels * AudioEngine.Channels;
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
