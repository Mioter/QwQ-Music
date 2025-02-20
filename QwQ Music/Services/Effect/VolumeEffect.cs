using System;
using System.Text;
using System.Threading;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 音量控制效果器
/// </summary>
public class VolumeEffect : AudioEffectBase
{
    private float _volume = 1.0f; // 音量值
    private readonly Lock _lock = new(); // 确保线程安全

    public override string Name => "Volume";

    /// <summary>
    /// 音量（范围：0.0 到 1.0）
    /// </summary>
    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0.0f, 1.0f);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        lock (_lock)
        {
            if (Source.WaveFormat.Channels > 2)
                throw new NotSupportedException("只支持单声道/立体声");
        }
    }

    /// <summary>
    /// 读取音频数据并应用音量控制
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
            float volume = _volume; // 获取当前音量

            for (int i = 0; i < samplesRead; i++)
            {
                buffer[offset + i] *= volume;
            }
        }

        return samplesRead;
    }
    
    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new VolumeEffect
        {
            Volume = _volume,
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
                sb.AppendLine($"Volume: {_volume:F2}");
                sb.AppendLine($"Channels: {Source.WaveFormat.Channels}");
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
            case "volume":
                if (value is float volume)
                {
                    lock (_lock)
                    {
                        _volume = Math.Clamp(volume, 0.0f, 1.0f);
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
        return key.ToLower() switch
        {
            "volume" => (T)(object)_volume,
            _ => base.GetParameter<T>(key),
        };
    }
}