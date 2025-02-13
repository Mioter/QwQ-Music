using System;
using System.Linq;
using NAudio.Wave;

namespace QwQ_Music.Services.Effect;

/// <summary>
/// 多声道回放增益效果器
/// </summary>
public class MultiChannelReplayGainEffect : AudioEffectBase
{
    private readonly float[] _channelGains; // 各声道增益值
    private readonly int _channels;         // 声道数

    /// <summary>
    /// 构造函数（支持多声道增益数组）
    /// </summary>
    /// <param name="source">音频源</param>
    /// <param name="channelGains">各声道增益数组</param>
    public MultiChannelReplayGainEffect(ISampleProvider source, float[] channelGains)
        : base(source)
    {
        ValidateWaveFormat(source.WaveFormat); // 确保音频格式兼容
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(channelGains);

        _channels = source.WaveFormat.Channels;
        _channelGains = AdaptGains(channelGains, _channels);
    }

    /// <summary>
    /// 自动适配增益数组到目标声道数
    /// </summary>
    /// <param name="inputGains">输入增益数组</param>
    /// <param name="targetChannels">目标声道数</param>
    /// <returns>适配后的增益数组</returns>
    private static float[] AdaptGains(float[] inputGains, int targetChannels)
    {
        if (inputGains.Length == targetChannels)
            return inputGains.ToArray();

        // 根据目标声道数生成新的增益数组
        var adaptedGains = new float[targetChannels];
        for (int i = 0; i < targetChannels; i++)
        {
            adaptedGains[i] = i < inputGains.Length ? inputGains[i] : 1f; // 默认增益为 1.0
        }
        return adaptedGains;
    }

    public override string Name => "多声道回放增益";

    /// <summary>
    /// 读取音频数据并应用增益
    /// </summary>
    /// <param name="buffer">音频缓冲区</param>
    /// <param name="offset">起始偏移量</param>
    /// <param name="count">要读取的样本数</param>
    /// <returns>实际读取的样本数</returns>
    public override int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = Source.Read(buffer, offset, count);

        // 遍历样本并应用增益
        for (int n = 0; n < samplesRead; n += _channels)
        {
            for (int c = 0; c < _channels; c++)
            {
                int index = offset + n + c;
                if (index >= buffer.Length) continue;

                buffer[index] *= _channelGains[c];
            }
        }

        return samplesRead;
    }
}