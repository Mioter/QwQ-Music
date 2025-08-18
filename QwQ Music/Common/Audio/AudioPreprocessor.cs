using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Models;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace QwQ_Music.Common.Audio;

public static class AudioPreprocessor
{
    public static double CalcGainOfMusicItem(AudioSlicer audioSlicer, MusicItemModel musicItem)
    {
        var ex = MusicExtractor.ExtractExtensionsInfo(musicItem.FilePath);
        return ReplayGainCalculator.CalculateGain(
            audioSlicer.ReadAudioBlocks(musicItem.FilePath, ex.SamplingRate, ex.Channels),
            ex.SamplingRate,
            ex.Channels
        );
    }

    public static void UpdateAudioFormat(AudioPlay audioPlay, MusicItemModel model)
    {
        var ex = MusicExtractor.ExtractExtensionsInfo(model.FilePath);

        audioPlay.AudioFormat = new AudioFormat
        {
            SampleRate = ConfigManager.PlayerConfig.IsAutoSetSampleRate ? ex.SamplingRate : ConfigManager.PlayerConfig.SampleRate,
            Channels = ex.Channels,
            Format = SampleFormat.F32,
        };
    }
}
