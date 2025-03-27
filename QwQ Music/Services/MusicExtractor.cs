using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATL;
using Avalonia.Media.Imaging;
using QwQ_Music.Models;
using File = System.IO.File;
using PlayerConfig = QwQ_Music.Models.ConfigModel.PlayerConfig;

namespace QwQ_Music.Services;

public static class MusicExtractor
{
    private static readonly Dictionary<string, Bitmap> ImageCache = new();

    /// <summary>
    /// 异步保存专辑封面图片。
    /// </summary>
    /// <param name="cover">专辑封面图片。</param>
    /// <param name="coverName">专辑封面索引。</param>
    private async static Task<bool> SaveCoverAsync(Lazy<Bitmap> cover, string coverName)
    {
        // 构造完整文件路径
        string filePath = Path.Combine(PlayerConfig.CoverSavePath, coverName);

        // 确保目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // 检查文件是否存在
        if (File.Exists(filePath))
        {
            /*LoggerService.Info($"Cover already exists: {filePath}. Skipping save.");*/
            return true;
        }

        try
        {
            await Task.Run(async () =>
                {
                    await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    cover.Value.Save(fs); // 指定格式
                }).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Error($"Failed to save cover {filePath}: {ex.Message}, TypeName: {ex.GetType().Name}");
            return false;
        }
    }

    public async static Task<LyricsModel> ExtractMusicLyricsAsync(string filePath) =>
        await Task.Run(() => LyricsModel.ParseAsync(new Track(filePath).Lyricist));

