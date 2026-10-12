using ATL;
using NcmdumpCSharp.Core;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Models;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Providers;
using AudioFormat = SoundFlow.Structs.AudioFormat;

namespace QwQ_Music.Common.Services;

public class AudioGainCalculator : IDisposable {
    private const double _DEFAULT_GAIN = 1.0;

    private readonly MiniAudioEngine _engine =
        Task.Run(() => new MiniAudioEngine()).ConfigureAwait(false).GetAwaiter().GetResult();

    public void Dispose() {
        _engine.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<double> CalculateGainAsync(
        MusicItemModel item,
        MusicReplayGainStandard standard,
        double customTargetLufs) {
        try {
            if (!File.Exists(item.FilePath)) {
                NotificationService.Error($"文件未找到: {item.FilePath}");

                return _DEFAULT_GAIN;
            }

            await using Stream? audioStream = await GetAudioStreamAsync(item).ConfigureAwait(false);

            if (audioStream == null) {
                NotificationService.Error($"从文件中解析音频流失败！《{item.Title}》使用默认增益值: {_DEFAULT_GAIN}");

                return _DEFAULT_GAIN;
            }

            var track = new Track(audioStream);
            int sampleRate = (int)track.SampleRate;
            int channels = track.ChannelsArrangement.NbChannels;

            if (sampleRate <= 0 || channels <= 0) {
                NotificationService.Error($"音频元数据无效！《{item.Title}》使用默认增益值: {_DEFAULT_GAIN}");

                return _DEFAULT_GAIN;
            }

            IEnumerable<float[]> audioBlocks = ReadAudioBlocks(audioStream, sampleRate, channels);

            return ReplayGainCalculator.CalculateGain(audioBlocks, sampleRate, channels, standard, customTargetLufs);
        } catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or InvalidOperationException) {
            NotificationService.Error($"处理音频文件时出错: {ex.Message}");

            return _DEFAULT_GAIN;
        }
    }

    private static async Task<Stream?> GetAudioStreamAsync(MusicItemModel item) {
        string extension = Path.GetExtension(item.FilePath).ToUpperInvariant();

        if (extension == AudioFileValidator.AudioFormatsExtendToNameMap[AudioFileValidator.ExtendAudioFormats.Ncm]) {
            var crypt = new NeteaseCrypt(item.FilePath);

            return await crypt.DumpToStreamAsync().ConfigureAwait(false); // caller disposes
        }

        return File.OpenRead(item.FilePath); // caller disposes
    }

    private IEnumerable<float[]> ReadAudioBlocks(Stream stream, int sampleRate, int channels) {
        using var reader = new StreamDataProvider(
            _engine,
            new AudioFormat { Format = SampleFormat.F32, SampleRate = sampleRate, Channels = channels },
            stream);

        float[] buffer = new float[sampleRate * channels]; // 1秒缓冲

        int samplesRead;

        while ((samplesRead = reader.ReadBytes(buffer)) > 0) {
            float[] actualBuffer = new float[samplesRead];
            Array.Copy(buffer, actualBuffer, samplesRead);

            yield return actualBuffer;
        }
    }
}