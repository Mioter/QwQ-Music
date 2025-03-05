using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 压缩器效果器
/// </summary>
public class CompressorEffect : AudioEffectBase
{
    private float _threshold;
    private float _ratio;
    private float _attackMs;
    private float _releaseMs;
    private float _currentLevel;
    private float _gainReduction;
    private readonly Lock _lock = new(); // 确保线程安全

    public override string Name => "Compressor";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetParameter("Threshold", -20f); // 默认阈值
        SetParameter("Ratio", 4f); // 默认压缩比
        SetParameter("AttackMs", 10f); // 默认攻击时间
        SetParameter("ReleaseMs", 100f); // 默认释放时间
    }

    /// <summary>
    /// 读取音频数据并应用压缩器效果
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

                // 计算信号电平（绝对值）
                _currentLevel = Math.Max(_currentLevel, Math.Abs(sample));

                // 计算增益减少量
                float dbLevel = 20 * MathF.Log10(_currentLevel + 1e-6f); // 避免除零
                float overThreshold = dbLevel - _threshold;
                float gainReductionDb = overThreshold > 0 ? overThreshold * (1 - 1 / _ratio) : 0;

                // 平滑增益减少量
                _gainReduction = SmoothGainReduction(gainReductionDb, _gainReduction);

                // 应用增益减少
                sample *= MathF.Pow(10, _gainReduction / 20);
                buffer[offset + i] = sample;

                // 模拟释放时间
                _currentLevel *= MathF.Exp(-1 / (_releaseMs * WaveFormat.SampleRate / 1000));
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 平滑增益减少量
    /// </summary>
    private float SmoothGainReduction(float target, float current)
    {
        float alpha = MathF.Exp(-1 / (_attackMs * WaveFormat.SampleRate / 1000));
        return current + (target - current) * (1 - alpha);
    }

    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new CompressorEffect();
        clone.SetParameter("Threshold", _threshold);
        clone.SetParameter("Ratio", _ratio);
        clone.SetParameter("AttackMs", _attackMs);
        clone.SetParameter("ReleaseMs", _releaseMs);
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
                case "threshold":
                    _threshold = Convert.ToSingle(value);
                    break;
                case "ratio":
                    _ratio = Convert.ToSingle(value);
                    break;
                case "attackms":
                    _attackMs = Convert.ToSingle(value);
                    break;
                case "releasems":
                    _releaseMs = Convert.ToSingle(value);
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
                case "threshold":
                    return (T)(object)_threshold;
                case "ratio":
                    return (T)(object)_ratio;
                case "attackms":
                    return (T)(object)_attackMs;
                case "releasems":
                    return (T)(object)_releaseMs;
                default:
                    return base.GetParameter<T>(key);
            }
        }
    }
}
