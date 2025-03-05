using System;
using System.Linq;
using System.Text;
using System.Threading;
using NAudio.Dsp;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

public class ReverbEffect : AudioEffectBase
{
    // 内部类：预延迟缓冲区
    private class PreDelayBuffer
    {
        private readonly float[] _buffer;
        private int _bufferIndex;

        public PreDelayBuffer(int delayMs, int sampleRate)
        {
            int bufferSize = (int)(delayMs * sampleRate / 1000.0f);
            _buffer = new float[bufferSize];
            _bufferIndex = 0;
        }

        public float Process(float input)
        {
            // 读取延迟样本
            float delayed = _buffer[_bufferIndex];

            // 写入新值到延迟缓冲区
            _buffer[_bufferIndex] = input;

            // 移动缓冲区索引
            _bufferIndex = (_bufferIndex + 1) % _buffer.Length;

            return delayed;
        }
    }

    // 内部类：梳状滤波器
    private class CombFilter
    {
        private float[] _delayBuffer;
        private int _bufferIndex;
        private readonly float _feedback;
        private BiQuadFilter _lowPassFilter;

        // 保存滤波器初始化参数
        private readonly int _sampleRate;
        private readonly float _cutoffFrequency;
        private readonly float _bandwidth;

        public CombFilter(int maxDelayMs, int sampleRate, float feedback, float dampening)
        {
            int delaySamples = (int)(maxDelayMs * sampleRate / 1000.0f);
            _delayBuffer = new float[delaySamples];
            _bufferIndex = 0;
            _feedback = feedback;

            // 保存参数到字段
            _sampleRate = sampleRate;
            _cutoffFrequency = 5000 * dampening; // 计算实际使用的截止频率
            _bandwidth = 1.0f;

            // 初始化低通滤波器
            _lowPassFilter = BiQuadFilter.LowPassFilter(sampleRate, _cutoffFrequency, _bandwidth);
        }

        public float Process(float input)
        {
            // 读取延迟样本并应用低通滤波
            float output = _lowPassFilter.Transform(_delayBuffer[_bufferIndex]);

            // 计算新输入值（当前输入 + 反馈）
            float newInput = input + output * _feedback;

            // 写入新值到延迟缓冲区
            _delayBuffer[_bufferIndex] = newInput;

            // 移动缓冲区索引
            _bufferIndex = (_bufferIndex + 1) % _delayBuffer.Length;

            return output;
        }

        public CombFilter Clone()
        {
            var clone = (CombFilter)MemberwiseClone();
            clone._delayBuffer = (float[])_delayBuffer.Clone();

            // 使用保存的参数重新创建低通滤波器
            clone._lowPassFilter = BiQuadFilter.LowPassFilter(
                _sampleRate, // 使用保存的 sampleRate
                _cutoffFrequency, // 使用保存的 cutoffFrequency
                _bandwidth
            ); // 使用保存的 bandwidth

            return clone;
        }
    }

    // 内部类：全通滤波器
    private class AllPassFilter
    {
        private float[] _delayBuffer;
        private int _bufferIndex;
        private readonly float _gain;

        public AllPassFilter(float delayMs, int sampleRate, float gain)
        {
            int delaySamples = (int)(delayMs * sampleRate / 1000.0f);
            _delayBuffer = new float[delaySamples];
            _gain = gain;
        }

        public float Process(float input)
        {
            // 读取延迟样本
            float delayed = _delayBuffer[_bufferIndex];

            // 计算输出
            float output = -_gain * input + delayed;

            // 写入新值到延迟缓冲区
            _delayBuffer[_bufferIndex] = input + _gain * delayed;

            // 移动缓冲区索引
            _bufferIndex = (_bufferIndex + 1) % _delayBuffer.Length;

            return output;
        }

        public AllPassFilter Clone()
        {
            var clone = (AllPassFilter)MemberwiseClone();
            clone._delayBuffer = (float[])_delayBuffer.Clone();
            return clone;
        }
    }

    // 字段定义
    private readonly Lock _lock = new();
    private CombFilter[] _combFilters = [];
    private AllPassFilter[] _allPassFilters = [];
    private PreDelayBuffer _preDelayBuffer = null!;

    private float _roomSize = 1.0f; // 房间大小 (0.5-2.0)
    private float _decayTime = 1.0f; // 衰减时间 (0.1-10.0)
    private float _dampening = 0.5f; // 高频衰减 (0.0-1.0)
    private float _dryMix = 0.5f; // 干信号比例
    private float _wetMix = 0.5f; // 湿信号比例
    private float _preDelay = 50.0f; // 预延迟（毫秒）

    public override string Name => "Reverb";

    // 初始化方法
    protected override void OnInitialized()
    {
        base.OnInitialized();
        InitializeFilters();

        // 初始化预延迟缓冲区
        int sampleRate = Source.WaveFormat.SampleRate;
        _preDelayBuffer = new PreDelayBuffer((int)_preDelay, sampleRate);
    }

    private void InitializeFilters()
    {
        lock (_lock)
        {
            int sampleRate = Source.WaveFormat.SampleRate;

            // 根据房间大小调整基础延迟时间
            float baseDelay = _roomSize * 30.0f;

            // 初始化梳状滤波器
            _combFilters =
            [
                new CombFilter((int)(baseDelay * 0.8f), sampleRate, GetDecayFeedback(), _dampening),
                new CombFilter((int)(baseDelay * 1.2f), sampleRate, GetDecayFeedback(), _dampening),
                new CombFilter((int)(baseDelay * 1.5f), sampleRate, GetDecayFeedback(), _dampening),
                new CombFilter((int)(baseDelay * 1.8f), sampleRate, GetDecayFeedback(), _dampening),
            ];

            // 初始化全通滤波器
            _allPassFilters = [new AllPassFilter(5, sampleRate, 0.7f), new AllPassFilter(1.7f, sampleRate, 0.7f)];
        }
    }

