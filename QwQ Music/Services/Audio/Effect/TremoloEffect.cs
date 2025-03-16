using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 颤音效果器
/// </summary>
public sealed class TremoloEffect : AudioEffectBase
{
    // 原子参数更新
    private volatile TremoloParameters _currentParams = new();
    private TremoloParameters _nextParams = new();

    // 相位状态（线程安全）
    private double _phase;
    private readonly Lock _phaseLock = new();

    public override string Name => "Tremolo";

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _currentParams.SampleRate = WaveFormat.SampleRate;

        SetParameter("FrequencyHz", 5f); // 默认5Hz调制
        SetParameter("Depth", 0.5f); // 默认50%深度
    }

    /// <summary>
    /// 音频处理核心逻辑
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        var paramsCopy = _currentParams;
        int samplesRead = Source.Read(buffer, offset, count);

        lock (_phaseLock) // 保护相位累加器
        {
            for (int i = 0; i < samplesRead; i++)
            {
                // 计算调制信号
                float modulation = CalculateModulation(paramsCopy);

                // 应用调制
                buffer[offset + i] *= modulation;

                // 更新相位
                _phase += paramsCopy.PhaseIncrement;
                if (_phase > MathF.PI * 2)
                    _phase -= MathF.PI * 2;
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 调制信号计算
    /// </summary>
    private float CalculateModulation(TremoloParameters parameters)
    {
        // 预计算的相位增量确保实时性
        return 1 - parameters.Depth + parameters.Depth * MathF.Sin((float)_phase);
    }

    /// <summary>
    /// 参数原子更新
    /// </summary>
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        _nextParams = _currentParams.Clone();
        _nextParams.SampleRate = WaveFormat.SampleRate;
        switch (key.ToLower())
        {
            case "frequencyhz":
                _nextParams.FrequencyHz = ValidateFrequency(Convert.ToSingle(value));
                break;
            case "depth":
                _nextParams.Depth = ValidateDepth(Convert.ToSingle(value));
                break;
        }
        Interlocked.Exchange(ref _currentParams, _nextParams); // 原子替换
    }

    /// <summary>
    /// 参数验证
    /// </summary>
    private static float ValidateFrequency(float value) => Math.Clamp(value, 0.1f, 20f);

    private static float ValidateDepth(float value) => Math.Clamp(value, 0f, 1f);

    /// <summary>
    /// 深度克隆
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new TremoloEffect
        {
            _currentParams = _currentParams.Clone(),
            _nextParams = _nextParams.Clone(),
            Enabled = Enabled,
            Priority = Priority,
        };
        return clone;
    }

    /// <summary>
    /// 参数存储结构
    /// </summary>
    [Serializable]
    private class TremoloParameters : ICloneable
    {
        public float FrequencyHz;
        public float Depth;

        public int SampleRate; // 新增字段

        // 预计算相位增量（优化实时计算）<button class="citation-flag" data-index="4">
        public float PhaseIncrement => 2 * MathF.PI * FrequencyHz / SampleRate;

        public TremoloParameters Clone()
        {
            return new TremoloParameters { FrequencyHz = FrequencyHz, Depth = Depth };
        }

        object ICloneable.Clone() => Clone();
    }
}
