using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 失真效果器<br />
/// Distortion effect modifier
/// </summary>
public sealed class DistortionModifier : SoundModifier
{
    private float _amount = 0.5f;
    private float _tone = 0.5f;
    private float _mix = 1.0f;
    private float _outputGain = 0.5f;

    /// <inheritdoc />
    public override string Name { get; set; } = "Distortion Effect";

    /// <summary>
    /// 失真量 (0.0-1.0)<br />
    /// Distortion amount (0.0-1.0)
    /// </summary>
    public float Amount
    {
        get => _amount;
        set => _amount = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// 音色控制 (0.0-1.0)，较低值增强低频，较高值增强高频<br />
    /// Tone control (0.0-1.0), lower values enhance bass, higher values enhance treble
    /// </summary>
    public float Tone
    {
        get => _tone;
        set => _tone = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// 干湿混合比例 (0.0-1.0)<br />
    /// Dry/wet mix ratio (0.0-1.0)
    /// </summary>
    public float Mix
    {
        get => _mix;
        set => _mix = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// 输出增益 (0.0-1.0)<br />
    /// Output gain (0.0-1.0)
    /// </summary>
    public float OutputGain
    {
        get => _outputGain;
        set => _outputGain = Math.Clamp(value, 0.0f, 1.0f);
    }

    // 低通滤波器状态
    private float[] _lpfState;

    /// <summary>
    /// 构造函数<br />
    /// Constructor
    /// </summary>
    public DistortionModifier()
    {
        _lpfState = new float[AudioEngine.Channels];
    }

    /// <inheritdoc />
    public override void Process(Span<float> buffer)
    {
        int channels = AudioEngine.Channels;

        // 确保滤波器状态数组大小正确
        if (_lpfState.Length != channels)
        {
            _lpfState = new float[channels];
        }

        // 计算失真参数
        float drive = 1.0f + 20.0f * _amount * _amount;
        float gain = MathF.Pow(10.0f, -0.2f * drive); // 补偿增益
        float outGain = 0.5f + _outputGain * 1.5f;

        // 计算低通滤波系数 (基于音色参数)
        float cutoff = 200.0f + _tone * _tone * 15000.0f;
        float rc = 1.0f / (2.0f * MathF.PI * cutoff);
        float dt = 1.0f / AudioEngine.Instance.SampleRate;
        float alpha = dt / (rc + dt);

        for (int i = 0; i < buffer.Length; i++)
        {
            int channel = i % channels;
            float sample = buffer[i];

            // 干信号
            float dry = sample;

            // 应用驱动增益
            sample *= drive;

            // 软削波失真
            sample = Math.Clamp(sample, -1.0f, 1.0f);
            sample = (float)Math.Tanh(sample * 1.5f);

            // 应用低通滤波
            _lpfState[channel] = alpha * sample + (1.0f - alpha) * _lpfState[channel];
            sample = _lpfState[channel];

            // 应用补偿增益
            sample *= gain * outGain;

            // 干湿混合
            buffer[i] = dry * (1.0f - _mix) + sample * _mix;
        }
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        // 确保滤波器状态数组大小正确
        if (channel >= _lpfState.Length)
        {
            Array.Resize(ref _lpfState, channel + 1);
        }

        // 计算失真参数
        float drive = 1.0f + 20.0f * _amount * _amount;
        float gain = MathF.Pow(10.0f, -0.2f * drive); // 补偿增益
        float outGain = 0.5f + _outputGain * 1.5f;

        // 计算低通滤波系数 (基于音色参数)
        float cutoff = 200.0f + _tone * _tone * 15000.0f;
        float rc = 1.0f / (2.0f * MathF.PI * cutoff);
        float dt = 1.0f / AudioEngine.Instance.SampleRate;
        float alpha = dt / (rc + dt);

        // 干信号
        float dry = sample;

        // 应用驱动增益
        sample *= drive;

        // 软削波失真
        sample = Math.Clamp(sample, -1.0f, 1.0f);
        sample = (float)Math.Tanh(sample * 1.5f);

        // 应用低通滤波
        _lpfState[channel] = alpha * sample + (1.0f - alpha) * _lpfState[channel];
        sample = _lpfState[channel];

        // 应用补偿增益
        sample *= gain * outGain;

        // 干湿混合
        return dry * (1.0f - _mix) + sample * _mix;
    }
}
