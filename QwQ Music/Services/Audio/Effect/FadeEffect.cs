using System;
using System.Threading;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 淡入淡出效果器
/// </summary>
public class FadeEffect : AudioEffectBase
{
    private enum FadeState
    {
        None,
        FadingIn,
        FadingOut,
    }

    private FadeState _currentState = FadeState.None;
    private int _fadeSamplePosition;
    private int _fadeSampleCount;
    private double _startVolume;
    private double _endVolume;
    private double _fadeInMilliseconds;
    private double _fadeOutMilliseconds;
    private readonly Lock _lock = new();

    /// <summary>
    /// 渐变模式
    /// </summary>
    public enum FadeCurveMode
    {
        Linear, // 线性渐变
        Exponential, // 指数渐变
        Sine, // 正弦渐变
    }

    private FadeCurveMode _fadeCurveMode = FadeCurveMode.Linear; // 默认为线性渐变

    public override string Name => "Fade";
    public double Volume { get; private set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetParameter("FadeInMilliseconds", 1000);
        SetParameter("FadeOutMilliseconds", 1000);
    }

    /// <summary>
    /// 开始淡入
    /// </summary>
    public void BeginFadeIn()
    {
        if (!Enabled)
            return;
        StartFade(Volume, 1, GetParameter<double>("FadeInMilliseconds"));
    }

    /// <summary>
    /// 开始淡出
    /// </summary>
    public void BeginFadeOut()
    {
        if (!Enabled)
            return;
        StartFade(Volume, 0, GetParameter<double>("FadeOutMilliseconds"));
    }

    /// <summary>
    /// 启动淡入或淡出
    /// </summary>
    private void StartFade(double startVolume, double endVolume, double milliseconds)
    {
        lock (_lock)
        {
            _startVolume = startVolume;
            _endVolume = endVolume;
            _fadeSamplePosition = 0;
            _fadeSampleCount = (int)(milliseconds * WaveFormat.SampleRate / 1000);
            _currentState = _endVolume > _startVolume ? FadeState.FadingIn : FadeState.FadingOut;
            Volume = startVolume; // 立即更新音量
        }
    }

    /// <summary>
    /// 读取音频数据并应用淡入淡出效果
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
            if (_currentState == FadeState.None)
                return samplesRead;

            int channels = WaveFormat.Channels;
            int sampleCount = samplesRead / channels;

            for (int n = 0; n < sampleCount; n++)
            {
                if (_fadeSamplePosition >= _fadeSampleCount)
                {
                    Volume = _endVolume;
                    _currentState = FadeState.None;
                    break;
                }

                // 计算当前音量因子
                double factor = CalculateFadeFactor(_fadeSamplePosition, _fadeSampleCount, _fadeCurveMode);
                Volume = _startVolume + (_endVolume - _startVolume) * factor;

                // 应用音量到所有声道
                for (int c = 0; c < channels; c++)
                {
                    int index = offset + n * channels + c;
                    buffer[index] = (float)(buffer[index] * Volume);
                }

                _fadeSamplePosition++;
            }
        }

        return samplesRead;
    }

    /// <summary>
    /// 计算渐变因子
    /// </summary>
    private static double CalculateFadeFactor(int position, int totalSamples, FadeCurveMode mode)
    {
        double normalizedPosition = (double)position / totalSamples;

        switch (mode)
        {
            case FadeCurveMode.Linear:
                return normalizedPosition; // 线性渐变

            case FadeCurveMode.Exponential:
                return Math.Pow(normalizedPosition, 2); // 指数渐变（平方）

            case FadeCurveMode.Sine:
                return (1 - Math.Cos(normalizedPosition * Math.PI)) / 2; // 正弦渐变

            default:
                throw new ArgumentException("Unsupported fade curve mode.");
        }
    }

    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new FadeEffect
        {
            _startVolume = _startVolume,
            _endVolume = _endVolume,
            _fadeSampleCount = _fadeSampleCount,
            _fadeCurveMode = _fadeCurveMode,
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
                return $"Name: {Name}\n"
                    + $"Enabled: {Enabled}\n"
                    + $"Priority: {Priority}\n"
                    + $"Current State: {_currentState}\n"
                    + $"Volume: {Volume:F2}\n"
                    + $"Fade Progress: {_fadeSamplePosition}/{_fadeSampleCount}\n"
                    + $"Fade Curve Mode: {_fadeCurveMode}";
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
            case "fadeinmilliseconds":
                _fadeInMilliseconds = Convert.ToDouble(value);
                break;
            case "fadeoutmilliseconds":
                _fadeOutMilliseconds = Convert.ToDouble(value);
                break;
            case "fadeduration":
                double fadeDurationMs = Convert.ToDouble(value);
                _fadeSampleCount = (int)(fadeDurationMs * WaveFormat.SampleRate / 1000);
                break;
            case "fadecurvemode":
                if (value is FadeCurveMode mode)
                {
                    _fadeCurveMode = mode;
                }
                break;
        }
    }

    /// <summary>
    /// 获取效果器的配置参数
    /// </summary>
    public override T GetParameter<T>(string key)
    {
        switch (key.ToLower())
        {
            case "fadeinmilliseconds":
                return (T)(object)_fadeInMilliseconds;
            case "fadeoutmilliseconds":
                return (T)(object)_fadeOutMilliseconds;
            case "fadeduration":
                return (T)(object)(_fadeSampleCount * 1000.0 / WaveFormat.SampleRate);
            case "fadecurvemode":
                return (T)(object)_fadeCurveMode;
            default:
                return base.GetParameter<T>(key);
        }
    }
}
