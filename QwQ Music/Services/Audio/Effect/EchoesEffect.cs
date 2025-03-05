using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NAudio.Dsp;
using NAudio.Wave;
using QwQ_Music.Services.Audio.Effect.Base;

namespace QwQ_Music.Services.Audio.Effect;

/// <summary>
/// 回声效果器
/// </summary>
public sealed class EchoesEffect : AudioEffectBase
{
    private readonly Lock _lock = new(); // 确保线程安全

    /// <summary>
    /// 每条延迟线的缓冲区
    /// </summary>
    private readonly List<float[]> _delayBuffers = [];

    /// <summary>
    /// 每条延迟线的样本数
    /// </summary>
    private int[] _delaySamples = [48000];

    /// <summary>
    /// 每条延迟线的反馈量
    /// </summary>
    private float[] _feedbacks = [0.5f];

    /// <summary>
    /// 反馈滤波器
    /// </summary>
    private BiQuadFilter?[]? _feedbackFilters;

    /// <summary>
    /// 缓冲区长度
    /// </summary>
    private int _bufferLength;

    /// <summary>
    /// 当前缓冲区索引
    /// </summary>
    private int _bufferIndex;

    public override string Name => "Echoes";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        lock (_lock)
        {
            if (Source.WaveFormat.Channels is < 1 or > 2)
                throw new NotSupportedException("仅支持单声道或立体声音频");

            if (_delaySamples.Length != _feedbacks.Length)
                throw new ArgumentException("延迟时间数组和反馈量数组长度必须相同");

            InitializeBuffers();
            InitializeFilters();
        }
    }

    /// <summary>
    /// 将延迟时间从毫秒转换为样本数
    /// </summary>
    private static int[] ConvertToSamples(float[] delayTimes, WaveFormat waveFormat)
    {
        int channels = waveFormat.Channels;
        int sampleRate = waveFormat.SampleRate;
        return delayTimes.Select(time => Math.Max(1, (int)(time * sampleRate / 1000) * channels)).ToArray();
    }

    /// <summary>
    /// 初始化延迟缓冲区
    /// </summary>
    private void InitializeBuffers()
    {
        _bufferLength = _delaySamples.Max();
        _delayBuffers.Clear();
        foreach (int _ in _delaySamples)
        {
            _delayBuffers.Add(new float[_bufferLength]);
        }
        _bufferIndex = 0;
    }

    /// <summary>
    /// 初始化反馈滤波器
    /// </summary>
    private void InitializeFilters()
    {
        _feedbackFilters = _delaySamples
            .Select(_ => BiQuadFilter.LowPassFilter(Source.WaveFormat.SampleRate, 2000, 1.0f))
            .ToArray();
    }

    /// <summary>
    /// 读取音频数据并应用延迟效果
    /// </summary>
    public override int Read(float[] buffer, int offset, int count)
    {
        if (!Enabled)
        {
            return Source.Read(buffer, offset, count);
        }

        int samplesRead = Source.Read(buffer, offset, count);

        for (int n = 0; n < samplesRead; n += Source.WaveFormat.Channels)
        {
            ProcessSample(buffer, offset + n);
        }
        return samplesRead;
    }

    /// <summary>
    /// 处理单个样本
    /// </summary>
    private void ProcessSample(float[] buffer, int index)
    {
        int channels = Source.WaveFormat.Channels;

        // 清空输出信号
        float outputL = 0;
        float outputR = 0;

        // 处理每条延迟线
        for (int i = 0; i < _delayBuffers.Count; i++)
        {
            float[] delayBuffer = _delayBuffers[i];
            var filter = _feedbackFilters?[i];

            // 计算读取位置
            int readPos = (_bufferIndex + delayBuffer.Length - _delaySamples[i]) % delayBuffer.Length;

            // 累加延迟信号（左右声道）
            outputL += delayBuffer[readPos];
            outputR += channels == 2 ? delayBuffer[readPos + 1] : delayBuffer[readPos];

            // 应用反馈滤波器
            float feedbackSampleL = filter?.Transform(delayBuffer[readPos] * _feedbacks[i]) ?? 0;
            float feedbackSampleR =
                channels == 2 ? filter?.Transform(delayBuffer[readPos + 1] * _feedbacks[i]) ?? 0 : feedbackSampleL;

            // 将当前信号写入延迟缓冲区
            delayBuffer[_bufferIndex] = buffer[index] + feedbackSampleL;
            if (channels == 2)
            {
                delayBuffer[_bufferIndex + 1] = buffer[index + 1] + feedbackSampleR;
            }
        }

        // 更新缓冲区索引
        _bufferIndex = (_bufferIndex + channels) % _bufferLength;

        // 输出延迟信号（仅湿信号）
        buffer[index] = outputL;
        if (channels == 2)
        {
            buffer[index + 1] = outputR;
        }
    }

    /// <summary>
    /// 克隆当前效果器的配置
    /// </summary>
    public override IAudioEffect Clone()
    {
        var clone = new EchoesEffect { Enabled = Enabled, Priority = Priority };
        clone.SetParameter("DelayTimesMs", GetParameter<float[]>("DelayTimesMs"));
        clone.SetParameter("Feedbacks", GetParameter<float[]>("Feedbacks"));
        return clone;
    }

    /// <summary>
    /// 设置效果器的配置参数
    /// </summary>
    public override void SetParameter<T>(string key, T value)
    {
        base.SetParameter(key, value);
        switch (key.ToLower())
        {
            case "delaytimesms":
                if (value is float[] delayTimes)
                {
                    lock (_lock)
                    {
                        _delaySamples = ConvertToSamples(delayTimes, Source.WaveFormat);
                        InitializeBuffers();
                    }
                }
                break;
            case "feedbacks":
                if (value is float[] feedbacks)
                {
                    lock (_lock)
                    {
                        _feedbacks = ClampFeedbacks(feedbacks);
                    }
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
            case "delaytimesms":
                return (T)
                    (object)
                        _delaySamples
                            .Select(sample => sample / Source.WaveFormat.SampleRate * 1000 / Source.WaveFormat.Channels)
                            .ToArray();
            case "feedbacks":
                return (T)(object)_feedbacks;
            default:
                return base.GetParameter<T>(key);
        }
    }

    /// <summary>
    /// 限制反馈量在合理范围内
    /// </summary>
    private static float[] ClampFeedbacks(float[] feedbacks)
    {
        return feedbacks.Select(feedback => Math.Clamp(feedback, 0.0f, 1.0f)).ToArray();
    }
}
