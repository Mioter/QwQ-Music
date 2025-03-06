using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using QwQ_Music.Models;
using QwQ_Music.Services.Audio.Play;
using static QwQ_Music.Models.ConfigInfoModel;
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
        string filePath = Path.Combine(Models.ConfigModel.PlayerConfig.CoverSavePath, coverName);

        // 确保目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // 检查文件是否存在
        if (File.Exists(filePath))
        {
            LoggerService.Info($"Cover already exists: {filePath}. Skipping save.");
            return true;
        }

        try
        {
            await Task.Run(async () =>
                {
                    await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    cover.Value.Save(fs); // 指定格式
                })
                .ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Error($"Failed to save cover {filePath}: {ex.Message}, TypeName: {ex.GetType().Name}");
            return false;
        }
    }

    public static async Task<LyricsModel> ExtractMusicLyricsAsync(string filePath) =>
        await Task.Run(() => LyricsModel.ParseAsync(TagLib.File.Create(filePath).Tag.Lyrics));

    public static async Task<MusicTagExtensions> ExtractMusicInfoExtensionsAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            using var file = TagLib.File.Create(filePath);
            var tag = file.Tag;
            var properties = file.Properties;
            return new MusicTagExtensions(
                string.Join(", ", tag.Genres),
                tag.Year,
                tag.Composers,
                tag.Copyright,
                tag.Disc,
                tag.Track,
                properties.AudioSampleRate,
                properties.AudioBitrate
            );
        });
    }

    public static MusicTagExtensions ExtractMusicInfoExtensions(string filePath)
    {
        using var file = TagLib.File.Create(filePath);
        var tag = file.Tag;
        var properties = file.Properties;
        return new MusicTagExtensions(
            string.Join(", ", tag.Genres),
            tag.Year,
            tag.Composers,
            tag.Copyright,
            tag.Disc,
            tag.Track,
            properties.AudioSampleRate,
            properties.AudioBitrate
        );
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
            using var file = TagLib.File.Create(filePath);
            var tag = file.Tag;
            var properties = file.Properties;
            long fileSizeBytes = new FileInfo(filePath).Length;
            string fileSize = FormatFileSize(fileSizeBytes);
            string title = tag.Title ?? Path.GetFileNameWithoutExtension(filePath);
            string[] artists = tag.Performers;
            string album = tag.Album;
            var duration = properties.Duration;
            string comment = tag.Comment;
            string encodingFormat = properties.Description;
            double gain = ReplayGainCalculator.CalculateGain(filePath);
            string lyrics = tag.Lyrics;
            string allArtists = string.Join(",", artists);

            if (tag.Pictures.Length <= 0)
            {
                return new MusicItemModel(
                    title,
                    artists,
                    album,
                    null,
                    filePath,
                    fileSize,
                    gain,
                    null,
                    duration,
                    encodingFormat,
                    comment
                );
            }
            string coverFileName = CleanFileName(
                $"{(allArtists.Length > 20 ? allArtists[..20] : allArtists)}-{(album.Length > 20 ? album[..20] : album)
                }.jpg"
            );

            await SaveCoverAsync(
                    new Lazy<Bitmap>(new Bitmap(new MemoryStream(tag.Pictures[0].Data.Data))),
                    coverFileName
                )
                .ConfigureAwait(false);
            return new MusicItemModel(
                title,
                artists,
                album,
                Path.Combine(PlayerConfig.CoverSavePath, coverFileName),
                filePath,
                fileSize,
                gain,
                null,
                duration,
                encodingFormat,
                comment
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading metadata from {filePath}: {ex.Message}");
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
        if (ImageCache.TryGetValue(coverPath, out var cachedImage))
        {
            LoggerService.Info($"Cover successfully loaded from cache: {coverPath}");
            return cachedImage;
        }

        if (!File.Exists(coverPath))
            return null;
        using var stream = new FileStream(coverPath, FileMode.Open, FileAccess.Read, FileShare.Read);

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
                $"Unexpected {ex.GetType()} occurred when loading compressed cover image from cache: {coverPath
                }. Error: {ex.Message}"
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
