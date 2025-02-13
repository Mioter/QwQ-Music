using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;

namespace QwQ_Music.Services.Audio;

public static class MultiChannelReplayGain
{
    /// <summary>
    /// 异步计算多声道回放增益。
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <param name="targetRms">目标 RMS 值（默认 0.1）。</param>
    /// <returns>每个声道的增益值数组。</returns>
    public async static Task<float[]> CalculateMultiChannelReplayGainAsync(string filePath, double targetRms = 0.1)
    {
        return await Task.Run(() => CalculateMultiChannelReplayGain(filePath, targetRms));
    }

    /// <summary>
    /// 计算多声道回放增益。
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <param name="targetRms">目标 RMS 值（默认 0.1）。</param>
    /// <param name="useMaxRms">是否使用最大增益值（防止削波）。</param>
    /// <returns>每个声道的增益值数组。</returns>
    public static float[] CalculateMultiChannelReplayGain(
        string filePath,
        double targetRms = 0.1,
        bool useMaxRms = false
        )
    {
        try
        {
            using var audioFileReader = new AudioFileReader(filePath);
            var format = audioFileReader.WaveFormat;
            int channels = format.Channels;

            // 缓冲区大小为 4096 个样本 * 声道数
            int bufferSize = 4096 * channels;
            float[] buffer = new float[bufferSize]; // 使用堆分配缓冲区

            // 为每个声道创建独立的平方和统计
            double[] channelSumSquares = new double[channels];
            long[] channelSamples = new long[channels];

            int samplesRead;
            while ((samplesRead = audioFileReader.Read(buffer, 0, buffer.Length)) > 0)
            {
                // 按样本步进（每个样本包含所有声道数据）
                for (int i = 0; i < samplesRead; i += channels)
                {
                    int maxChannel = Math.Min(channels, samplesRead - i); // 防止越界
                    for (int ch = 0; ch < maxChannel; ch++)
                    {
                        float sample = buffer[i + ch];
                        channelSumSquares[ch] += sample * sample;
                        channelSamples[ch]++;
                    }
                }
            }

            // 计算各声道增益
            float[] gains = new float[channels];
            for (int ch = 0; ch < channels; ch++)
            {
                if (channelSamples[ch] == 0)
                {
                    gains[ch] = 1.0f; // 如果没有样本，设置为默认增益
                    continue;
                }

                double rms = Math.Sqrt(channelSumSquares[ch] / channelSamples[ch]);
                gains[ch] = (float)(targetRms / rms);
            }

            // 返回策略选择
            if (!useMaxRms) return gains;

            // 取最大增益值（防止削波）
            float maxGain = gains.Max();
            return Enumerable.Repeat(maxGain, channels).ToArray();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"多声道增益计算失败: {ex.Message}");
            return [1.0f]; // 安全返回值
        }
    }
}
