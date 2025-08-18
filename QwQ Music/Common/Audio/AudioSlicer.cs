using System;
using System.Collections.Generic;
using System.IO;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;

namespace QwQ_Music.Common.Audio;

public class AudioSlicer : IDisposable
{
    private readonly MiniAudioEngine _engine = new();

    public IEnumerable<float[]> ReadAudioBlocks(string filePath, int sampleRate, int channels)
    {
        using var fileStream = File.OpenRead(filePath);
     
        using var reader = new StreamDataProvider(_engine, new AudioFormat
        {
            Format = SampleFormat.F32,
            SampleRate = sampleRate,
            Channels = channels,
        }, fileStream);

        float[] buffer = new float[sampleRate * channels]; // 1秒缓冲

        int samplesRead;

        while ((samplesRead = reader.ReadBytes(buffer)) > 0)
        {
            float[] actualBuffer = new float[samplesRead];
            Array.Copy(buffer, actualBuffer, samplesRead);

            yield return actualBuffer;
        }
    }

    public void Dispose()
    {
        _engine.Dispose();
        GC.SuppressFinalize(this);
    }
}
