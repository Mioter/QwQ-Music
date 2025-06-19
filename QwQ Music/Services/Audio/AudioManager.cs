using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;

namespace QwQ_Music.Services.Audio;

public static class AudioEngineManager
{
    public static AudioEngine? AudioEngine { get; private set; }

    public static void Create(int sampleRate = 48000)
    {
        Dispose();
        AudioEngine = new MiniAudioEngine(sampleRate);
    }

    public static void Dispose()
    {
        AudioEngine?.Dispose();
    }
}
