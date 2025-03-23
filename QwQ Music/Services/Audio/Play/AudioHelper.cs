using System;
using System.Collections.Generic;
using System.IO;
using QwQ_Music.Models;
using QwQ_Music.Utilities;
using SoundFlow.Providers;

namespace QwQ_Music.Services.Audio.Play;

public static class AudioHelper
{

    public static void CalcGainOfMusicItem(MusicItemModel item)
    {
        var ex = item.GetExtensionsInfo().GetAwaiter().GetResult();
        item.Gain = ReplayGainCalculator.CalculateGain(
            ReadAudioBlocks(item.FilePath,ex.SamplingRate,ex.Channels),
            ex.SamplingRate,
            ex.Channels);
    }
    
    public static IEnumerable<float[]> ReadAudioBlocks(string filePath,int sampleRate, int channels)
    {
        var fileStream = File.OpenRead(filePath);
        var reader = new StreamDataProvider(fileStream);
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
