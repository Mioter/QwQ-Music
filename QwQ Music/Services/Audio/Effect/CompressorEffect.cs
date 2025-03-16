using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 压缩器效果器
/// </summary>
public class CompressorEffect : AudioEffectBase
{
    // 预计算参数（原子更新）
    private volatile CompressorParameters _currentParams = new();

    // 状态变量（线程安全）
    private float _currentLevel;
    private float _gainReduction;
    private readonly Lock _stateLock = new();

    public override string Name => "Compressor";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetParameter("Threshold", -20f); // dB
        SetParameter("Ratio", 4f); // 压缩比
        SetParameter("AttackMs", 10f); // 攻击时间
        SetParameter("ReleaseMs", 100f); // 释放时间
    }

    /// <summary>
    /// 音频处理核心逻辑
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        var paramsCopy = _currentParams; // 原子读取参数
        int samplesRead = Source.Read(buffer, offset, count);

        lock (_stateLock)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                float sample = buffer[offset + i];
                float absSample = Math.Abs(sample);

                // 包络检测（峰值保持）
                _currentLevel = Math.Max(absSample, _currentLevel * paramsCopy.ReleaseCoeff);

                // 增益计算
                float dbLevel = 20f * MathF.Log10(_currentLevel + 1e-6f);
                float overThreshold = dbLevel - paramsCopy.Threshold;
                float gainReductionDb = overThreshold > 0 ? overThreshold * (1 - 1 / paramsCopy.Ratio) : 0;

                // 平滑增益变化
                _gainReduction += (gainReductionDb - _gainReduction) * paramsCopy.AttackCoeff;

                // 应用增益补偿
                buffer[offset + i] = sample * MathF.Pow(10, (_gainReduction + paramsCopy.MakeupGain) / 20f);
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 参数原子更新
    /// </summary>
    public override sealed void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        lock (_stateLock)
        {
            var newParams = _currentParams.Clone();
            switch (key.ToLower())
            {
                case "threshold":
                    newParams.Threshold = ValidateThreshold(Convert.ToSingle(value));
                    break;
                case "ratio":
                    newParams.Ratio = ValidateRatio(Convert.ToSingle(value));
                    break;
                case "attackms":
                    newParams.AttackCoeff = CalculateAttackCoeff(Convert.ToSingle(value));
                    break;
                case "releasems":
                    newParams.ReleaseCoeff = CalculateReleaseCoeff(Convert.ToSingle(value));
                    break;
            }
            Interlocked.Exchange(ref _currentParams, newParams); // 原子替换
        }
    }

    /// <summary>
    /// 参数预计算
    /// </summary>
    private float CalculateAttackCoeff(float attackMs)
    {
        float attackSamples = attackMs * WaveFormat.SampleRate / 1000f;
        return MathF.Exp(-1f / attackSamples);
    }

    private float CalculateReleaseCoeff(float releaseMs)
    {
        float releaseSamples = releaseMs * WaveFormat.SampleRate / 1000f;
        return MathF.Exp(-1f / releaseSamples);
    }

    /// <summary>
    /// 参数验证
    /// </summary>
    private static float ValidateThreshold(float value) => Math.Clamp(value, -60f, 0f);

    private static float ValidateRatio(float value) => Math.Max(1f, value);

    /// <summary>
    /// 深度克隆
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new CompressorEffect
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
    private class CompressorParameters : ICloneable
    {
        public float Threshold;
        public float Ratio;
        public float AttackCoeff;
        public float ReleaseCoeff;
        public float MakeupGain => 20f * MathF.Log10(Ratio); // 自动增益补偿

        public CompressorParameters Clone()
        {
            return new CompressorParameters
            {
                Threshold = Threshold,
                Ratio = Ratio,
                AttackCoeff = AttackCoeff,
                ReleaseCoeff = ReleaseCoeff,
            };
        }

        object ICloneable.Clone() => Clone();
    }
}
