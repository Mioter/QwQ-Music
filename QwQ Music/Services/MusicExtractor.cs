using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using QwQ_Music.Models;
using TagLib;
using File = System.IO.File;

namespace QwQ_Music.Services;

public static class MusicExtractor {
    private static readonly Dictionary<string, Bitmap> ImageCache = new();

    /// <summary>
    /// 异步保存专辑封面图片。
    /// </summary>
    /// <param name="cover">专辑封面图片。</param>
    /// <param name="coverName">专辑封面索引。</param>
    private static async Task<bool> SaveCoverAsync(Lazy<Bitmap> cover, string coverName) {
        if (!PlayerConfig.IsInitialized) await PlayerConfig.LoadAsync();
        if (File.Exists(PlayerConfig.CoverSavePath)) {
            LoggerService.Info($"Cover already exists: {coverName}. Skipping save.");
            return true;
        }

        try {
            await Task.Run(
                async () => {
                    await using var fs = new FileStream(
                        PlayerConfig.CoverSavePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None);
                    cover.Value.Save(fs);
                }).ConfigureAwait(false);
            return true;
        } catch (Exception ex) {
            LoggerService.Error(
                $"Unexpected {ex.GetType()} Occured while saving cover {coverName} into {PlayerConfig.CoverSavePath}. {
                    ex.Message}");
            return false;
        }
    }


    public static async Task<LyricsModel> ExtractMusicLyricsAsync(string filePath) =>
        await Task.Run(() => LyricsModel.ParseAsync(TagLib.File.Create(filePath).Tag.Lyrics));


    public static async Task<MusicTagExtensions> ExtractMusicInfoExtensionsAsync(string filePath) {
        return await Task.Run(
            () => {
                using var file = TagLib.File.Create(filePath, ReadStyle.PictureLazy);
                var tag = file.Tag;
                var properties = file.Properties;
                string genre = string.Join(", ", tag.Genres);
                uint year = tag.Year;
                string[] composers = tag.Composers;
                string copyright = tag.Copyright;
                uint disc = tag.Disc;
                uint track = tag.Track;
                int samplingRate = properties.AudioSampleRate;
                int bitrate = properties.AudioBitrate;
                return new MusicTagExtensions(genre, year, composers, copyright, disc, track, samplingRate, bitrate);
            });
    }

    /// <summary>
    /// 异步提取音乐文件的元数据。
    /// </summary>
    /// <param name="filePath">音乐文件路径。</param>
    /// <returns>包含音乐信息的模型。</returns>
    public static async Task<MusicItemModel?> ExtractMusicInfoAsync(string filePath) {
        try {
            using var file = TagLib.File.Create(filePath, ReadStyle.PictureLazy);
            var tag = file.Tag;
            var properties = file.Properties;
            long fileSizeBytes = new FileInfo(filePath).Length;
            string fileSize = FormatFileSize(fileSizeBytes);
            string title = tag.Title;
            string[] artists = tag.Performers;
            string album = tag.Album;
            TimeSpan duration = properties.Duration;
            string comment = tag.Comment;
            string encodingFormat = properties.Description;
            double gain = tag.ReplayGainTrackGain;
            string lyrics = tag.Lyrics;
            string allArtists = string.Join(",", artists);
            string fileNameBase = CleanFileName(
                $"{(allArtists.Length > 20 ? allArtists[..20] : allArtists)}-{(album.Length > 20 ? album[..20] : album)
                }");
            string coverFileName = fileNameBase + ".jpg";

            await SaveCoverAsync(
                    new Lazy<Bitmap>(new Bitmap(new MemoryStream(tag.Pictures[0].Data.Data))),
                    coverFileName)
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
                comment);
        } catch (Exception ex) {
            Console.WriteLine($"Error reading metadata from {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从缓存中加载压缩的位图。
    /// </summary>
    /// <param name="coverPath">专辑封面索引。</param>
    /// <returns>压缩后的位图。</returns>
    public static Bitmap? LoadCompressedBitmapFromCache(string coverPath) {
        if (ImageCache.TryGetValue(coverPath, out var cachedImage)) {
            LoggerService.Info($"Cover successfully loaded from cache: {coverPath}");
            return cachedImage;
        }

        using var stream = new FileStream(coverPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        try {
            // 解码并缩放图片
            var bitmap = Bitmap.DecodeToWidth(stream, 256);

            // 更新缓存
            ImageCache[coverPath] = bitmap;
            return bitmap;
        } catch (Exception ex) {
            LoggerService.Error(
                $"Unexpected {ex.GetType()} occurred when loading compressed cover image from cache: {coverPath
                }. Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 格式化文件大小为人类可读的形式。
    /// </summary>
    /// <param name="bytes">文件大小（字节）。</param>
    /// <returns>格式化后的文件大小。</returns>
    private static string FormatFileSize(long bytes) {
        const int scale = 1024;
        string[] orders = ["GiB", "MiB", "KiB", "Bytes"];
        double len = bytes;
        var order = orders.Length - 1;

        while (len >= scale && order > 0) {
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