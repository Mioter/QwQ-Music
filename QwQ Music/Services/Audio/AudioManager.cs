using System;
using System.Threading.Tasks;
using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;

namespace QwQ_Music.Services.Audio;

public static class AudioEngineManager
{
    public static AudioEngine? AudioEngine { get; private set; }

    public static async void Create(int sampleRate = 48000)
    {
        try
        {
            Dispose();
            AudioEngine = await Task.Run(() => new MiniAudioEngine(sampleRate));
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static void Dispose()
    {
        AudioEngine?.Dispose();
    }
}
