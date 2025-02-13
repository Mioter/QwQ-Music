using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 混响效果器
/// </summary>
public class ReverbEffect : AudioEffectBase
{
    private readonly List<float[]> _delayBuffers = new();
    private readonly int[] _delaySamples; // 延迟样本数
    private readonly float[] _decayFactors;
    private int _bufferLength;   // 缓冲区长度
    private int _bufferIndex;             // 当前缓冲区索引

    public float RoomSize { get; set; } = 1.0f;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="source">音频源</param>
    /// <param name="delayTimes">延迟时间数组（毫秒）</param>
    /// <param name="decayFactors">衰减因子数组</param>
    public ReverbEffect(ISampleProvider source, int[]? delayTimes = null, float[]? decayFactors = null)
        : base(source)
    {
        ValidateWaveFormat(source.WaveFormat); // 确保音频格式兼容
        ArgumentNullException.ThrowIfNull(source);
        if (source.WaveFormat.Channels != 2)
            throw new NotSupportedException("只支持立体声音频");

        // 使用默认值或用户提供的参数初始化延迟时间和衰减因子
        _delaySamples = ConvertToSamples(delayTimes ?? [50, 65, 80, 95], source.WaveFormat);
        _decayFactors = decayFactors ?? [0.7f, 0.6f, 0.5f, 0.4f];

        if (_delaySamples.Length != _decayFactors.Length)
            throw new ArgumentException("延迟时间数组和衰减因子数组长度必须相同");

        InitializeBuffers();
    }

    /// <summary>
    /// 将延迟时间从毫秒转换为样本数
    /// </summary>
    /// <param name="delayTimes">延迟时间数组（毫秒）</param>
    /// <param name="waveFormat">音频格式</param>
    /// <returns>延迟样本数数组</returns>
    private static int[] ConvertToSamples(int[] delayTimes, WaveFormat waveFormat)
    {
        int channels = waveFormat.Channels;
        int sampleRate = waveFormat.SampleRate;
        return delayTimes.Select(time => time * sampleRate / 1000 * channels).ToArray();
    }

    /// <summary>
    /// 初始化延迟缓冲区
    /// </summary>
    private void InitializeBuffers()
    {
        _bufferLength = _delaySamples.Max(); // 最大延迟样本数
        foreach (var _ in _delaySamples)
        {
            _delayBuffers.Add(new float[_bufferLength]);
        }
    }

    public override string Name => "混响";

    /// <summary>
    /// 读取音频数据并应用混响效果
    /// </summary>
    /// <param name="buffer">音频缓冲区</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="count">要读取的样本数</param>
    /// <returns>实际读取的样本数</returns>
    public override int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = Source.Read(buffer, offset, count);

        int channels = Source.WaveFormat.Channels;
        float wet = 0.5f * RoomSize;
        float dry = 1.0f - wet;

        for (int n = 0; n < samplesRead; n += channels)
        {
            ProcessSample(buffer, offset + n, wet, dry);
        }

        return samplesRead;
    }

    /// <summary>
    /// 处理单个样本
    /// </summary>
    /// <param name="buffer">音频缓冲区</param>
    /// <param name="index">样本索引</param>
    /// <param name="wet">湿信号比例</param>
    /// <param name="dry">干信号比例</param>
    private void ProcessSample(float[] buffer, int index, float wet, float dry)
    {
        // 原始信号混合
        float inputL = buffer[index] * dry;
        float inputR = buffer[index + 1] * dry;

        buffer[index] = inputL;
        buffer[index + 1] = inputR;

        // 处理每个延迟线
        for (int i = 0; i < _delayBuffers.Count; i++)
        {
            float[] delayBuffer = _delayBuffers[i];
            int readPos = (_bufferIndex + delayBuffer.Length - _delaySamples[i]) % delayBuffer.Length;

            // 累加延迟信号
            buffer[index] += delayBuffer[readPos] * _decayFactors[i] * wet;
            buffer[index + 1] += delayBuffer[readPos + 1] * _decayFactors[i] * wet;

            // 写入当前信号到延迟线
            delayBuffer[_bufferIndex] = buffer[index];
            delayBuffer[_bufferIndex + 1] = buffer[index + 1];
        }

        // 更新缓冲区索引
        _bufferIndex = (_bufferIndex + Source.WaveFormat.Channels) % _bufferLength;
    }
}