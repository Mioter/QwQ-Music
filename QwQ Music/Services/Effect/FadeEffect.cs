using NAudio.Wave;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 淡入淡出效果器
/// </summary>
public class FadeEffect : AudioEffectBase
{
    private enum FadeState { None, FadingIn, FadingOut }

    private FadeState _currentState = FadeState.None;
    private int _fadeSamplePosition;
    private int _fadeSampleCount;
    private double _startVolume;
    private double _endVolume;
    /// <summary>
    /// 淡入淡出效果器
    /// </summary>
    public FadeEffect(ISampleProvider source) : base(source)
    {
        ValidateWaveFormat(source.WaveFormat); // 确保音频格式兼容
    }

    public override string Name => "淡入淡出";

    public double Volume { get; private set; } = 1.0;

    /// <summary>
    /// 开始淡入
    /// </summary>
    /// <param name="fadeDurationInMilliseconds">淡入持续时间（毫秒）</param>
    public void BeginFadeIn(double fadeDurationInMilliseconds)
    {
        StartFade(0, 1.0, fadeDurationInMilliseconds);
    }

    /// <summary>
    /// 开始淡出
    /// </summary>
    /// <param name="fadeDurationInMilliseconds">淡出持续时间（毫秒）</param>
    public void BeginFadeOut(double fadeDurationInMilliseconds)
    {
        StartFade(Volume, 0, fadeDurationInMilliseconds);
    }

    /// <summary>
    /// 启动淡入或淡出
    /// </summary>
    private void StartFade(double startVolume, double endVolume, double milliseconds)
    {
        _startVolume = startVolume;
        _endVolume = endVolume;
        _fadeSamplePosition = 0;
        _fadeSampleCount = (int)(milliseconds * WaveFormat.SampleRate / 1000);
        _currentState = _endVolume > _startVolume ? FadeState.FadingIn : FadeState.FadingOut;
    }

    /// <summary>
    /// 读取音频数据并应用淡入淡出效果
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = Source.Read(buffer, offset, count);

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
            double factor = (double)_fadeSamplePosition / _fadeSampleCount;
            Volume = _startVolume + (_endVolume - _startVolume) * factor;

            // 应用音量到所有声道
            for (int c = 0; c < channels; c++)
            {
                int index = offset + (n * channels) + c;
                buffer[index] = (float)(buffer[index] * Volume);
            }

            _fadeSamplePosition++;
        }

        return samplesRead;
    }
}
