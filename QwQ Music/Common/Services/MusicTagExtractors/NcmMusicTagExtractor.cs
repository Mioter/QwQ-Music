using ATL;
using NcmdumpCSharp.Core;
using NcmdumpCSharp.Models;
using QwQ_Music.Common.Interfaces;

namespace QwQ_Music.Common.Services.MusicTagExtractors;

public class NcmMusicTagExtractor(string filePath) : IMusicTagExtractor {
    public Track? GetTrack() {
        using var crypt = new NeteaseCrypt(filePath);
        using MemoryStream? audioStream = crypt.DumpToStream();

        return CreateTrackFromCrypt(crypt, audioStream);
    }

    public async Task<Track?> GetTrackAsync() {
        using var crypt = new NeteaseCrypt(filePath);
        using MemoryStream? audioStream = await crypt.DumpToStreamAsync();

        return CreateTrackFromCrypt(crypt, audioStream);
    }

    private Track? CreateTrackFromCrypt(NeteaseCrypt crypt, Stream? audioStream) {
        if (audioStream == null) {
            LoggerService.Error($"无法提取{filePath}的音频文件流。");

            return null;
        }

        if (crypt.Metadata == null) {
            LoggerService.Error($"NCM文件{filePath}不存在元数据。");

            return null;
        }

        NeteaseMusicMetadata? metadata = crypt.Metadata;

        var track = new Track(audioStream) { Title = metadata.Name, Artist = metadata.Artist, Album = metadata.Album };

        if (crypt.ImageData != null)
            track.EmbeddedPictures.Add(PictureInfo.fromBinaryData(crypt.ImageData));

        return track;
    }
}