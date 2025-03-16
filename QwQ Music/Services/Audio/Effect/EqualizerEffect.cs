using System;
using System.Linq;
using System.Threading;
using NAudio.Dsp;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 均衡器效果器
/// </summary>
public class EqualizerEffect : AudioEffectBase
{
    // 原子参数存储
    private volatile EqualizerParameters _currentParams = new();

    public override string Name => "Equalizer";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetParameter(
            "bands",
            new (float frequency, float gain)[]
            {
                (31.25f, 0),
                (62.5f, 0),
                (125f, 0), // 低频段
                (250f, 0),
                (500f, 0),
                (1000f, 0),
                (2000f, 0), // 中频段
                (4000f, 0),
                (8000f, 0),
                (16000f, 0), // 高频段
            }
        ); // 定义均衡器的频段配置（中心频率和增益）

        UpdateFilters(WaveFormat.SampleRate);
    }

    /// <summary>
    /// 音频处理核心逻辑
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled || _currentParams.Filters.Length == 0)
            return Source.Read(buffer, offset, count);

        var paramsCopy = _currentParams; // volatile读取
        int samplesRead = Source.Read(buffer, offset, count);

        // 无锁处理循环
        for (int i = 0; i < samplesRead; i++)
        {
            float sample = buffer[offset + i];
            sample = paramsCopy.Filters.Aggregate(sample, (current, filter) => filter.Transform(current));
            buffer[offset + i] = sample;
        }

        return samplesRead;
    }

    /// <summary>
    /// 参数原子更新
    /// </summary>
    public override sealed void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        if (
            key.Equals("bands", StringComparison.CurrentCultureIgnoreCase)
            && value is (float frequency, float gain)[] bands
        )
        {
            var newParams = new EqualizerParameters
            {
                SampleRate = _currentParams.SampleRate,
                Bands = bands
                    .Select(b => new FilterConfig
                    {
                        Frequency = b.frequency,
                        Gain = b.gain,
                        Q = 1.0f, // 保持原有Q值
                    })
                    .ToArray(),
            };

            UpdateFilters(newParams.SampleRate, newParams.Bands);
        }
    }

    /// <summary>
    /// 深度克隆
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new EqualizerEffect
        {
            _currentParams = _currentParams.Clone(),
            Enabled = Enabled,
            Priority = Priority,
        };
        return clone;
    }

    /// <summary>
    /// 更新滤波器（线程安全）
    /// </summary>
    private void UpdateFilters(int sampleRate, FilterConfig[]? newBands = null)
    {
        var bands = newBands ?? _currentParams.Bands;
        var filters = bands.Select(b => BiQuadFilter.PeakingEQ(sampleRate, b.Frequency, b.Q, b.Gain)).ToArray();

        Interlocked.Exchange(
            ref _currentParams,
            new EqualizerParameters
            {
                SampleRate = sampleRate,
                Bands = bands,
                Filters = filters,
            }
        );
    }

    /// <summary>
    /// 参数存储结构
    /// </summary>
    [Serializable]
    private class EqualizerParameters : ICloneable
    {
        public int SampleRate;
        public FilterConfig[] Bands = [];
        public BiQuadFilter[] Filters = [];

        public EqualizerParameters Clone()
        {
            return new EqualizerParameters
            {
                SampleRate = SampleRate,
                Bands = Bands
                    .Select(b => new FilterConfig
                    {
                        Frequency = b.Frequency,
                        Gain = b.Gain,
                        Q = b.Q,
                    })
                    .ToArray(),
                Filters = Bands.Select(b => BiQuadFilter.PeakingEQ(SampleRate, b.Frequency, b.Q, b.Gain)).ToArray(),
            };
        }

        object ICloneable.Clone() => Clone();
    }

    /// <summary>
    /// 滤波器配置结构
    /// </summary>
    [Serializable]
    private struct FilterConfig
    {
        public float Frequency;
        public float Gain;
        public float Q;
    }
}
