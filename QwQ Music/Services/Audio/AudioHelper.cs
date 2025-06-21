using System;
using System.Collections.Generic;
using System.IO;
using QwQ_Music.Utilities;
using SoundFlow.Providers;

namespace QwQ_Music.Services.Audio;

public static class AudioHelper
{
    public static double CalcGainOfMusicItem(string filePath, int samplingRate, int channels)
    {
        return ReplayGainCalculator.CalculateGain(
            ReadAudioBlocks(filePath, samplingRate, channels),
            samplingRate,
            channels
        );
    }

    public static IEnumerable<float[]> ReadAudioBlocks(string filePath, int sampleRate, int channels)
    {
        using var fileStream = File.OpenRead(filePath);
        using var reader = new StreamDataProvider(fileStream);
        float[] buffer = new float[sampleRate * channels]; // 1秒缓冲

        int samplesRead;
        while ((samplesRead = reader.ReadBytes(buffer)) > 0)
        {
            float[] actualBuffer = new float[samplesRead];
            Array.Copy(buffer, actualBuffer, samplesRead);
            yield return actualBuffer;
        }
    }
}
