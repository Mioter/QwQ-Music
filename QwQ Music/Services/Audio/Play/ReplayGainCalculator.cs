using System;
using NAudio.Wave;

namespace QwQ_Music.Services.Audio.Play;

/// <summary>
/// 回放增益计算器（基于EBU R128简化实现）
/// </summary>
public static class ReplayGainCalculator
{
    // 目标响度标准（EBU R128为-23 LUFS，ReplayGain 2.0为-18 LUFS）
    private const double EbuR128Target = -23.0;
    private const double ReplayGain2Target = -18.0;

    /// <summary>
    /// 计算音频文件的回放增益值
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <param name="standard">使用的响度标准</param>
    /// <returns>线性比例增益值（例如：0.5表示降低6dB）</returns>
    public static double CalculateGain(string filePath, ReplayGainStandard standard = ReplayGainStandard.EbuR128)
    {
        using var audioFile = new AudioFileReader(filePath);
        float[] buffer = new float[4096];
        double sum = 0;
        int totalSamples = 0;

        // 1. 计算音频能量总和
        int samplesRead;
        while ((samplesRead = audioFile.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                sum += buffer[i] * buffer[i];
                totalSamples++;
            }
        }

        // 2. 计算RMS值
        double rms = Math.Sqrt(sum / totalSamples);
        if (rms == 0)
            return 1.0; // 静音文件返回0dB增益

        // 3. 转换为dBFS（相对于满量程的分贝值）
        // ReSharper disable once InconsistentNaming
        double dBFS = 20 * Math.Log10(rms);

        // 4. 计算目标增益（转换为线性比例）
        double target = standard == ReplayGainStandard.EbuR128 ? EbuR128Target : ReplayGain2Target;
        double gainDb = target - dBFS;

        return Math.Pow(10, gainDb / 20);
    }
}

/// <summary>
/// 支持的回放增益标准
/// </summary>
public enum ReplayGainStandard
{
    /// <summary>
    /// EBU R128标准（目标-23 LUFS）
    /// </summary>
    EbuR128,

    /// <summary>
    /// ReplayGain 2.0标准（目标-18 LUFS）
    /// </summary>
    ReplayGain2,
}