    /// <summary>
    /// 异步获取扩展信息
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <returns>包含扩展信息的字典。</returns>
    public async static Task<MusicTagExtensions> ExtractExtensionsInfoAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var track = new Track(filePath);
            return new MusicTagExtensions(
                track.Genre ?? string.Empty,
                track.Year,
                track.Composer?.Split(',').Select(c => c.Trim()).ToArray() ?? [],
                track.Copyright ?? string.Empty,
                track.DiscNumber.HasValue ? (uint)track.DiscNumber.Value : 0,
                track.TrackNumber.HasValue ? (uint)track.TrackNumber.Value : 0,
                (int)track.SampleRate,
                track.ChannelsArrangement.NbChannels,
                track.Bitrate,
                track.BitDepth,
                // 添加更多基本信息
                track.OriginalAlbum ?? string.Empty,
                track.OriginalArtist ?? string.Empty,
                track.AlbumArtist ?? string.Empty,
                track.Publisher ?? string.Empty,
                track.Description ?? string.Empty,
                track.Language ?? string.Empty,
                // 添加技术信息
                track.IsVBR,
                track.AudioFormat.ShortName,
                track.Encoder ?? string.Empty
            );
        });
    }
    
    /// <summary>
    /// 异步获取详细信息
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <returns>包含详细信息的字典。</returns>
    public async static Task<MusicDetailedInfo> ExtractDetailedInfoAsync(string filePath)
    {
        return await Task.Run(() =>
        {
        var track = new Track(filePath);
        return new MusicDetailedInfo(
            // 发布信息
            track.Date,
            track.OriginalReleaseDate,
            track.PublishingDate,
            // 专业信息
            track.ISRC ?? string.Empty,
            track.CatalogNumber ?? string.Empty,
            track.ProductId ?? string.Empty,
            // 其他信息
            track.BPM,
            track.Popularity,
            track.SeriesTitle ?? string.Empty,
            track.SeriesPart ?? string.Empty,
            track.LongDescription ?? string.Empty,
            track.Group ?? string.Empty,
            // 技术信息
            track.TechnicalInformation.AudioDataOffset,
            track.TechnicalInformation.AudioDataSize
        );
        });
    }
    
    /// <summary>
    /// 异步获取自定义字段
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <returns>自定义字段信息字典。</returns>
    public async static Task<Dictionary<string, string>> ExtractAdditionalFieldsAsync(string filePath)
    {
        return await Task.Run(() =>
        {
        var track = new Track(filePath);
        return track.AdditionalFields != null 
            ? new Dictionary<string, string>(track.AdditionalFields) 
            : new Dictionary<string, string>();
        });
    }

    /// <summary>
    /// 异步提取音乐文件的元数据。
    /// </summary>
    /// <param name="filePath">音乐文件路径。</param>
    /// <returns>包含音乐信息的模型。</returns>
    public async static Task<MusicItemModel?> ExtractMusicInfoAsync(string filePath)
    {
        try
        {
            var track = new Track(filePath);
            long fileSizeBytes = new FileInfo(filePath).Length;
            string fileSize = FormatFileSize(fileSizeBytes);
            string title = track.Title ?? Path.GetFileNameWithoutExtension(filePath);
            string composer = track.Composer;
            string artists = track.Artist;
            string album = track.Album;
            var duration = TimeSpan.FromMilliseconds(track.DurationMs);
            string comment = track.Comment;
            string encodingFormat = track.Description;
            string allArtists = string.Join(",", artists);

            if (track.EmbeddedPictures.Count <= 0)
            {
                return new MusicItemModel(
                    title,
                    artists, 
                    composer,
                    album,
                    null,
                    filePath,
                    fileSize,
                    null,
                    duration,
                    encodingFormat,
                    comment
                );
            }
            string coverFileName = CleanFileName(
                $"{(allArtists.Length > 20 ? allArtists[..20] : allArtists)}-{(album?.Length > 20 ? album[..20] : album)
                }.jpg"
            );

            await SaveCoverAsync(
                    new Lazy<Bitmap>(new Bitmap(new MemoryStream(track.EmbeddedPictures[0].PictureData))),
                    coverFileName
                )
                .ConfigureAwait(false);
            return new MusicItemModel(
                title,
                artists, composer,
                album,
                Path.Combine(PlayerConfig.CoverSavePath, coverFileName),
                filePath,
                fileSize,
                null,
                duration,
                encodingFormat,
                comment
            );
        }
        catch (Exception ex)
        {
            LoggerService.Error($"Error reading metadata from {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取文件流。
    /// </summary>
    /// <param name="filePath">文件路径。</param>
    /// <returns>文件流，如果文件不存在则返回 null。</returns>
    private static FileStream? GetFileStream(string filePath)
    {
        if (!File.Exists(filePath))
        {
            LoggerService.Warning($"File not found: {filePath}");
            return null;
        }
        try
        {
            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception ex)
        {
            LoggerService.Error($"Failed to open file stream for: {filePath}. Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从缓存中加载压缩的位图。
    /// </summary>
    /// <param name="coverPath">专辑封面索引。</param>
    /// <returns>压缩后的位图。</returns>
    public static Bitmap? LoadCompressedBitmapFromCache(string coverPath)
    {
        // 尝试从缓存中加载
        if (ImageCache.TryGetValue(coverPath, out var cachedImage))
        {
            return cachedImage;
        }

        // 获取文件流
        using var stream = GetFileStream(coverPath);
        if (stream == null)
        {
            return null;
        }

        try
        {
            // 解码并缩放图片
            var bitmap = Bitmap.DecodeToWidth(stream, 256);

            // 更新缓存
            ImageCache[coverPath] = bitmap;
            return bitmap;
        }
        catch (Exception ex)
        {
            LoggerService.Error(
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
    public static Bitmap? LoadOriginalBitmap(string coverPath)
    {
        // 获取文件流
        using var stream = GetFileStream(coverPath);
        if (stream == null)
        {
            return null;
        }

        try
        {
            // 直接解码图片
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            LoggerService.Error(
                $"Unexpected {ex.GetType()} occurred when loading original cover image: {coverPath}. Error: {ex.Message}"
            );
            return null;
        }
    }

    /// <summary>
    /// 格式化文件大小为人类可读的形式。
    /// </summary>
    /// <param name="bytes">文件大小（字节）。</param>
    /// <returns>格式化后的文件大小。</returns>
    private static string FormatFileSize(long bytes)
    {
        const int scale = 1024;
        string[] orders = ["GiB", "MiB", "KiB", "Bytes"];
        double len = bytes;
        int order = orders.Length - 1;

        while (len >= scale && order > 0)
        {
            order--;
            len /= scale;
        }

        return $"{len:0.0}{orders[order]}";
    }

    /// <summary>
    /// 清理文件名中的非法字符。
    /// </summary>
    /// <param name="fileName">原始文件名。</param>
    /// <returns>清理后的文件名。</returns>
    private static string CleanFileName(string fileName) =>
        Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c, '&'));
}
