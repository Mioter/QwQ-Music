using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 失真效果器
/// </summary>
public class DistortionEffect : AudioEffectBase
{
    // 使用volatile修饰引用类型字段
    private volatile DistortionParameters _currentParams = new();

    public override string Name => "Distortion";

    /// <summary>
    /// 音频处理核心逻辑
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        // 直接读取volatile字段（已保证内存屏障）
        var paramsCopy = _currentParams;
        int samplesRead = Source.Read(buffer, offset, count);

        // 无锁处理循环
        for (int i = 0; i < samplesRead; i++)
        {
            float sample = buffer[offset + i];
            float distorted = MathF.Tanh(sample * paramsCopy.Drive);
            sample = sample * paramsCopy.DryMix + distorted * paramsCopy.WetMix;
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

        var newParams = _currentParams.Clone();
        switch (key.ToLower())
        {
            case "drive":
                newParams.Drive = ValidateDrive(Convert.ToSingle(value));
                break;
            case "mix":
                newParams.Mix = ValidateMix(Convert.ToSingle(value));
                break;
        }

        Interlocked.Exchange(ref _currentParams, newParams); // 原子替换
    }

    /// <summary>
    /// 参数验证
    /// </summary>
    private static float ValidateDrive(float value) => Math.Max(0.1f, value);

    private static float ValidateMix(float value) => Math.Clamp(value, 0f, 1f);

    /// <summary>
    /// 深度克隆
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new DistortionEffect
        {
            _currentParams = _currentParams.Clone(),
            Enabled = Enabled,
            Priority = Priority,
        };
        return clone;
    }

    /// <summary>
    /// 参数存储结构
    /// </summary>
    [Serializable]
    private class DistortionParameters : ICloneable
    {
        public float Drive = 10f; // 驱动强度
        public float Mix = 0.5f; // 混音比例

        // 预计算混音参数
        public float WetMix => Mix;
        public float DryMix => 1f - Mix;

        public DistortionParameters Clone()
        {
            return new DistortionParameters { Drive = Drive, Mix = Mix };
        }

        object ICloneable.Clone() => Clone();
    }
}
