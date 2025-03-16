using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 回放增益效果器
/// </summary>
public class ReplayGainEffect : AudioEffectBase
{
    // 参数键定义（强类型化）
    private static class Parameters
    {
        public const string GlobalGain = nameof(GlobalGain);
    }

    // 线程安全的增益参数
    private double _globalGain;
    private readonly Lock _gainLock = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="initialGain">初始增益值（dB）</param>
    public ReplayGainEffect(double initialGain = 0.0)
    {
        _globalGain = initialGain;
        SetParameter(Parameters.GlobalGain, initialGain);
    }

    public override string Name => "ReplayGain";

    /// <summary>
    /// 音频处理核心逻辑
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        // 原子读取最新增益值
        double currentGain = Volatile.Read(ref _globalGain);
        float gainFactor = (float)Math.Pow(10, currentGain / 20.0); // dB转线性比例

        int samplesRead = Source.Read(buffer, offset, count);

        // 应用增益（无锁循环）
        for (int i = 0; i < samplesRead; i++)
        {
            buffer[offset + i] *= gainFactor;
        }

        return samplesRead;
    }

    /// <summary>
    /// 参数设置（线程安全）
    /// </summary>
    public override sealed void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        if (key == Parameters.GlobalGain)
        {
            lock (_gainLock)
            {
                _globalGain = Convert.ToDouble(value);
            }
        }
    }

    /// <summary>
    /// 参数获取（类型安全）
    /// </summary>
    public override T GetParameter<T>(string key)
    {
        if (key == Parameters.GlobalGain)
            return (T)(object)Volatile.Read(ref _globalGain);

        return base.GetParameter<T>(key);
    }

    /// <summary>
    /// 深度克隆
    /// </summary>
    public override IAudioEffect Clone()
    {
        double currentGain;
        lock (_gainLock)
        {
            currentGain = _globalGain;
        }

        var clone = new ReplayGainEffect(currentGain) { Enabled = Enabled, Priority = Priority };
        return clone;
    }
}
