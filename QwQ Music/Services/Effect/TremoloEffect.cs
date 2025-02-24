using System;
using System.Threading;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 颤音效果器
/// </summary>
public sealed class TremoloEffect : AudioEffectBase
{
    private float _frequencyHz;
    private float _depth;
    private double _phase;
    private readonly Lock _lock = new(); // 确保线程安全

    public override string Name => "Tremolo";
    
    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetParameter("FrequencyHz", 5f); // 默认调制频率
        SetParameter("Depth", 0.5f);     // 默认调制深度
    }

    /// <summary>
    /// 读取音频数据并应用颤音效果
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled) // 如果效果器未启用，直接返回原始数据
        {
            return Source.Read(buffer, offset, count);
        }

        int samplesRead = Source.Read(buffer, offset, count);

        lock (_lock)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                float modulation = 1 - _depth + _depth * (float)Math.Sin(_phase);
                buffer[offset + i] *= modulation;

                _phase += 2 * Math.PI * _frequencyHz / WaveFormat.SampleRate;
                if (_phase > 2 * Math.PI) _phase -= 2 * Math.PI;
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new TremoloEffect();
        clone.SetParameter("FrequencyHz", _frequencyHz);
        clone.SetParameter("Depth", _depth);
        clone.Enabled = Enabled;
        clone.Priority = Priority;
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
                case "frequencyhz":
                    _frequencyHz = Convert.ToSingle(value);
                    break;
                case "depth":
                    _depth = Convert.ToSingle(value);
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
                case "frequencyhz":
                    return (T)(object)_frequencyHz;
                case "depth":
                    return (T)(object)_depth;
                default:
                    return base.GetParameter<T>(key);
            }
        }
    }
}