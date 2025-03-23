using System;
using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 淡入淡出效果器（支持多种渐变曲线）<br />
/// Fade in/out effect with multiple curve types
/// </summary>
public sealed class FadeModifier : SoundModifier
{
    /// <summary>
    /// 渐变曲线类型<br />
    /// Fade curve types
    /// </summary>
    public enum FadeCurve
    {
        /// <summary>线性渐变</summary>
        Linear,
        /// <summary>指数渐变</summary>
        Exponential,
        /// <summary>余弦渐变</summary>
        Cosine
    }

    // 状态参数
    private FadePhase _phase = FadePhase.Idle;
    private int _processedSamples;
    private int _targetSamples;
    private double _startGain;
    private double _endGain;
    private Func<double, double> _curveFunc = null!;
    
    // 配置参数
    private double _fadeInTimeMs = 1000;
    private double _fadeOutTimeMs = 1000;
    private FadeCurve _curveType = FadeCurve.Linear;
    private double _smoothnessThreshold = 1e-6;

    /// <summary>
    /// 渐入时间（毫秒）<br />
    /// Fade in duration in milliseconds
    /// </summary>
    public double FadeInTimeMs
    {
        get => _fadeInTimeMs;
        set => _fadeInTimeMs = Math.Clamp(value, 10, 60000);
    }

    /// <summary>
    /// 渐出时间（毫秒）<br />
    /// Fade out duration in milliseconds
    /// </summary>
    public double FadeOutTimeMs
    {
        get => _fadeOutTimeMs;
        set => _fadeOutTimeMs = Math.Clamp(value, 10, 60000);
    }

    /// <summary>
    /// 渐变曲线类型<br />
    /// Fade curve type
    /// </summary>
    public FadeCurve CurveType
    {
        get => _curveType;
        set
        {
            _curveType = value;
            UpdateCurveFunction();
        }
    }

    /// <summary>
    /// 平滑度阈值<br />
    /// Smoothness threshold for final gain
    /// </summary>
    public double SmoothnessThreshold
    {
        get => _smoothnessThreshold;
        set => _smoothnessThreshold = Math.Clamp(value, 1e-9, 0.1);
    }

    /// <summary>
    /// 启动淡入效果<br />
    /// Begin fade in effect
    /// </summary>
    public void BeginFadeIn() => BeginFade(targetGain: 1.0);

    /// <summary>
    /// 启动淡出效果<br />
    /// Begin fade out effect
    /// </summary>
    public void BeginFadeOut() => BeginFade(targetGain: 0.0);

    /// <inheritdoc/>
    public override void Process(Span<float> buffer)
    {
        // 如果不在活动状态，直接返回，保持原始音频不变
        if (_phase != FadePhase.Active && _phase != FadePhase.Completing) return;

        for (int i = 0; i < buffer.Length; i++)
        {
            int channel = i % AudioEngine.Channels;

            if (_phase == FadePhase.Active)
            {
                if (_processedSamples >= _targetSamples)
                {
                    FinalizeFade();
                }
                else
                {
                    double progress = (double)_processedSamples / _targetSamples;
                    double gain = CalculateGain(progress);
                    buffer[i] *= (float)gain;
                    
                    if (channel == AudioEngine.Channels - 1)
                        _processedSamples++;
                }
            }
        }

        // 应用最终增益
        if (_phase == FadePhase.Completing)
        {
            ApplyFinalGain(buffer);
            _phase = FadePhase.Idle;
        }
    }

    /// <inheritdoc/>
    public override float ProcessSample(float sample, int channel)
    {
        // 不需要单独实现，统一在Process中处理
        return sample;
    }

    private void BeginFade(double targetGain)
    {
        // 如果当前已经处于目标增益状态，则不需要执行渐变
        _startGain = GetCurrentGain();
        
        // 如果起始增益和目标增益相同或非常接近，则不执行渐变
        if (Math.Abs(_startGain - targetGain) < _smoothnessThreshold)
        {
            _endGain = targetGain;
            return;
        }
        
        _endGain = targetGain;
        
        // 根据目标增益选择使用渐入或渐出时间
        double fadeTimeMs = targetGain > _startGain ? _fadeInTimeMs : _fadeOutTimeMs;
        _targetSamples = CalculateSampleCount(fadeTimeMs);
        
        // 确保至少有一些样本需要处理
        if (_targetSamples <= 0)
            _targetSamples = 1;
            
        _processedSamples = 0;
        UpdateCurveFunction();
        _phase = FadePhase.Active;
    }

    private double GetCurrentGain()
    {
        return _phase switch
        {
            FadePhase.Active when _processedSamples > 0 => 
                CalculateGain((double)_processedSamples / _targetSamples),
            FadePhase.Completing => _endGain,
            FadePhase.Idle => _endGain,
            _ => _startGain
        };
    }

    // 添加一个重置方法，用于外部重置状态
    /// <summary>
    /// 重置淡入淡出状态<br />
    /// Reset fade state
    /// </summary>
    public void Reset()
    {
        _phase = FadePhase.Idle;
        _processedSamples = 0;
        _startGain = 1.0;
        _endGain = 1.0;
    }

    private void UpdateCurveFunction()
    {
        _curveFunc = _curveType switch
        {
            FadeCurve.Linear => p => p,
            FadeCurve.Exponential => p => p * p,
            FadeCurve.Cosine => p => (1 - Math.Cos(Math.PI * p)) / 2,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private double CalculateGain(double progress)
    {
        double curveValue = _curveFunc(progress);
        return _startGain + (_endGain - _startGain) * curveValue;
    }

    private void FinalizeFade()
    {
        _phase = FadePhase.Completing;
        _processedSamples = 0;
    }

    private void ApplyFinalGain(Span<float> buffer)
    {
        float finalGain = (float)_endGain;
        if (Math.Abs(finalGain - 1f) < _smoothnessThreshold) return;

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] *= finalGain;
        }
    }

    private static int CalculateSampleCount(double fadeTimeMs)
    {
        return (int)(fadeTimeMs * AudioEngine.Instance.SampleRate / 1000);
    }

    private enum FadePhase
    {
        Idle,
        Active,
        Completing
    }
}