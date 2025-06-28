using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATL;
using Avalonia;
using Avalonia.Media.Imaging;
using NcmdumpCSharp.Core;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Utilities;
using File = System.IO.File;

namespace QwQ_Music.Services;

public static class MusicExtractor
{
    private const int DEFAULT_THUMBNAIL_WIDTH = 128; // 默认缩略图宽度，降低了原来的256
    private const int MAX_CACHE_SIZE = 80; // 最大缓存数量

    public static readonly WeakImageCache ImageCache = new();

    public static readonly Bitmap DefaultCover = GetDefaultCover();

    public static async Task<LyricsData> ExtractMusicLyricsAsync(string? filePath)
    {
        var lyricsData = new LyricsData();
        var track = new Track(filePath);
        var lyricsInfo = track.Lyrics;
        var syncLyrics = lyricsInfo.SynchronizedLyrics;
        if (syncLyrics is { Count: > 0 })
        {
            // 按时间点分组
            var grouped = syncLyrics.GroupBy(p => p.TimestampMs).OrderBy(g => g.Key);

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

        string? lyric = lyricsInfo.UnsynchronizedLyrics;
        if (!string.IsNullOrEmpty(lyric))
            return await Task.Run(() => LyricsService.ParseLrcFile(lyric)) ?? lyricsData;

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

        lyric = await File.ReadAllTextAsync(lyricPath);
        return await Task.Run(() => LyricsService.ParseLrcFile(lyric)) ?? lyricsData;
    }

    /// <summary>
    /// 获取扩展信息
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
    /// 获取详细信息
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
    /// 异步获取自定义字段
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <returns>自定义字段信息字典。</returns>
    public static Dictionary<string, string> ExtractAdditionalFieldsAsync(string filePath)
    {
        var track = new Track(filePath);
        return track.AdditionalFields != null ? new Dictionary<string, string>(track.AdditionalFields) : [];
    }

    /// <summary>
    /// 异步提取音乐文件的元数据。
    /// </summary>
    /// <param name="filePath">音乐文件路径。</param>
    /// <returns>包含音乐信息的模型。</returns>
    public static async Task<MusicItemModel?> ExtractMusicInfoAsync(string filePath)
    {
        var (metadata, error) = Path.GetExtension(filePath).ToUpperInvariant() switch
        {
            ".NCM" => await ExtractNcmMetadataAsync(filePath),
            _ => await ExtractTrackMetadataAsync(filePath),
        };

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
        string fileSize = StringProcessor.FormatFileSize(fileInfo.Length);

        string? coverFileName = null;
        Bitmap? coverImage = null;

        if (metadata.CoverImageData != null)
        {
            coverFileName = PrepareCoverInfo(metadata.Artists, metadata.Album, filePath);
            coverImage = new Bitmap(new MemoryStream(metadata.CoverImageData));
            await FileOperation.SaveImageAsync(coverImage, Path.Combine(MainConfig.MusicCoverSavePath, coverFileName));
        }

        return new MusicItemModel(
            metadata.Title,
            metadata.Artists,
            metadata.Composer,
            metadata.Album,
            coverFileName,
            filePath,
            fileSize,
            metadata.Duration,
            metadata.EncodingFormat,
            metadata.Comment,
            coverImage
        );
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

    private static string PrepareCoverInfo(string artists, string album, string filePath)
    {
        bool isArtistsEmpty = string.IsNullOrWhiteSpace(artists);
        bool isAlbumEmpty = string.IsNullOrWhiteSpace(album);
        string timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

        if (isArtistsEmpty && isAlbumEmpty)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return $"{fileName}#{timeStamp}.png";
        }

        string finalArtists = isArtistsEmpty ? $"未知歌手#{timeStamp}" : artists;
        string finalAlbum = isAlbumEmpty ? $"未知专辑#{timeStamp}" : album;

        return GetCoverFileName(finalArtists, finalAlbum);
    }

    /// <summary>
    /// 生成并清理封面文件名。
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
    /// 获取文件流。
    /// </summary>
    /// <param name="filePath">文件路径。</param>
    /// <returns>文件流，如果文件不存在则返回 null。</returns>
    private static async Task<FileStream?> GetFileStream(string filePath)
    {
        string fullFilePath = PathEnsurer.EnsureFullPath(filePath, MainConfig.MusicCoverSavePath);

        if (!File.Exists(fullFilePath))
        {
            await LoggerService.WarningAsync($"File not found: {fullFilePath}");
            return null;
        }
        try
        {
            return await Task.Run(() => new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"Failed to open file stream for: {filePath}. Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 加载压缩的位图。
    /// </summary>
    /// <param name="coverPath">专辑封面索引。</param>
    /// <returns>压缩后的位图。</returns>
    public static async Task<Bitmap?> LoadCompressedBitmap(string coverPath)
    {
        await using var stream = await GetFileStream(coverPath);

        try
        {
            return stream == null ? null : Bitmap.DecodeToWidth(stream, DEFAULT_THUMBNAIL_WIDTH); // 解码并缩放图片，使用较小的宽度
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
    /// 加载原始图片。
    /// </summary>
    /// <param name="coverPath">专辑封面索引。</param>
    /// <returns>原始位图。</returns>
    public static async Task<Bitmap?> LoadOriginalBitmap(string coverPath)
    {
        // 获取文件流
        await using var stream = await GetFileStream(coverPath);

        try
        {
            return stream == null ? null : new Bitmap(stream); // 直接解码图片
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync(
                $"Unexpected {ex.GetType()} occurred when loading original cover image: {coverPath}. Error: {ex.Message}"
            );
            return null;
        }
    }

    /// <summary>
    /// 获取默认专辑封面
    /// </summary>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException">无法找到默认封面资源时抛出异常</exception>
    private static Bitmap GetDefaultCover()
    {
        try
        {
            var assembly = typeof(MusicExtractor).Assembly;
            using var stream =
                assembly.GetManifestResourceStream("QwQ_Music.Assets.Images.看我.png")
                ?? throw new FileNotFoundException("无法找到默认封面图片资源");
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            // 如果资源加载失败，返回一个空位图
            var bitmap = new RenderTargetBitmap(new PixelSize(100, 100));
            return bitmap;
        }
    }

    /// <summary>
    /// 从音频文件中提取专辑封面并保存
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <returns>保存后的封面文件名，如果没有封面则返回null</returns> // 返回文件名
    public static async Task<string?> ExtractAndSaveCoverFromAudioAsync(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null; // 添加空检查

        try
        {
            var track = new Track(filePath);

            if (track.EmbeddedPictures.Count <= 0)
                return null;

            string artists = track.Artist;
            string album = track.Album;
            // 获取清理后的文件名
            string coverFileName = GetCoverFileName(artists, album);

            // 异步保存封面，传递文件名
            bool saved = await FileOperation.SaveImageAsync(
                new Bitmap(new MemoryStream(track.EmbeddedPictures[0].PictureData)),
                Path.Combine(MainConfig.MusicCoverSavePath, coverFileName)
            );

            return saved ? coverFileName : null; // 返回文件名或null
        }
        catch (FileNotFoundException) // 更具体地处理文件未找到异常
        {
            await LoggerService.WarningAsync($"Audio file not found for cover extraction: {filePath}");
            return null;
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"Error extracting cover from audio file {filePath}: {ex.Message}");
            return null;
        }
    }
}
