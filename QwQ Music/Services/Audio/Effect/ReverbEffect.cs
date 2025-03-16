using System;
using System.Linq;
using System.Threading;
using NAudio.Dsp;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

public class ReverbEffect : AudioEffectBase
{
    #region 内部缓冲区实现

    /// <summary>
    /// 环形缓冲区基类，提供基础的延迟处理能力
    /// </summary>
    /// <param name="capacity">缓冲区容量</param>
    private abstract class RingBuffer(int capacity)
    {
        protected float[] Buffer = new float[capacity];
        protected int Index;

        // 移动缓冲区索引指针
        protected void Step()
        {
            Index = (Index + 1) % Buffer.Length;
        }
    }

    /// <summary>
    /// 创建预延迟缓冲区
    /// </summary>
    /// <param name="delayMs">延迟时间（毫秒）</param>
    /// <param name="sampleRate">采样率</param>
    private class PreDelayBuffer(int delayMs, int sampleRate) : RingBuffer((int)(delayMs * sampleRate / 1000f))
    {
        public float Process(float input)
        {
            float output = Buffer[Index];
            Buffer[Index] = input;
            Step();
            return output;
        }
    }

    /// <summary>
    /// 梳状滤波器，生成基础回声效果
    /// </summary>
    /// <param name="delayMs">延迟时间（毫秒）</param>
    /// <param name="sampleRate">采样率</param>
    /// <param name="feedback">反馈增益</param>
    /// <param name="dampening">高频衰减系数</param>
    private class CombFilter(int delayMs, int sampleRate, float feedback, float dampening)
        : RingBuffer((int)(delayMs * sampleRate / 1000f))
    {
        private BiQuadFilter _lowPass = BiQuadFilter.LowPassFilter(sampleRate, 5000 * dampening, 1f);

        public float Process(float input)
        {
            float output = _lowPass.Transform(Buffer[Index]);
            float newInput = input + output * feedback;
            Buffer[Index] = newInput;
            Step();
            return output;
        }

        public CombFilter Clone()
        {
            return new CombFilter(Buffer.Length, 1, feedback, 0)
            {
                Buffer = (float[])Buffer.Clone(),
                _lowPass = BiQuadFilter.LowPassFilter(1, 1, 1),
            };
        }
    }

    /// <summary>
    /// 全通滤波器，用于扩散回声效果
    /// </summary>
    private class AllPassFilter(float delayMs, int sampleRate, float gain)
        : RingBuffer((int)(delayMs * sampleRate / 1000f))
    {
        public float Process(float input)
        {
            float output = -gain * input + Buffer[Index];
            Buffer[Index] = input + gain * Buffer[Index];
            Step();
            return output;
        }

        public AllPassFilter Clone()
        {
            return new AllPassFilter(Buffer.Length, 1, gain) { Buffer = (float[])Buffer.Clone() };
        }
    }

    #endregion

    #region 字段与属性

    /// <summary>原子化参数存储</summary>
    private volatile ReverbParameters _currentParams = new();
    private ReverbParameters _nextParams = new();

    private PreDelayBuffer _preDelay = null!;
    private CombFilter[] _combFilters = [];
    private AllPassFilter[] _allPassFilters = [];

    public override string Name => "Reverb";

    #endregion

