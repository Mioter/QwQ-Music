using System;
using System.Text;
using System.Threading;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 整体回放增益效果器
/// </summary>
public class GlobalReplayGainEffect : AudioEffectBase
{
    private double _globalGain; // 整体增益值
    private int _channels;     // 声道数
    private readonly Lock _lock = new(); // 确保线程安全

    /// <summary>
    /// 构造函数（支持整体增益值）
    /// </summary>
    /// <param name="gain">整体增益值</param>
    public GlobalReplayGainEffect(double gain)
    {
        base.SetParameter("GlobalGain", gain);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        lock (_lock)
        {
            _channels = Source.WaveFormat.Channels;
            _globalGain = base.GetParameter<double>("GlobalGain");
        }
    }

    public override string Name => "Global Replay Gain";

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
            if (_channels <= 0)
                return samplesRead;

            // 遍历样本并应用整体增益
            for (int n = 0; n < samplesRead; n++)
            {
                int index = offset + n;
                if (index >= buffer.Length) continue;
                buffer[index] *= (float)_globalGain; // 应用整体增益
            }
        }
        return samplesRead;
    }

    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new GlobalReplayGainEffect(GetParameter<double>("GlobalGain"))
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
                sb.AppendLine($"Global Gain: {_globalGain:F2}");
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
            case "globalgain":
                if (value is double gain)
                {
                    lock (_lock)
                    {
                        _globalGain = gain;
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
        if (typeof(T) != typeof(double))
        {
            throw new ArgumentException($"Unsupported type for parameter '{key}'. Expected double.");
        }
        return key.ToLower() switch
        {
            "globalgain" => (T)(object)_globalGain,
            _ => base.GetParameter<T>(key),
        };
    }
}