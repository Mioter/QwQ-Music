using System;
using System.Runtime.CompilerServices;

namespace QwQ_Music.Common.Audio.SoundModifier;

/// <summary>
///     淡入淡出效果器（支持多种渐变曲线）<br />
///     Fade in/out effect with multiple curve types
/// </summary>
public sealed class FadeModifier : SoundFlow.Abstracts.SoundModifier
{
    /// <summary>
    ///     渐变曲线类型
    /// </summary>
    public enum FadeCurve
    {
        Linear,
        Exponential,
        Cosine,
    }

    // 曲线函数：输入 [0,1]，输出归一化增益变化
    private Func<float, float> _curveFunc = p => p; // 默认线性

    // 配置参数
    private double _fadeInTimeMs = 1000;
    private double _fadeOutTimeMs = 1000;

    private FadePhase _phase = FadePhase.Idle;
    private int _samplesProcessed;
    private float _smoothnessThreshold = 1e-6f;
    private float _startGain;
    private float _targetGain;
    private int _totalSamples;

    /// <inheritdoc />
    public override string Name { get; set; } = "Fade";

    public double SampleRate { get; set; } = 48000;

    /// <summary>
    ///     渐入时间（毫秒）
    /// </summary>
    public double FadeInTimeMs
    {
        get => _fadeInTimeMs;
        set => _fadeInTimeMs = Math.Clamp(value, 10, 60000);
    }

    /// <summary>
    ///     渐出时间（毫秒）
    /// </summary>
    public double FadeOutTimeMs
    {
        get => _fadeOutTimeMs;
        set => _fadeOutTimeMs = Math.Clamp(value, 10, 60000);
    }

    /// <summary>
    ///     渐变曲线类型
    /// </summary>
    public FadeCurve CurveType
    {
        get;
        set
        {
            field = value;

            _curveFunc = value switch
            {
                FadeCurve.Linear => static p => p,
                FadeCurve.Exponential => static p => p * p,
                FadeCurve.Cosine => static p => (1f - MathF.Cos(MathF.PI * p)) * 0.5f,
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };
        }
    } = FadeCurve.Linear;

    /// <summary>
    ///     平滑度阈值（避免无意义的小幅调整）
    /// </summary>
    public double SmoothnessThreshold
    {
        get => _smoothnessThreshold;
        set => _smoothnessThreshold = (float)Math.Clamp(value, 1e-9, 0.1);
    }

    /// <summary>
    ///     启动淡入
    /// </summary>
    public void BeginFadeIn()
    {
        BeginFade(1.0f);
    }

    /// <summary>
    ///     启动淡出
    /// </summary>
    public void BeginFadeOut()
    {
        BeginFade(0.0f);
    }

    /// <summary>
    ///     重置状态
    /// </summary>
    public void Reset()
    {
        _phase = FadePhase.Idle;
        _samplesProcessed = 0;
        _startGain = _targetGain = 1.0f;
    }

    /// <inheritdoc />
    public override void Process(Span<float> buffer, int channels)
    {
        if (_phase is FadePhase.Idle) return;

        ref float first = ref buffer[0];
        int length = buffer.Length;

        if (_phase is FadePhase.Completing)
        {
            ApplyFinalGain(ref first, length, _targetGain);
            _phase = FadePhase.Idle;

            return;
        }

        // Active phase
        float start = _startGain;
        float target = _targetGain;
        float delta = target - start;
        int total = _totalSamples;

        for (int i = 0; i < length; i++)
        {
            if (_samplesProcessed >= total)
            {
                _phase = FadePhase.Completing;
                ApplyFinalGain(ref Unsafe.Add(ref first, i), length - i, target);
                _samplesProcessed = 0;

                return;
            }

            float progress = _samplesProcessed / (float)total;
            float gain = start + delta * _curveFunc(progress);
            buffer[i] *= gain;

            // 每处理完一个采样周期（所有声道）才递增样本计数
            if (i % channels == channels - 1)
                _samplesProcessed++;
        }
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        return sample;

        // 不单独处理
    }

    private void BeginFade(float targetGain)
    {
        float currentGain = _phase switch
        {
            FadePhase.Active => _startGain + (_targetGain - _startGain) * (_samplesProcessed / (float)_totalSamples),
            FadePhase.Completing or FadePhase.Idle => _targetGain,
            _ => 1.0f,
        };

        if (MathF.Abs(currentGain - targetGain) <= _smoothnessThreshold) return;

        _startGain = currentGain;
        _targetGain = targetGain;

        double fadeTime = targetGain > currentGain ? _fadeInTimeMs : _fadeOutTimeMs;
        _totalSamples = Math.Max(CalculateSampleCount(fadeTime), 1);

        _samplesProcessed = 0;
        _phase = FadePhase.Active;
    }

    private static void ApplyFinalGain(ref float buffer, int count, float gain)
    {
        if (MathF.Abs(gain - 1.0f) <= 1e-6f) return;

        for (int i = 0; i < count; i++)
        {
            Unsafe.Add(ref buffer, i) *= gain;
        }
    }

    private int CalculateSampleCount(double timeMs)
    {
        return (int)(timeMs * SampleRate / 1000.0);
    }

    private enum FadePhase
    {
        Idle,
        Active,
        Completing,
    }
}
