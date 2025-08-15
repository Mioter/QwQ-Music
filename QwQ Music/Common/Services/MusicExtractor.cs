using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATL;
using Avalonia.Media.Imaging;
using NcmdumpCSharp.Core;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Common.Utilities.StringUtilities;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using File = System.IO.File;

namespace QwQ_Music.Common.Services;

public static class MusicExtractor
{
    public static async Task<LyricsData> ExtractMusicLyricsAsync(string? filePath)
    {
        var lyricsData = new LyricsData();
        var track = new Track(filePath);
        var lyricsList = track.Lyrics;

        // 查找同步歌词
        var syncLyricsInfo = lyricsList.FirstOrDefault(l => l.SynchronizedLyrics.Count > 0);

        if (syncLyricsInfo != null)
        {
            var syncLyrics = syncLyricsInfo.SynchronizedLyrics;

            // 按时间点分组
            var grouped = syncLyrics.GroupBy(p => p.TimestampStart).OrderBy(g => g.Key);

            var lyricLines = new List<LyricLine>();

            foreach (var group in grouped)
            {
                var phrases = group.ToList();

                string? primary,
                    translation = null;

                // 方案二：尝试分隔符
                string[] split = phrases[0].Text.Split(["//", "|", "\n"], StringSplitOptions.RemoveEmptyEntries);

                if (split.Length == 2)
                {
                    primary = split[0].Trim();
                    translation = split[1].Trim();
                }
                else
                {
                    primary = phrases[0].Text.Trim();

                    // 方案一：同一时间点有多条
                    if (phrases.Count > 1)
                        translation = phrases[1].Text.Trim();
                }

                lyricLines.Add(new LyricLine(group.Key / 1000.0, primary, translation));
            }

            lyricsData.Lyrics = lyricLines;

            return lyricsData;
        }

        // 查找非同步歌词
        var unsyncLyricsInfo = lyricsList.FirstOrDefault(l => !string.IsNullOrEmpty(l.UnsynchronizedLyrics));

        if (unsyncLyricsInfo != null)
        {
            string? lyric = unsyncLyricsInfo.UnsynchronizedLyrics;

            if (!string.IsNullOrEmpty(lyric))
                return await Task.Run(() => LyricsService.ParseLrcFile(lyric)) ?? lyricsData;
        }

        // 获取目录路径
        string? directoryPath = Path.GetDirectoryName(filePath);

        // 获取文件名（不含扩展名）
        string? fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

        // 拼接完整路径（不含扩展名）
        if (directoryPath == null || fileNameWithoutExtension == null)
            return lyricsData;

        string fullPathWithoutExtension = Path.Combine(directoryPath, fileNameWithoutExtension);
        string lyricPath = fullPathWithoutExtension + ".lrc";

        if (!Path.Exists(lyricPath))
            return lyricsData;

        string lyricText = await File.ReadAllTextAsync(lyricPath);

        return await Task.Run(() => LyricsService.ParseLrcFile(lyricText)) ?? lyricsData;
    }

    /// <summary>
    ///     获取扩展信息
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <returns>包含扩展信息的字典。</returns>
    public static MusicTagExtensions ExtractExtensionsInfo(string? filePath)
    {
        var track = new Track(filePath);

        return new MusicTagExtensions(
            track.Genre,
            track.Year,
            track.Copyright,
            track.DiscNumber.HasValue ? (uint)track.DiscNumber.Value : 0,
            track.TrackNumber.HasValue ? (uint)track.TrackNumber.Value : 0,
            (int)track.SampleRate,
            track.ChannelsArrangement.NbChannels,
            track.Bitrate,
            track.BitDepth,

            // 添加更多基本信息
            track.OriginalAlbum,
            track.OriginalArtist,
            track.AlbumArtist,
            track.Publisher,
            track.Description,
            track.Language,

            // 添加技术信息
            track.IsVBR,
            track.AudioFormat.DataFormat.Name,
            track.Encoder
        );
    }

    /// <summary>
    ///     获取详细信息
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <returns>包含详细信息的字典。</returns>
    public static MusicDetailedInfo ExtractDetailedInfoAsync(string filePath)
    {
        var track = new Track(filePath);

        return new MusicDetailedInfo(

            // 发布信息
            track.Date,
            track.OriginalReleaseDate,
            track.PublishingDate,

            // 专业信息
            track.ISRC,
            track.CatalogNumber,
            track.ProductId,

            // 其他信息
            track.BPM,
            track.Popularity,
            track.SeriesTitle,
            track.SeriesPart,
            track.LongDescription,
            track.Group,

            // 技术信息
            track.TechnicalInformation.AudioDataOffset,
            track.TechnicalInformation.AudioDataSize
        );
    }

