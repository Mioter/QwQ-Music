using System;
using NAudio.Wave;

namespace QwQ_Music.Services;

public class MultiChannelGainSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float[] _channelGains;
    private readonly int _channels; // 缓存声道数

    public WaveFormat WaveFormat => _source.WaveFormat;

    public MultiChannelGainSampleProvider(ISampleProvider source, float[] channelGains)
    {
        _source = source;
        _channelGains = channelGains;

        // 验证声道数匹配
        if (source.WaveFormat.Channels != channelGains.Length)
            throw new ArgumentException("声道增益数与音频声道数不匹配");

        _channels = source.WaveFormat.Channels; // 缓存声道数
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        // 计算需要处理的样本数（每个样本包含多个声道）
        int sampleFrames = samplesRead / _channels;

        // 使用指针操作或直接索引优化
        for (int i = 0; i < sampleFrames; i++)
        {
            for (int ch = 0; ch < _channels; ch++)
            {
                int index = offset + i * _channels + ch;
                buffer[index] *= _channelGains[ch];
            }
        }

        return samplesRead;
    }
}