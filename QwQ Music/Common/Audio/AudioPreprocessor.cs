using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NcmdumpCSharp.Core;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Models;
using QwQ_Music.ViewModels;
using SoundFlow.Enums;
using SoundFlow.Providers;
using SoundFlow.Structs;
using Track = ATL.Track;

namespace QwQ_Music.Common.Audio;

public static class AudioPreprocessor
{
    public static AudioFormat UpdateAudioFormat(MusicItemModel model)
    {
        var ex = MusicExtractor.ExtractExtensionsInfo(model.FilePath);

        return new AudioFormat
        {
            SampleRate = ConfigManager.PlayerConfig.IsAutoSetSampleRate ? ex.SamplingRate : ConfigManager.PlayerConfig.SampleRate,
            Channels = ex.Channels,
            Format = SampleFormat.F32,
        };
    }
    
    public static async Task<AudioFormat> UpdateNcmAudioFormat(MusicItemModel model)
    {
        using var crypt = new NeteaseCrypt(model.FilePath);
        var (audioStream, _) = await crypt.DumpToStreamAsync();

        var track = new Track(audioStream);

        return new AudioFormat
        {
            SampleRate = ConfigManager.PlayerConfig.IsAutoSetSampleRate ? (int)track.SampleRate : ConfigManager.PlayerConfig.SampleRate,
            Channels = track.ChannelsArrangement.NbChannels,
            Format = SampleFormat.F32,
        };
    }
    
    public static double CalcGainOfMusicItem(MusicItemModel musicItem)
    {
        string extension = Path.GetExtension(musicItem.FilePath).ToUpper();

        if (extension == AudioFileValidator.AudioFormatsExtendToNameMap[AudioFileValidator.ExtendAudioFormats.Ncm])
        {
            NotificationService.Warning($"暂不支持对Ncm文件计算回放增益！《{musicItem.Title}》已使用默认值: 1 ");

            return 1;
        }

        var ex = MusicExtractor.ExtractExtensionsInfo(musicItem.FilePath);

        return ReplayGainCalculator.CalculateGain(
            ReadAudioBlocks(musicItem.FilePath, ex.SamplingRate, ex.Channels),
            ex.SamplingRate,
            ex.Channels
        );
    }

    public static IEnumerable<float[]> ReadAudioBlocks(string filePath, int sampleRate, int channels)
    {
        using var fileStream = File.OpenRead(filePath);

        using var reader = new StreamDataProvider(MusicPlayerViewModel.Default.AudioEngine, new AudioFormat
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
}