    #region 初始化逻辑

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        InitializeFilters();
        InitializePreDelay();
    }

    /// <summary>
    /// 初始化预延迟缓冲区
    /// </summary>
    private void InitializePreDelay()
    {
        _preDelay = new PreDelayBuffer((int)_currentParams.PreDelay, WaveFormat.SampleRate);
    }

    /// <summary>
    /// 初始化滤波器组
    /// </summary>
    private void InitializeFilters()
    {
        int sampleRate = WaveFormat.SampleRate;
        float baseDelay = _currentParams.RoomSize * 30f;

        _combFilters =
        [
            new CombFilter((int)(baseDelay * 0.8f), sampleRate, GetDecayFeedback(), _currentParams.Dampening),
            new CombFilter((int)(baseDelay * 1.2f), sampleRate, GetDecayFeedback(), _currentParams.Dampening),
            new CombFilter((int)(baseDelay * 1.5f), sampleRate, GetDecayFeedback(), _currentParams.Dampening),
            new CombFilter((int)(baseDelay * 1.8f), sampleRate, GetDecayFeedback(), _currentParams.Dampening),
        ];

        _allPassFilters = [new AllPassFilter(5, sampleRate, 0.7f), new AllPassFilter(1.7f, sampleRate, 0.7f)];
    }

    #endregion

    #region 核心处理逻辑

    /// <inheritdoc />
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        var paramsCopy = _currentParams;
        int samplesRead = Source.Read(buffer, offset, count);

        // 优化点3：向量化处理准备
        float dry = paramsCopy.DryMix;
        float wet = paramsCopy.WetMix;
        int channels = WaveFormat.Channels;

        // 并行处理每个音频样本
        for (int n = 0; n < samplesRead; n += channels)
        {
            int index = offset + n;
            float inputL = buffer[index];
            float inputR = channels == 2 ? buffer[index + 1] : inputL;
            float monoInput = (inputL + inputR) * 0.5f;

            // 预延迟处理
            monoInput = _preDelay.Process(monoInput);

            // 梳状滤波器处理
            float combSum = _combFilters.Sum(filter => filter.Process(monoInput));
            combSum *= 0.2f; // 4个滤波器平均

            // 全通滤波器处理
            float allPassOut = _allPassFilters.Aggregate(combSum, (current, filter) => filter.Process(current));

            // 混合输出
            buffer[index] = inputL * dry + allPassOut * wet;
            if (channels == 2)
                buffer[index + 1] = inputR * dry + allPassOut * wet;
        }

        return samplesRead;
    }

    #endregion

    #region 参数管理方法

    /// <inheritdoc />
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        _nextParams = _currentParams.Clone();
        switch (key.ToLower())
        {
            case "roomsize":
                _nextParams.RoomSize = ValidateRoomSize(Convert.ToSingle(value));
                break;
            case "decaytime":
                _nextParams.DecayTime = ValidateDecayTime(Convert.ToSingle(value));
                break;
            case "dampening":
                _nextParams.Dampening = ValidateDampening(Convert.ToSingle(value));
                break;
            case "drymix":
                _nextParams.DryMix = ValidateMix(Convert.ToSingle(value));
                break;
            case "wetmix":
                _nextParams.WetMix = ValidateMix(Convert.ToSingle(value));
                break;
            case "predelay":
                _nextParams.PreDelay = ValidatePreDelay(Convert.ToSingle(value));
                break;
        }
        Interlocked.Exchange(ref _currentParams, _nextParams);
    }

    private static float ValidateRoomSize(float value) => Math.Clamp(value, 0.5f, 2.0f);

    private static float ValidateDecayTime(float value) => Math.Clamp(value, 0.1f, 10.0f);

    private static float ValidateDampening(float value) => Math.Clamp(value, 0.0f, 1.0f);

    private static float ValidateMix(float value) => Math.Clamp(value, 0.0f, 1.0f);

    private static float ValidatePreDelay(float value) => Math.Clamp(value, 50f, 200f);

    #endregion

    #region 辅助方法

    /// <summary>
    /// 计算反馈系数（动态范围压缩）
    /// </summary>
    private float GetDecayFeedback()
    {
        float maxFeedback = Math.Clamp(0.95f - (_currentParams.DecayTime - 2f) * 0.05f, 0.7f, 0.95f);
        float feedback = (float)Math.Pow(10.0, -3.0 * _currentParams.PreDelay / (_currentParams.DecayTime * 1000));
        return Math.Clamp(feedback, 0.01f, maxFeedback);
    }

    /// <inheritdoc />
    public override IAudioEffect Clone()
    {
        var clone = new ReverbEffect
        {
            _currentParams = _currentParams.Clone(),
            _preDelay = _preDelay,
            _combFilters = _combFilters.Select(f => f.Clone()).ToArray(),
            _allPassFilters = _allPassFilters.Select(f => f.Clone()).ToArray(),
        };
        return clone;
    }

    #endregion

    #region 参数管理

    /// <summary>
    /// 混响参数存储结构
    /// </summary>
    private class ReverbParameters : ICloneable
    {
        public float RoomSize = 1.0f;
        public float DecayTime = 1.0f;
        public float Dampening = 0.5f;
        public float DryMix = 0.5f;
        public float WetMix = 0.5f;
        public float PreDelay = 50.0f;

        public ReverbParameters Clone()
        {
            return new ReverbParameters
            {
                RoomSize = RoomSize,
                DecayTime = DecayTime,
                Dampening = Dampening,
                DryMix = DryMix,
                WetMix = WetMix,
                PreDelay = PreDelay,
            };
        }

        object ICloneable.Clone() => Clone();
    }
    #endregion
}
