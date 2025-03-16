using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 延迟效果器
/// </summary>
public class DelayEffect : AudioEffectBase
{
    // 延迟参数（原子更新）
    private volatile DelayParameters _currentParams = new();
    private DelayParameters _nextParams = new();

    // 延迟缓冲区（每个通道独立）
    private float[][] _delayBuffers = [];
    private int[] _writePositions = [];
    private readonly Lock _bufferLock = new();

    public override string Name => "Delay";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetParameter("DelayMs", 200f); // 延迟时间（毫秒）
        SetParameter("Feedback", 0.5f); // 反馈系数（0-1）
        SetParameter("Mix", 0.5f); // 干湿混合比例（0-1）
        InitializeBuffers();
    }

    /// <summary>
    /// 初始化/重置延迟缓冲区（线程安全）
    /// </summary>
    private void InitializeBuffers()
    {
        lock (_bufferLock)
        {
            int channels = WaveFormat.Channels;
            int bufferSize = CalculateBufferSize(_currentParams.DelayMs);

            // 创建新缓冲区
            float[][]? newBuffers = new float[channels][];
            int[]? newPositions = new int[channels];

            for (int ch = 0; ch < channels; ch++)
            {
                newBuffers[ch] = new float[bufferSize];
            }

            // 原子替换缓冲区
            _delayBuffers = newBuffers;
            _writePositions = newPositions;
        }
    }

    /// <summary>
    /// 音频处理核心逻辑（修复越界问题）
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        var paramsCopy = _currentParams; // 原子读取参数
        int samplesRead = Source.Read(buffer, offset, count);
        int channels = WaveFormat.Channels;

        lock (_bufferLock)
        {
            // 确保缓冲区已初始化
            if (_delayBuffers.Length != channels)
                InitializeBuffers();

            for (int i = 0; i < samplesRead; )
            {
                for (int ch = 0; ch < channels; ch++)
                {
                    // 计算延迟样本数
                    int delaySamples = CalculateDelaySamples(paramsCopy.DelayMs);
                    float[] bufferCh = _delayBuffers[ch];
                    int bufferLength = bufferCh.Length;

                    // 计算读取位置（修复负数问题）
                    int readPos = _writePositions[ch] - delaySamples;
                    readPos = (readPos % bufferLength + bufferLength) % bufferLength;

                    // 获取延迟样本
                    float delayedSample = bufferCh[readPos];

                    // 混合信号
                    int sampleIndex = offset + i;
                    float dry = buffer[sampleIndex];
                    float wet = delayedSample * paramsCopy.Mix;
                    buffer[sampleIndex] = dry * (1 - paramsCopy.Mix) + wet;

                    // 写入新样本
                    bufferCh[_writePositions[ch]] = dry + delayedSample * paramsCopy.Feedback;

                    // 更新写指针
                    _writePositions[ch] = (_writePositions[ch] + 1) % bufferLength;
                    i++;
                }
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 参数原子更新
    /// </summary>
    public override sealed void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        lock (_bufferLock)
        {
            _nextParams = _currentParams.Clone();
            switch (key.ToLower())
            {
                case "delayms":
                    float newDelay = ValidateDelayMs(Convert.ToSingle(value));
                    _nextParams.DelayMs = newDelay;
                    InitializeBuffers(); // 重置缓冲区
                    break;
                case "feedback":
                    _nextParams.Feedback = ValidateFeedback(Convert.ToSingle(value));
                    break;
                case "mix":
                    _nextParams.Mix = ValidateMix(Convert.ToSingle(value));
                    break;
            }
            Interlocked.Exchange(ref _currentParams, _nextParams);
        }
    }

    /// <summary>
    /// 计算需要的缓冲区大小（样本数）
    /// </summary>
    private int CalculateBufferSize(float delayMs)
    {
        return (int)Math.Ceiling(delayMs * WaveFormat.SampleRate / 1000f);
    }

    /// <summary>
    /// 计算延迟对应的样本数
    /// </summary>
    private int CalculateDelaySamples(float delayMs)
    {
        return (int)(delayMs * WaveFormat.SampleRate / 1000f);
    }

    /// <summary>
    /// 参数验证
    /// </summary>
    private static float ValidateDelayMs(float value) => Math.Clamp(value, 0f, 5000f);

    private static float ValidateFeedback(float value) => Math.Clamp(value, 0f, 0.707f);

    private static float ValidateMix(float value) => Math.Clamp(value, 0f, 1f);

    /// <summary>
    /// 深度克隆
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new DelayEffect
        {
            _currentParams = _currentParams.Clone(),
            _nextParams = _nextParams.Clone(),
            Enabled = Enabled,
            Priority = Priority,
        };

        // 深度复制缓冲区
        lock (_bufferLock)
        {
            clone._delayBuffers = new float[_delayBuffers.Length][];
            for (int i = 0; i < _delayBuffers.Length; i++)
            {
                clone._delayBuffers[i] = (float[])_delayBuffers[i].Clone();
            }
            clone._writePositions = (int[])_writePositions.Clone();
        }

        return clone;
    }

    /// <summary>
    /// 参数存储结构
    /// </summary>
    [Serializable]
    private class DelayParameters : ICloneable
    {
        public float DelayMs; // 延迟时间（毫秒）
        public float Feedback; // 反馈系数
        public float Mix; // 湿信号混合比例

        public DelayParameters Clone()
        {
            return new DelayParameters
            {
                DelayMs = DelayMs,
                Feedback = Feedback,
                Mix = Mix,
            };
        }

        object ICloneable.Clone() => Clone();
    }
}
