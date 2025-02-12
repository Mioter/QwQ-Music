using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;

namespace QwQ_Music.Services;

public static class MultiChannelReplayGain
{

    public async static Task<float[]> CalculateMultiChannelReplayGainAsync(string filePath, double targetRms = 0.1)
    {
        return await Task.Run(() => CalculateMultiChannelReplayGain(filePath, targetRms));
    }

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
            float[] buffer = new float[4096 * channels]; // 按声道数扩展缓冲区

            // 为每个声道创建独立的平方和统计
            double[] channelSumSquares = new double[channels];
            long[] channelSamples = new long[channels];

            int samplesRead;
            while ((samplesRead = audioFileReader.Read(buffer, 0, buffer.Length)) > 0)
            {
                // 按样本步进（每个样本包含所有声道数据）
                for (int i = 0; i < samplesRead; i += channels)
                {
                    // 确保不越界
                    int maxChannel = Math.Min(channels, samplesRead - i);

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
                    gains[ch] = 1.0f;
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

            // 默认返回独立声道增益
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"多声道增益计算失败: {ex.Message}");
            return
            [
                1.0f,
            ]; // 安全返回值
        }
    }
}
