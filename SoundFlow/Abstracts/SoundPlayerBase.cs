using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Interfaces;

namespace SoundFlow.Abstracts;

/// <summary>
/// Abstract base class for sound players, providing common functionality.
/// </summary>
public abstract class SoundPlayerBase : SoundComponent, ISoundPlayer
{
    private readonly Lock _stateLock = new();
    private volatile bool _isSeeking;
    private volatile bool _isStateChanging;
    private readonly ISoundDataProvider _dataProvider;
    private readonly int _processingBufferSize;
    private float[] _processingBuffer;
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
    public float Time
    {
        get
        {
            if (AudioEngine.Channels == 0)
                return 0f;

            lock (_stateLock)
            {
                // 计算当前播放位置（考虑播放速度）
                float currentPosition = _rawSamplePosition / (float)AudioEngine.Channels;
                if (_playbackSpeed != 0f)
                {
                    currentPosition /= _playbackSpeed;
                }
                return currentPosition / AudioEngine.Instance.SampleRate;
            }
        }
    }

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
        _processingBufferSize = 4096;
        _processingBuffer = new float[_processingBufferSize];
    }

    /// <inheritdoc />
    protected override void GenerateAudio(Span<float> output)
    {
        // 快速检查状态，避免不必要的锁
        if (State != PlaybackState.Playing || AudioEngine.Channels == 0)
        {
            output.Clear();
            return;
        }

        int channels = AudioEngine.Channels;
        if (channels == 0)
        {
            output.Clear();
            return;
        }

        // 如果正在切歌，使用处理缓冲区
        if (_isSeeking)
        {
            // 使用处理缓冲区来平滑过渡
            int framesToProcess = Math.Min(output.Length / channels, _processingBufferSize / channels);
            if (framesToProcess > 0)
            {
                // 使用淡出效果
                float fadeStep = 1.0f / framesToProcess;
                for (int i = 0; i < framesToProcess; i++)
                {
                    float fadeMultiplier = 1.0f - i * fadeStep;
                    for (int ch = 0; ch < channels; ch++)
                    {
                        int index = i * channels + ch;
                        if (index < _processingBuffer.Length)
                        {
                            output[index] = _processingBuffer[index] * fadeMultiplier;
                        }
                    }
                }
                // 清空剩余部分
                output[(framesToProcess * channels)..].Clear();
            }
            else
            {
                output.Clear();
            }
            return;
        }

        // 确保时间拉伸器有正确的通道数
        if (_timeStretcher.GetTargetSpeed() == 0f && _playbackSpeed != 0f && channels > 0)
        {
            lock (_stateLock)
            {
                _timeStretcher.SetChannels(channels);
            }
        }

        // 安全检查：确保 _currentFractionalFrame 在合理范围内
        if (_currentFractionalFrame < 0 || float.IsNaN(_currentFractionalFrame) || float.IsInfinity(_currentFractionalFrame))
        {
            _currentFractionalFrame = 0;
        }

        int outputFramesTotal = output.Length / channels;
        int outputBufferOffset = 0;
        int totalSourceSamplesAdvancedThisCall = 0;

        // 限制最大帧数，防止缓冲区溢出
        const int maxFramesPerCall = 4096;
        outputFramesTotal = Math.Min(outputFramesTotal, maxFramesPerCall);

        // 使用处理缓冲区来存储当前帧
        if (_processingBuffer.Length < output.Length)
        {
            Array.Resize(ref _processingBuffer, output.Length);
        }

        for (int i = 0; i < outputFramesTotal; i++)
        {
            // 如果正在切歌，使用淡出效果
            if (_isSeeking)
            {
                float fadeMultiplier = 1.0f - (float)i / outputFramesTotal;
                for (int ch = 0; ch < channels; ch++)
                {
                    int index = i * channels + ch;
                    if (index < _processingBuffer.Length)
                    {
                        output[index] = _processingBuffer[index] * fadeMultiplier;
                    }
                }
                continue;
            }

            int currentIntegerFrame = (int)Math.Floor(_currentFractionalFrame);

            // 安全检查：确保 currentIntegerFrame 不会导致缓冲区溢出
            if (currentIntegerFrame < 0 || currentIntegerFrame > int.MaxValue / channels - 2)
            {
                _currentFractionalFrame = 0;
                currentIntegerFrame = 0;
            }

            // We need 2 frames for linear interpolation (current and next).
            int samplesRequiredInBufferForInterpolation = (currentIntegerFrame + 2) * channels;

            // 安全检查：确保请求的样本数在合理范围内
            if (samplesRequiredInBufferForInterpolation <= 0 || samplesRequiredInBufferForInterpolation > int.MaxValue / 2)
            {
                _currentFractionalFrame = 0;
                samplesRequiredInBufferForInterpolation = 2 * channels;
            }

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

        // 更新原始样本位置
        _rawSamplePosition += totalSourceSamplesAdvancedThisCall;
        _rawSamplePosition = Math.Min(_rawSamplePosition, _dataProvider.Length);

        // 保存当前帧到处理缓冲区
        output[..Math.Min(output.Length, _processingBuffer.Length)].CopyTo(_processingBuffer);

    }

    /// <summary>
    /// Fills the internal resample buffer using the time stretcher and data provider.
    /// </summary>
    /// <param name="minSamplesRequiredInOutputBuffer">Minimum samples needed in _resampleBuffer.</param>
    /// <returns>The total number of original source samples advanced by this fill operation.</returns>
    private int FillResampleBuffer(int minSamplesRequiredInOutputBuffer)
    {
        if (minSamplesRequiredInOutputBuffer <= 0)
            return 0;

        int channels = AudioEngine.Channels;
        if (channels == 0)
            return 0;

        // 确保请求的样本数不超过缓冲区最大可能大小
        minSamplesRequiredInOutputBuffer = Math.Min(minSamplesRequiredInOutputBuffer, int.MaxValue / channels);

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
                    // TODO: 临时修复缓冲区越久问题，可能导致播放不连贯
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
                _resampleBuffer.AsSpan(0, toCopy).CopyTo(remainingOutputBuffer[..toCopy]);
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
                    remainingOutputBuffer[toCopy..].Clear(); // Clear any unfilled part.
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
        if (_isStateChanging)
            return;

        try
        {
            _isStateChanging = true;
            lock (_stateLock)
            {
                Enabled = true;
                State = PlaybackState.Playing;
            }
        }
        finally
        {
            _isStateChanging = false;
        }
    }

    /// <inheritdoc />
    public void Pause()
    {
        if (_isStateChanging)
            return;

        try
        {
            _isStateChanging = true;
            lock (_stateLock)
            {
                Enabled = false;
                State = PlaybackState.Paused;
            }
        }
        finally
        {
            _isStateChanging = false;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (_isStateChanging)
            return;

        try
        {
            _isStateChanging = true;
            lock (_stateLock)
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
                _currentFractionalFrame = 0;
                _rawSamplePosition = 0;
                _loopingSeekPending = false;
            }
        }
        finally
        {
            _isStateChanging = false;
        }
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
        // 如果已经在切歌或状态改变中，返回 false
        if (_isSeeking || _isStateChanging)
            return false;

        try
        {
            _isStateChanging = true;

            if (sampleOffset < 0 || sampleOffset > _dataProvider.Length)
                return false;

            // 在切歌前重置所有状态
            lock (_stateLock)
            {
                _isSeeking = true;
                _timeStretcher.Reset();
                _resampleBufferValidSamples = 0;
                Array.Clear(_resampleBuffer, 0, _resampleBuffer.Length);
                _timeStretcherInputBufferValidSamples = 0;
                _timeStretcherInputBufferReadOffset = 0;
                Array.Clear(_timeStretcherInputBuffer, 0, _timeStretcherInputBuffer.Length);
                _currentFractionalFrame = 0;
                _rawSamplePosition = sampleOffset;
                _loopingSeekPending = false;

                // 重置数据提供者的位置
                try
                {
                    _dataProvider.Seek(sampleOffset);
                }
                catch
                {
                    _rawSamplePosition = Math.Min(_rawSamplePosition, _dataProvider.Length);
                    return false;
                }

                // 确保时间拉伸器使用正确的通道数
                if (AudioEngine.Channels > 0)
                    _timeStretcher.SetChannels(AudioEngine.Channels);
            }

            return true;
        }
        finally
        {
            _isSeeking = false;
            _isStateChanging = false;
        }
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
