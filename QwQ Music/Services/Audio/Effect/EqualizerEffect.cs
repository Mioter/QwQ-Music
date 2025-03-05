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
    private BiQuadFilter[]? _filters;
    private (float frequency, float gain)[] _bands; // 保存频段配置
    private readonly Lock _lock = new(); // 确保线程安全

    public override string Name => "Equalizer";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="bands">频段配置（中心频率和增益）</param>
    public EqualizerEffect((float frequency, float gain)[] bands)
    {
        if (bands == null || bands.Length == 0)
            throw new ArgumentException("频段配置不能为空");
        _bands = bands;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _filters = _bands
            .Select(band => BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, band.frequency, 1.0f, band.gain))
            .ToArray();
    }

    /// <summary>
    /// 读取音频数据并应用均衡器效果
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled || _filters == null) // 如果效果器未启用，直接返回原始数据
        {
            return Source.Read(buffer, offset, count);
        }

        int samplesRead = Source.Read(buffer, offset, count);

        lock (_lock)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                float sample = buffer[offset + i];
                sample = _filters.Aggregate(sample, (current, filter) => filter.Transform(current));
                buffer[offset + i] = sample;
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new EqualizerEffect(_bands) { Enabled = Enabled, Priority = Priority }; // 使用保存的频段配置
        return clone;
    }

    /// <summary>
    /// 设置效果器的配置参数
    /// </summary>
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        lock (_lock)
        {
            switch (key.ToLower())
            {
                case "bands":
                    if (value is (float frequency, float gain)[] bands)
                    {
                        _bands = bands;
                        _filters = bands
                            .Select(band =>
                                BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, band.frequency, 1.0f, band.gain)
                            )
                            .ToArray();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 获取效果器的配置参数
    /// </summary>
    public override T GetParameter<T>(string key)
    {
        lock (_lock)
        {
            switch (key.ToLower())
            {
                case "bands":
                    return (T)(object)_bands; // 返回保存的频段配置
                default:
                    return base.GetParameter<T>(key);
            }
        }
    }
}
