using SoundFlow.Abstracts;

namespace SoundFlow.Modifiers;

/// <summary>
/// 多通道合唱效果器<br />
/// A sound modifier that implements a multichannel chorus effect.
/// </summary>
public class MultiChannelChorusModifier : SoundModifier
{
    private class ChannelState
    {
        public readonly float[] DelayLine;

        // 延迟线缓冲区 / Delay line buffer
        public float LfoPhase; // LFO相位 / LFO phase
        public int DelayIndex; // 当前延迟索引 / Current delay index
        public float Depth; // 调制深度 / Modulation depth
        public float Rate; // LFO速率 / LFO rate (Hz)
        public float Feedback; // 反馈系数 / Feedback amount

        public ChannelState(int maxDelay)
        {
            DelayLine = new float[maxDelay];
            Reset();
        }

        // 重置状态 / Reset state
        public void Reset()
        {
            Array.Clear(DelayLine, 0, DelayLine.Length);
            LfoPhase = 0;
            DelayIndex = 0;
        }
    }

    private float _wetMix = 0.5f;
    private int _maxDelay = 1024;
    private readonly List<ChannelState> _channels = [];
    private ChannelParams[] _channelParameters = [];

    /// <summary>
    /// 湿信号混合比例 (0.0-1.0)<br />
    /// Wet/dry mix ratio (0.0-1.0)
    /// </summary>
    public float WetMix
    {
        get => _wetMix;
        set => _wetMix = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// 最大延迟样本数<br />
    /// Maximum delay length in samples
    /// </summary>
    public int MaxDelay
    {
        get => _maxDelay;
        set
        {
            if (_maxDelay == value)
                return;
            _maxDelay = Math.Max(64, value);
            RebuildDelayLines();
        }
    }

    /// <summary>
    /// 通道参数配置<br />
    /// Channel configuration parameters
    /// </summary>
    public ChannelParams[] ChannelParameters
    {
        get => _channelParameters;
        set
        {
            if (value.Length != AudioEngine.Channels)
            {
                throw new ArgumentException(
                    $"需要 {AudioEngine.Channels} 个通道参数 / "
                        + $"Expected {AudioEngine.Channels} channel parameters, got {value.Length}"
                );
            }
            _channelParameters = value;
            ApplyChannelParameters();
        }
    }

    /// <summary>
    /// 创建多通道合唱效果器<br />
    /// Constructs a new multichannel chorus effect
    /// </summary>
    public MultiChannelChorusModifier()
    {
        // 初始化默认参数 / Initialize default parameters
        var defaultParams = new ChannelParams[AudioEngine.Channels];
        Array.Fill(defaultParams, new ChannelParams(2f, 0.5f, 0.7f));
        ChannelParameters = defaultParams;
    }

    private void RebuildDelayLines()
    {
        // 重建所有通道状态 / Rebuild all channel states
        _channels.Clear();
        for (int i = 0; i < AudioEngine.Channels; i++)
        {
            var state = new ChannelState(_maxDelay);
            _channels.Add(state);
        }
        ApplyChannelParameters();
    }

    private void ApplyChannelParameters()
    {
        // 应用通道参数 / Apply channel parameters
        for (int i = 0; i < _channels.Count; i++)
        {
            var param = _channelParameters[i];
            _channels[i].Depth = param.Depth;
            _channels[i].Rate = param.Rate;
            _channels[i].Feedback = param.Feedback;
        }
    }

    /// <inheritdoc />
    public override void Process(Span<float> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            int channel = i % AudioEngine.Channels;
            var state = _channels[channel];

            // 计算调制延迟 / Calculate modulated delay
            float lfo = MathF.Sin(state.LfoPhase) * state.Depth;
            int delayTime = (int)(_maxDelay / 2f + lfo);

            // 获取延迟样本 / Get delayed sample
            int readIndex = (state.DelayIndex - delayTime + _maxDelay) % _maxDelay;
            float delayed = state.DelayLine[readIndex];

            // 更新延迟线 / Update delay line
            state.DelayLine[state.DelayIndex] = buffer[i] + delayed * state.Feedback;

            // 更新LFO相位 / Update LFO phase
            state.LfoPhase += 2 * MathF.PI * state.Rate / AudioEngine.Instance.SampleRate;
            if (state.LfoPhase > 2 * MathF.PI)
                state.LfoPhase -= 2 * MathF.PI;

            // 推进延迟索引 / Advance delay index
            state.DelayIndex = (state.DelayIndex + 1) % _maxDelay;

            // 混合湿/干信号 / Mix wet/dry
            buffer[i] = buffer[i] * (1 - _wetMix) + delayed * _wetMix;
        }
    }

    /// <inheritdoc />
    public override float ProcessSample(float sample, int channel) => throw new NotImplementedException();

    /// <summary>
    /// 通道参数结构<br />
    /// Channel parameters structure
    /// </summary>
    public struct ChannelParams(float depth, float rate, float feedback)
    {
        /// <summary>调制深度 / Modulation depth</summary>
        public float Depth { get; set; } = Math.Clamp(depth, 0.1f, 10f);

        /// <summary>LFO速率 (Hz) / LFO rate (Hz)</summary>
        public float Rate { get; set; } = Math.Clamp(rate, 0.1f, 5f);

        /// <summary>反馈系数 / Feedback amount</summary>
        public float Feedback { get; set; } = Math.Clamp(feedback, 0f, 0.95f);
    }
}
