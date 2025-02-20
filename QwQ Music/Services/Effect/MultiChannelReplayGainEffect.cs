using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 多声道回放增益效果器
/// </summary>
public class MultiChannelReplayGainEffect : AudioEffectBase
{
    private float[]? _channelGains; // 各声道增益值
    private int _channels;         // 声道数
    private readonly Lock _lock = new(); // 确保线程安全

    /// <summary>
    /// 构造函数（支持多声道增益数组）
    /// </summary>
    /// <param name="gains">各声道增益数组</param>
    public MultiChannelReplayGainEffect(float[] gains)
    {
        base.SetParameter("Gains", gains);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        lock (_lock)
        {
            _channels = Source.WaveFormat.Channels;
            float[] gains = base.GetParameter<float[]>("Gains");
            _channelGains = AdaptGains(gains, _channels);
        }
    }

    /// <summary>
    /// 自动适配增益数组到目标声道数
    /// </summary>
    /// <param name="inputGains">输入增益数组</param>
    /// <param name="targetChannels">目标声道数</param>
    /// <returns>适配后的增益数组</returns>
    private static float[] AdaptGains(float[] inputGains, int targetChannels)
    {
        if (inputGains.Length == targetChannels)
            return inputGains.ToArray();

        // 根据目标声道数生成新的增益数组
        float[] adaptedGains = new float[targetChannels];
        for (int i = 0; i < targetChannels; i++)
        {
            adaptedGains[i] = i < inputGains.Length ? inputGains[i] : 1f; // 默认增益为 1.0
        }
        return adaptedGains;
    }

    public override string Name => "Multi-Channel Replay Gain";

    /// <summary>
    /// 读取音频数据并应用增益
    /// </summary>
    /// <param name="buffer">音频缓冲区</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="count">要读取的样本数</param>
    /// <returns>实际读取的样本数</returns>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled) // 如果效果器未启用，直接返回原始数据
        {
            return Source.Read(buffer, offset, count);
        }

        int samplesRead = Source.Read(buffer, offset, count);

        lock (_lock)
        {
            if (_channelGains == null || _channels <= 0)
                return samplesRead;

            // 遍历样本并应用增益
            for (int n = 0; n < samplesRead; n += _channels)
            {
                for (int c = 0; c < _channels; c++)
                {
                    int index = offset + n + c;
                    if (index >= buffer.Length) continue;
                    buffer[index] *= _channelGains[c];
                }
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new MultiChannelReplayGainEffect(GetParameter<float[]>("Gains"))
        {
            Enabled = Enabled,
            Priority = Priority,
        };
        return clone;
    }

    /// <summary>
    /// 返回当前效果器的调试信息
    /// </summary>
    public override string DebugInfo
    {
        get
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Name: {Name}");
                sb.AppendLine($"Enabled: {Enabled}");
                sb.AppendLine($"Priority: {Priority}");
                sb.AppendLine($"Channels: {_channels}");
                sb.AppendLine("Channel Gains:");
                if (_channelGains != null)
                {
                    for (int i = 0; i < _channelGains.Length; i++)
                    {
                        sb.AppendLine($"  Channel {i}: {_channelGains[i]:F2}");
                    }
                }
                else
                {
                    sb.AppendLine("  No gains set.");
                }
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// 设置效果器的配置参数
    /// </summary>
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        switch (key.ToLower())
        {
            case "gains":
                if (value is float[] gains)
                {
                    lock (_lock)
                    {
                        _channelGains = AdaptGains(gains, _channels);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// 获取效果器的配置参数
    /// </summary>
    public override T GetParameter<T>(string key)
    {
        if (typeof(T) != typeof(float[]))
        {
            throw new ArgumentException($"Unsupported type for parameter '{key}'. Expected float[].");
        }

        return key.ToLower() switch
        {
            "gains" => (T)(object)(_channelGains ?? []),
            _ => base.GetParameter<T>(key),
        };
    }
}