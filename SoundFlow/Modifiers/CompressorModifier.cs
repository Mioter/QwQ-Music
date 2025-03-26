using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 动态范围压缩器<br />
/// Dynamic range compressor modifier
/// </summary>
public class CompressorModifier : SoundModifier
{
    private float _attackMs;
    private float _releaseMs;
    private float _alphaA;
    private float _alphaR;

    /// <summary>
    /// 压缩阈值（dBFS），超过此值的信号将被压缩<br />
    /// The threshold level in dBFS (-inf to 0)
    /// </summary>
    public float ThresholdDb { get; set; } = -20f;

    /// <summary>
    /// 压缩比（例如4:1表示超过阈值的信号按4:1压缩）<br />
    /// The compression ratio (1:1 to inf:1)
    /// </summary>
    public float Ratio { get; set; } = 4f;

    /// <summary>
    /// 启动时间（毫秒），控制增益衰减的速度<br />
    /// The attack time in milliseconds
    /// </summary>
    public float AttackMs
    {
        get => _attackMs;
        set
        {
            _attackMs = Math.Max(0.1f, value);
            UpdateAlphaValues();
        }
    }

    /// <summary>
    /// 释放时间（毫秒），控制增益恢复的速度<br />
    /// The release time in milliseconds
    /// </summary>
    public float ReleaseMs
    {
        get => _releaseMs;
        set
        {
            _releaseMs = Math.Max(0.1f, value);
            UpdateAlphaValues();
        }
    }

    /// <summary>
    /// 软拐点宽度（dB），0表示硬拐点<br />
    /// The knee radius in dBFS. 0 means hard knee
    /// </summary>
    public float KneeDb { get; set; } = 0f;

    /// <summary>
    /// 补偿增益（dB），用于补偿压缩后的电平下降<br />
    /// The make-up gain in dBFS
    /// </summary>
    public float MakeupGainDb { get; set; } = 0f;

    private float _envelope;
    private float _gain;

    /// <summary>
    /// 创建动态范围压缩器实例<br />
    /// Constructs a new compressor instance
    /// </summary>
    public CompressorModifier()
    {
        UpdateAlphaValues();
        _gain = 1f;
    }

    private void UpdateAlphaValues()
    {
        float sampleRate = AudioEngine.Instance.SampleRate;
        _alphaA = MathF.Exp(-1f / (_attackMs * 0.001f * sampleRate));
        _alphaR = MathF.Exp(-1f / (_releaseMs * 0.001f * sampleRate));
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        // 转换为绝对值dB值 / Convert to absolute dB value
        float sampleDb = LinearToDb(MathF.Abs(sample));

        // 更新包络检测器 / Update envelope detector
        _envelope = sampleDb > _envelope 
            ? _alphaA * _envelope + (1 - _alphaA) * sampleDb
            : _alphaR * _envelope + (1 - _alphaR) * sampleDb;

        // 计算超阈值量 / Calculate overshoot
        float overshootDb = _envelope - ThresholdDb;
        float reductionDb = 0f;

        // 软拐点处理 / Soft knee processing
        if (overshootDb > 0)
        {
            if (KneeDb > 0)
            {
                // 软拐点区域 / Soft knee region
                float halfKnee = KneeDb / 2;
                if (overshootDb <= halfKnee)
                {
                    reductionDb = (Ratio - 1) * overshootDb * overshootDb / (2 * KneeDb);
                }
                else
                {
                    reductionDb = (Ratio - 1) * (overshootDb - halfKnee) / Ratio;
                }
            }
            else
            {
                // 硬拐点处理 / Hard knee processing
                reductionDb = (Ratio - 1) * overshootDb / Ratio;
            }
        }

        // 计算目标增益 / Calculate target gain
        float targetGain = DbToLinear(-reductionDb + MakeupGainDb);

        // 平滑增益变化 / Smooth gain changes
        float alpha = reductionDb > 0 ? _alphaA : _alphaR;
        _gain = alpha * _gain + (1 - alpha) * targetGain;

        return sample * _gain;
    }

    /// <summary>
    /// dB转线性值<br />
    /// Convert dB to linear value
    /// </summary>
    private static float DbToLinear(float db) => MathF.Pow(10, db / 20f);

    /// <summary>
    /// 线性值转dB<br />
    /// Convert linear value to dB
    /// </summary>
    private static float LinearToDb(float linear) => 
        linear > 0 ? 20f * MathF.Log10(linear) : float.NegativeInfinity;
}