    private float GetDecayFeedback()
    {
        float maxFeedback = 0.95f - (_decayTime - 2.0f) * 0.05f; // 动态调整最大反馈增益
        maxFeedback = Math.Clamp(maxFeedback, 0.7f, 0.95f);

        float feedback = (float)Math.Pow(10.0, -3.0 * _preDelay / (_decayTime * 1000.0));
        return Math.Min(Math.Max(feedback, 0.01f), maxFeedback);
    }

    // 处理音频样本
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
            return Source.Read(buffer, offset, count);

        int samplesRead = Source.Read(buffer, offset, count);

        float dry = _dryMix;
        float wet = _wetMix;

        // 获取当前滤波器快照
        CombFilter[] currentCombs;
        AllPassFilter[] currentAllPass;
        lock (_lock)
        {
            currentCombs = _combFilters.ToArray();
            currentAllPass = _allPassFilters.ToArray();
        }

        for (int n = 0; n < samplesRead; n += Source.WaveFormat.Channels)
        {
            ProcessSample(currentCombs, currentAllPass, buffer, offset + n, dry, wet);
        }

        return samplesRead;
    }

    private void ProcessSample(
        CombFilter[] combs,
        AllPassFilter[] allPasses,
        float[] buffer,
        int index,
        float dryMix,
        float wetMix
    )
    {
        int channels = Source.WaveFormat.Channels;

        // 读取原始信号
        float inputL = buffer[index];
        float inputR = channels == 2 ? buffer[index + 1] : inputL;

        // 创建单声道混响输入
        float monoInput = (inputL + inputR) * 0.5f;

        // 应用预延迟
        if (_preDelay > 0)
        {
            monoInput = _preDelayBuffer.Process(monoInput);
        }

        // 处理梳状滤波器
        float combSum = combs.Sum(comb => comb.Process(monoInput)) * 0.25f; // 平均四个梳状滤波器的输出
        combSum *= 0.8f; // 添加额外的衰减因子

        // 处理全通滤波器
        float allPassOut = allPasses.Aggregate(combSum, (current, allPass) => allPass.Process(current));

        // 混合干湿信号
        buffer[index] = inputL * dryMix + allPassOut * wetMix;
        if (channels == 2)
        {
            buffer[index + 1] = inputR * dryMix + allPassOut * wetMix;
        }
    }

    // 克隆方法
    public override IAudioEffect Clone()
    {
        var clone = new ReverbEffect
        {
            _roomSize = _roomSize,
            _decayTime = _decayTime,
            _dampening = _dampening,
            _dryMix = _dryMix,
            _wetMix = _wetMix,
            _preDelay = _preDelay,
            Enabled = Enabled,
            Priority = Priority,
        };

        lock (_lock)
        {
            clone._combFilters = _combFilters.Select(c => c.Clone()).ToArray();
            clone._allPassFilters = _allPassFilters.Select(a => a.Clone()).ToArray();
        }

        return clone;
    }

    // 调试信息
    public override string DebugInfo =>
        new StringBuilder()
            .AppendLine($"Name: {Name}")
            .AppendLine($"Enabled: {Enabled}")
            .AppendLine($"Room Size: {_roomSize:F2}")
            .AppendLine($"Decay Time: {_decayTime:F2}s")
            .AppendLine($"Dampening: {_dampening:F2}")
            .AppendLine($"Dry/Wet: {_dryMix:F2}/{_wetMix:F2}")
            .AppendLine($"PreDelay: {_preDelay:F2}ms")
            .ToString();

    // 设置参数
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);

        lock (_lock)
        {
            switch (key.ToLower())
            {
                case "roomsize":
                    if (value is float rs)
                    {
                        _roomSize = Math.Clamp(rs, 0.5f, 2.0f);
                        InitializeFilters();
                    }
                    break;

                case "decaytime":
                    if (value is float dt)
                    {
                        _decayTime = Math.Clamp(dt, 0.1f, 10.0f);
                        InitializeFilters();
                    }
                    break;

                case "dampening":
                    if (value is float dmp)
                    {
                        _dampening = Math.Clamp(dmp, 0.0f, 1.0f);
                        InitializeFilters();
                    }
                    break;

                case "drymix":
                    if (value is float dm)
                        _dryMix = Math.Clamp(dm, 0.0f, 1.0f);
                    break;

                case "wetmix":
                    if (value is float wm)
                        _wetMix = Math.Clamp(wm, 0.0f, 1.0f);
                    break;

                case "predelay":
                    if (value is float pd)
                        _preDelay = Math.Clamp(pd, 50.0f, 200.0f);
                    break;
            }
        }
    }

    // 获取参数
    public override T GetParameter<T>(string key)
    {
        return key.ToLower() switch
        {
            "roomsize" => (T)(object)_roomSize,
            "decaytime" => (T)(object)_decayTime,
            "dampening" => (T)(object)_dampening,
            "drymix" => (T)(object)_dryMix,
            "wetmix" => (T)(object)_wetMix,
            "predelay" => (T)(object)_preDelay,
            _ => base.GetParameter<T>(key),
        };
    }
}
