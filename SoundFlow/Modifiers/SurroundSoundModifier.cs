using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 全景声音效修改器<br />
/// Surround sound effect modifier
/// </summary>
public sealed class SurroundSoundModifier : SoundModifier
{
    // 延迟缓冲区
    private readonly float[][] _delayBuffers;
    private readonly int[] _delayIndices;

    // 混响参数
    private float _surroundWidth = 0.5f;
    private float _surroundDepth = 0.3f;
    private float _centerLevel = 0.7f;
    private float _surroundLevel = 0.5f;
    private float _crossfeedAmount = 0.2f;

    // 延迟参数
    private readonly int _maxDelaySamples;
    private readonly int[] _delaySamples;

    // 滤波器状态
    private readonly float[] _lpfStates;
    private readonly float[] _hpfStates;
    private readonly float[] _previousSamples;

    /// <inheritdoc />
    public override string Name { get; set; } = "全景声效果";

    /// <summary>
    /// 全景宽度 (0.0-1.0)<br />
    /// Surround width (0.0-1.0)
    /// </summary>
    public float SurroundWidth
    {
        get => _surroundWidth;
        set => _surroundWidth = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// 全景深度 (0.0-1.0)<br />
    /// Surround depth (0.0-1.0)
    /// </summary>
    public float SurroundDepth
    {
        get => _surroundDepth;
        set => _surroundDepth = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// 中央声道电平 (0.0-1.0)<br />
    /// Center channel level (0.0-1.0)
    /// </summary>
    public float CenterLevel
    {
        get => _centerLevel;
        set => _centerLevel = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// 环绕声道电平 (0.0-1.0)<br />
    /// Surround channel level (0.0-1.0)
    /// </summary>
    public float SurroundLevel
    {
        get => _surroundLevel;
        set => _surroundLevel = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// 交叉馈送量 (0.0-1.0)<br />
    /// Crossfeed amount (0.0-1.0)
    /// </summary>
    public float CrossfeedAmount
    {
        get => _crossfeedAmount;
        set => _crossfeedAmount = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// 构造函数<br />
    /// Constructor
    /// </summary>
    public SurroundSoundModifier()
    {
        // 初始化延迟参数
        _maxDelaySamples = (int)(0.05f * AudioEngine.Instance.SampleRate); // 最大50ms延迟
        _delaySamples = new int[2];
        _delaySamples[0] = (int)(0.015f * AudioEngine.Instance.SampleRate); // 左声道15ms延迟
        _delaySamples[1] = (int)(0.020f * AudioEngine.Instance.SampleRate); // 右声道20ms延迟

        // 初始化延迟缓冲区
        _delayBuffers = new float[2][];
        _delayBuffers[0] = new float[_maxDelaySamples];
        _delayBuffers[1] = new float[_maxDelaySamples];
        _delayIndices = new int[2];

        // 初始化滤波器状态
        _lpfStates = new float[2];
        _hpfStates = new float[2];
        _previousSamples = new float[2];
    }

    /// <inheritdoc />
    public override void Process(Span<float> buffer)
    {
        if (AudioEngine.Channels < 2)
            return;

        int channels = AudioEngine.Channels;

        for (int n = 0; n < buffer.Length; n += channels)
        {
            // 确保有足够的样本
            if (n + 1 >= buffer.Length)
                break;

            // 获取左右声道样本
            float left = buffer[n];
            float right = buffer[n + 1];

            // 计算中央声道
            float center = (left + right) * 0.5f * _centerLevel;

            // 应用交叉馈送
            float leftWithCrossfeed = left * (1 - _crossfeedAmount) + right * _crossfeedAmount;
            float rightWithCrossfeed = right * (1 - _crossfeedAmount) + left * _crossfeedAmount;

            // 应用延迟和全景效果
            float surroundLeft = ProcessDelay(leftWithCrossfeed, 0) * _surroundLevel;
            float surroundRight = ProcessDelay(rightWithCrossfeed, 1) * _surroundLevel;

            // 应用低通滤波器到环绕声道
            surroundLeft = ProcessLowPass(surroundLeft, 0);
            surroundRight = ProcessLowPass(surroundRight, 1);

            // 应用高通滤波器到原始信号
            float directLeft = ProcessHighPass(left, 0);
            float directRight = ProcessHighPass(right, 1);

            // 混合直接声道和环绕声道
            float widthFactor = 1.0f + _surroundWidth;
            float depthFactor = _surroundDepth;

            // 最终输出
            buffer[n] = directLeft + center + surroundLeft * widthFactor - surroundRight * depthFactor;
            buffer[n + 1] = directRight + center + surroundRight * widthFactor - surroundLeft * depthFactor;

            // 软限幅以防止削波
            buffer[n] = SoftClip(buffer[n]);
            buffer[n + 1] = SoftClip(buffer[n + 1]);
        }
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel)
    {
        // 单样本处理在Process中已实现，这里仅返回原样本
        return sample;
    }

    /// <summary>
    /// 处理延迟<br />
    /// Process delay
    /// </summary>
    private float ProcessDelay(float input, int channel)
    {
        // 保存当前样本到延迟缓冲区
        _delayBuffers[channel][_delayIndices[channel]] = input;

        // 计算延迟输出索引
        int delayOutputIndex = (_delayIndices[channel] - _delaySamples[channel] + _maxDelaySamples) % _maxDelaySamples;

        // 获取延迟输出
        float delayedSample = _delayBuffers[channel][delayOutputIndex];

        // 更新延迟索引
        _delayIndices[channel] = (_delayIndices[channel] + 1) % _maxDelaySamples;

        return delayedSample;
    }

    /// <summary>
    /// 处理低通滤波<br />
    /// Process low-pass filter
    /// </summary>
    private float ProcessLowPass(float input, int channel)
    {
        // 简单的一阶低通滤波器，截止频率约为3kHz
        float alpha = 0.2f;
        _lpfStates[channel] = alpha * input + (1 - alpha) * _lpfStates[channel];
        return _lpfStates[channel];
    }

    /// <summary>
    /// 处理高通滤波<br />
    /// Process high-pass filter
    /// </summary>
    private float ProcessHighPass(float input, int channel)
    {
        // 简单的一阶高通滤波器，截止频率约为200Hz
        float alpha = 0.95f;
        float output = alpha * (_hpfStates[channel] + input - _previousSamples[channel]);
        _hpfStates[channel] = output;
        _previousSamples[channel] = input;
        return output;
    }

    /// <summary>
    /// 软限幅函数<br />
    /// Soft clipping function
    /// </summary>
    private static float SoftClip(float sample, float threshold = 0.9f)
    {
        if (Math.Abs(sample) <= threshold)
            return sample;

        return Math.Sign(sample)
            * (threshold + (1 - threshold) * MathF.Tanh((Math.Abs(sample) - threshold) / (1 - threshold)));
    }

    /// <summary>
    /// 重置所有内部状态<br />
    /// Reset all internal states
    /// </summary>
    public void Reset()
    {
        // 清空延迟缓冲区
        for (int c = 0; c < 2; c++)
        {
            Array.Clear(_delayBuffers[c], 0, _delayBuffers[c].Length);
            _delayIndices[c] = 0;
            _lpfStates[c] = 0;
            _hpfStates[c] = 0;
            _previousSamples[c] = 0;
        }
    }
}