    /// <summary>
    ///     异步获取自定义字段
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <returns>自定义字段信息字典。</returns>
    public static Dictionary<string, string> ExtractAdditionalFieldsAsync(string filePath)
    {
        var track = new Track(filePath);

        return track.AdditionalFields != null ? new Dictionary<string, string>(track.AdditionalFields) : [];
    }

    /// <summary>
    ///     异步提取音乐文件的元数据。
    /// </summary>
    /// <param name="filePath">音乐文件路径。</param>
    /// <returns>包含音乐信息的模型。</returns>
    public static async Task<MusicItemModel?> ExtractMusicInfoAsync(string filePath)
    {
        MusicMetadata? metadata;
        Exception? error;
        string extension = Path.GetExtension(filePath).ToUpper();

        if (extension == AudioFileValidator.AudioFormatsExtendToNameMap[AudioFileValidator.ExtendAudioFormats.Ncm])
        {
            (metadata, error) = await ExtractNcmMetadataAsync(filePath);
        }
        else
        {
            (metadata, error) = await ExtractTrackMetadataAsync(filePath);
        }

        if (error != null)
        {
            await LoggerService.ErrorAsync($"Error reading metadata from {filePath}: {error.Message}");

            return null;
        }

        if (metadata == null)
        {
            await LoggerService.ErrorAsync($"Failed to extract metadata from {filePath} for an unknown reason.");

            return null;
        }

        var fileInfo = new FileInfo(filePath);
        string fileSize = StringFormatter.FormatFileSize(fileInfo.Length);

        Bitmap? coverImage = null;

        if (metadata.CoverImageData != null)
        {
            coverImage = Bitmap.DecodeToWidth(new MemoryStream(metadata.CoverImageData), 128);
        }

        return new MusicItemModel
        {
            Title = metadata.Title,
            Artists = metadata.Artists,
            Album = metadata.Album,
            Composer = metadata.Composer,
            FilePath = filePath,
            FileSize = fileSize,
            Duration = metadata.Duration,
            EncodingFormat = metadata.EncodingFormat,
            Comment = metadata.Comment,
            CoverImage = coverImage,
        };
    }

    private static Task<(MusicMetadata?, Exception?)> ExtractNcmMetadataAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                using var crypt = new NeteaseCrypt(filePath);

                if (crypt.Metadata == null)
                {
                    return (null, new InvalidDataException("NCM metadata is null."));
                }

                var metadata = crypt.Metadata;

                var musicMetadata = new MusicMetadata
                {
                    Title = string.IsNullOrWhiteSpace(metadata.Name)
                        ? Path.GetFileNameWithoutExtension(filePath)
                        : metadata.Name,
                    Artists = metadata.Artist,
                    Album = metadata.Album,
                    Duration = TimeSpan.FromMilliseconds(metadata.Duration),
                    EncodingFormat = metadata.Format,
                    CoverImageData = crypt.ImageData,
                };

