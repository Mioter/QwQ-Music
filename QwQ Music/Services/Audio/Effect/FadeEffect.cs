using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 淡入淡出效果器（支持多种渐变曲线）
/// </summary>
public sealed class FadeEffect : AudioEffectBase
{
    /// <summary>
    /// 渐变曲线类型
    /// </summary>
    public enum FadeCurve
    {
        Linear, // 线性：volume = progress
        Exponential, // 指数：volume = progress^2
        Cosine, // 余弦：volume = (1 - cos(π*progress))/2
    }

    // 配置参数（原子更新）
    private volatile FadeConfig _config = new();

    // 状态管理（使用ReaderWriterLockSlim保证线程安全）
    private readonly ReaderWriterLockSlim _stateLock = new(LockRecursionPolicy.NoRecursion);
    private FadePhase _phase = FadePhase.Idle;
    private int _processedSamples;
    private int _targetSamples;
    private double _startGain;
    private double _endGain;
    private Func<double, double> _curveFunc = null!;

    public override string Name => "Fade";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ConfigureDefaults();
    }

    /// <summary>
    /// 启动淡入效果
    /// </summary>
    public void BeginFadeIn() => BeginFade(1.0, _config.FadeInTimeMs);

    /// <summary>
    /// 启动淡出效果
    /// </summary>
    public void BeginFadeOut() => BeginFade(0.0, _config.FadeOutTimeMs);

    /// <summary>
    /// 核心音频处理方法
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled || _phase == FadePhase.Idle)
            return Source.Read(buffer, offset, count);

        int samplesRead = Source.Read(buffer, offset, count);
        int channels = WaveFormat.Channels;

        _stateLock.EnterReadLock();
        try
        {
            ProcessSamples(buffer, offset, samplesRead, channels);
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
        return samplesRead;
    }

    /// <summary>
    /// 设置效果器参数
    /// </summary>
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        var newConfig = _config.Clone();
        switch (key.ToLower())
        {
            case "fadeintimems":
                newConfig.FadeInTimeMs = ValidateTime(Convert.ToDouble(value));
                break;
            case "fadeouttimems":
                newConfig.FadeOutTimeMs = ValidateTime(Convert.ToDouble(value));
                break;
            case "fadecurve":
                if (value is FadeCurve curve)
                    newConfig.CurveType = curve;
                break;
            case "smoothnessthreshold":
                newConfig.SmoothnessThreshold = ValidateThreshold(Convert.ToDouble(value));
                break;
        }
        Interlocked.Exchange(ref _config, newConfig);
    }

    public override IAudioEffect Clone()
    {
        _stateLock.EnterReadLock();
        try
        {
            return new FadeEffect
            {
                _config = _config.Clone(),
                Enabled = Enabled,
                Priority = Priority,
            };
        }
        finally
        {
            _stateLock.ExitReadLock();
        }
    }

    #region 私有实现

    private enum FadePhase
    {
        Idle,
        Active,
        Completing,
    }

    [Serializable]
    private sealed class FadeConfig : ICloneable
    {
        public double FadeInTimeMs = 1000;
        public double FadeOutTimeMs = 1000;
        public FadeCurve CurveType = FadeCurve.Linear;
        public double SmoothnessThreshold = 1e-6;

        public FadeConfig Clone() => (FadeConfig)MemberwiseClone();

        object ICloneable.Clone() => Clone();
    }

    /// <summary>
    /// 初始化默认参数
    /// </summary>
    private void ConfigureDefaults()
    {
        SetParameter("SmoothnessThreshold", 1e-6);
        SetParameter("FadeInTimeMs", 1000d);
        SetParameter("FadeOutTimeMs", 1000d);
        SetParameter("FadeCurve", FadeCurve.Linear);
    }

    /// <summary>
    /// 启动渐变过程
    /// </summary>
    private void BeginFade(double targetGain, double durationMs)
    {
        if (!Enabled)
            return;

        _stateLock.EnterWriteLock();
        try
        {
            var config = _config;

            // 始终从当前实际增益开始（改进点）
            _startGain = GetCurrentGain();
            _endGain = targetGain;

            // 动态计算目标样本数（支持中途修改持续时间）
            _targetSamples = CalculateSampleCount(durationMs);
            _processedSamples = 0;

            _curveFunc = CreateCurveFunction(config.CurveType);
            _phase = FadePhase.Active;
        }
        finally
        {
            _stateLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 处理样本数据
    /// </summary>
    private void ProcessSamples(float[] buffer, int offset, int count, int channels)
    {
        int remaining = count;
        while (remaining > 0 && _phase == FadePhase.Active)
        {
            int batch = Math.Min(remaining, _targetSamples - _processedSamples);
            ProcessBatch(buffer, offset, batch, channels);

            offset += batch;
            remaining -= batch;

            if (_processedSamples >= _targetSamples)
            {
                FinalizeFade();
            }
        }

        if (remaining > 0)
        {
            ApplyFinalGain(buffer, offset, remaining);
        }
    }

    /// <summary>
    /// 处理批次样本
    /// </summary>
    private void ProcessBatch(float[] buffer, int offset, int count, int channels)
    {
        double progressStep = 1.0 / _targetSamples;

        for (int i = 0; i < count; )
        {
            double progress = _processedSamples * progressStep;
            double gain = CalculateGain(progress);

            for (int ch = 0; ch < channels; ch++)
            {
                buffer[offset + i + ch] *= (float)gain;
            }

            i += channels;
            _processedSamples++;
        }
    }

    /// <summary>
    /// 计算当前增益值
    /// </summary>
    private double CalculateGain(double progress)
    {
        try
        {
            double curveValue = _curveFunc(progress);
            return _startGain + (_endGain - _startGain) * curveValue;
        }
        catch (Exception ex)
        {
            LoggerService.Error($"Gain calculation failed: {ex.Message}");
            return _endGain; // 故障安全处理
        }
    }

    /// <summary>
    /// 创建渐变曲线函数
    /// </summary>
    private static Func<double, double> CreateCurveFunction(FadeCurve curve) =>
        curve switch
        {
            FadeCurve.Linear => p => p,
            FadeCurve.Exponential => p => p * p,
            FadeCurve.Cosine => p => (1 - Math.Cos(Math.PI * p)) / 2,
            _ => throw new ArgumentOutOfRangeException(nameof(curve), $"不支持的渐变曲线类型: {curve}"),
        };

    /// <summary>
    /// 完成渐变过程
    /// </summary>
    private void FinalizeFade()
    {
        _phase = FadePhase.Completing;
        _processedSamples = 0;
        _phase = FadePhase.Idle;
    }

    /// <summary>
    /// 应用最终增益值
    /// </summary>
    private void ApplyFinalGain(float[] buffer, int offset, int count)
    {
        float finalGain = (float)_endGain;
        if (Math.Abs(finalGain - 1f) < _config.SmoothnessThreshold)
            return;

        for (int i = 0; i < count; i++)
        {
            buffer[offset + i] *= finalGain;
        }
    }

    /// <summary>
    /// 获取当前增益值（用于平滑过渡）
    /// </summary>
    private double GetCurrentGain() =>
        _phase switch
        {
            FadePhase.Active => CalculateGain(Math.Clamp((double)_processedSamples / _targetSamples, 0, 1)),
            FadePhase.Completing => _endGain,
            _ => _phase == FadePhase.Idle ? _endGain : throw new InvalidOperationException("淡入淡出阶段无效。"),
        };

    /// <summary>
    /// 计算对应样本数量
    /// </summary>
    private int CalculateSampleCount(double milliseconds) => (int)(milliseconds * WaveFormat.SampleRate / 1000);

    /// <summary>
    /// 验证时间参数有效性
    /// </summary>
    private static double ValidateTime(double value) => Math.Clamp(value, 10, 60000);

    /// <summary>
    /// 验证平滑度阈值
    /// </summary>
    private static double ValidateThreshold(double value) => Math.Clamp(value, 1e-9, 0.1);
    #endregion
}
