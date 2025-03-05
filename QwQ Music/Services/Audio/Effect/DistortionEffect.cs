using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 失真效果器
/// </summary>
public class DistortionEffect : AudioEffectBase
{
    private float _drive;
    private float _mix;
    private readonly Lock _lock = new(); // 确保线程安全

    public override string Name => "Distortion";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetParameter("Drive", 10f); // 默认驱动强度
        SetParameter("Mix", 0.5f); // 默认混音比例
    }

    /// <summary>
    /// 读取音频数据并应用失真效果
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
                float sample = buffer[offset + i];
                float distorted = MathF.Tanh(sample * _drive);
                sample = sample * (1 - _mix) + distorted * _mix;
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
        var clone = new DistortionEffect();
        clone.SetParameter("Drive", _drive);
        clone.SetParameter("Mix", _mix);
        clone.Enabled = Enabled;
        clone.Priority = Priority;
        return clone;
    }

    /// <summary>
    /// 设置效果器的配置参数
    /// </summary>
    public override sealed void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        lock (_lock)
        {
            switch (key.ToLower())
            {
                case "drive":
                    _drive = Convert.ToSingle(value);
                    break;
                case "mix":
                    _mix = Convert.ToSingle(value);
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
                case "drive":
                    return (T)(object)_drive;
                case "mix":
                    return (T)(object)_mix;
                default:
                    return base.GetParameter<T>(key);
            }
        }
    }
}