                return ((MusicMetadata?)musicMetadata, (Exception?)null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        });
    }

    private static Task<(MusicMetadata?, Exception?)> ExtractTrackMetadataAsync(string filePath)
    {
        return Task.Run(() =>
        {
            try
            {
                var track = new Track(filePath);

                var musicMetadata = new MusicMetadata
                {
                    Title = string.IsNullOrWhiteSpace(track.Title)
                        ? Path.GetFileNameWithoutExtension(filePath)
                        : track.Title,
                    Artists = track.Artist,
                    Album = track.Album,
                    Composer = track.Composer,
                    Comment = track.Comment,
                    Duration = TimeSpan.FromMilliseconds(track.DurationMs),
                    EncodingFormat = track.AudioFormat.ShortName,
                    CoverImageData = track.EmbeddedPictures.Count > 0 ? track.EmbeddedPictures[0].PictureData : null,
                };

                return ((MusicMetadata?)musicMetadata, (Exception?)null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        });
    }

    public static string PrepareCoverInfo(string? artists, string? album, string filePath)
    {
        if (string.IsNullOrWhiteSpace(artists) && string.IsNullOrWhiteSpace(album))
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            return $"{fileName}#{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png";
        }

        string finalArtists = string.IsNullOrWhiteSpace(artists)
            ? $"未知歌手#{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : artists;

        string finalAlbum = string.IsNullOrWhiteSpace(album)
            ? $"未知专辑#{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
            : album;

        return GetCoverFileName(finalArtists, finalAlbum);
    }

    /// <summary>
    ///     生成并清理封面文件名。
    /// </summary>
    /// <param name="artists">艺术家</param>
    /// <param name="album">专辑</param>
    /// <returns>清理后的封面文件名 (例如 "Artist-Album.jpg")</returns>
    private static string GetCoverFileName(string artists, string album)
    {
        // 截断并清理文件名
        string safeArtists = string.IsNullOrWhiteSpace(artists) ? "未知歌手" : artists;
        string safeAlbum = string.IsNullOrWhiteSpace(album) ? "未知专辑" : album;

        string coverFileName = PathEnsurer.CleanFileName(
            $"{(safeArtists.Length > 20 ? safeArtists[..20] : safeArtists)}-{(safeAlbum.Length > 20 ? safeAlbum[..20] : safeAlbum)}.png"
        );

        return coverFileName; // 只返回文件名
    }

    /// <summary>
    ///     获取文件流。
    /// </summary>
    /// <param name="filePath">文件路径。</param>
    /// <returns>文件流，如果文件不存在则返回 null。</returns>
    private static async Task<FileStream?> GetMusicCoverStream(string filePath)
    {
        if (!File.Exists(filePath))
        {
            await LoggerService.WarningAsync($"File not found: {filePath}");

            return null;
        }

        try
        {
            return await Task.Run(() => new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"Failed to open file stream for: {filePath}. Error: {ex.Message}");

            return null;
        }
    }

    public static string GetMusicCoverFullPath(string filePath)
    {
        return Path.Combine(StaticConfig.MusicCoverSavePath, filePath);
    }

    public static string GetMusicListCoverFullPath(string filePath)
    {
        return Path.Combine(StaticConfig.MusicListCoverSavePath, filePath);
    }

    /// <summary>
    ///     加载压缩的位图。
    /// </summary>
    /// <param name="coverPath">专辑封面索引。</param>
    /// <param name="size">目标尺寸，默认128</param>
    /// <returns>压缩后的位图。</returns>
    public static async Task<Bitmap?> LoadCompressedBitmapFromFileAsync(string coverPath, int size = 128)
    {
        await using var stream = await GetMusicCoverStream(coverPath);

        try
        {
            return stream == null ? null : Bitmap.DecodeToWidth(stream, size); // 解码并缩放图片，使用较小的宽度
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync(
                $"Unexpected {ex.GetType()} occurred when loading compressed cover image from cache: {coverPath}. Error: {ex.Message}"
            );

            return null;
        }
    }

    /// <summary>
    ///     从文件系统中加载位图。
    /// </summary>
    /// <param name="coverPath">图片路径。</param>
    /// <returns>原始位图。</returns>
    public static async Task<Bitmap?> LoadBitmapFromFileAsync(string coverPath)
    {
        // 获取文件流
        await using var stream = await GetMusicCoverStream(coverPath);

        try
        {
            return stream == null ? null : new Bitmap(stream); // 直接解码图片
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync(
                $"意外的 {ex.GetType()} 类型错误发生在加载原始封面图像时: {coverPath}\n" +
                $"{ex.Message}\n{ex.StackTrace}"
            );

            return null;
        }
    }

    /// <summary>
    ///     从音频文件中提取专辑封面（支持 .ncm 和常规音频文件）
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <returns>提取的封面原始位图。</returns>
    public static async Task<Bitmap?> GetCoverFromAudioAsync(string filePath)
    {
        try
        {
            string extension = Path.GetExtension(filePath).ToUpperInvariant();

            // 判断是否为 NCM 格式
            if (extension == AudioFileValidator.AudioFormatsExtendToNameMap[AudioFileValidator.ExtendAudioFormats.Ncm])
            {
                return await Task.Run(() =>
                {
                    try
                    {
                        using var crypt = new NeteaseCrypt(filePath);

                        if (crypt.ImageData == null || crypt.ImageData.Length == 0)
                            return null;

                        return new Bitmap(new MemoryStream(crypt.ImageData));
                    }
                    catch (Exception ex)
                    {
                        LoggerService.ErrorAsync($"解析 NCM 封面时出错: {filePath}, 错误: {ex.Message}").Wait();

                        return null;
                    }
                });
            }

            // 普通音频文件处理逻辑
            var track = new Track(filePath);

            if (track.EmbeddedPictures.Count <= 0)
                return null;

            byte[]? pictureData = track.EmbeddedPictures[0].PictureData;

            if (pictureData == null || pictureData.Length == 0)
                return null;

            return new Bitmap(new MemoryStream(pictureData));
        }
        catch (FileNotFoundException)
        {
            await LoggerService.WarningAsync($"找不到用于封面提取的音频文件: {filePath}");

            return null;
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"从音频文件中提取封面时出错: {filePath}: {ex.Message}");

            return null;
        }
    }

    private sealed class MusicMetadata
    {
        public string Title { get; set; } = string.Empty;

        public string Artists { get; set; } = string.Empty;

        public string Album { get; set; } = string.Empty;

        public string Composer { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;

        public string EncodingFormat { get; set; } = string.Empty;

        public TimeSpan Duration { get; set; }

        public byte[]? CoverImageData { get; set; }
    }
}
