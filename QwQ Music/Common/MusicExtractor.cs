using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using QwQ_Music.Models;
using TagLib;
using File = System.IO.File;

namespace QwQ_Music.Common;

public static class MusicExtractor
{
    private static readonly Dictionary<string, Bitmap> ImageCache = new();

    private async static Task SaveAlbumImageAsync(Bitmap albumImage, string albumImageIndexes)
    {
        string imagePath = GetAlbumImagePath(albumImageIndexes);

        if (File.Exists(imagePath)) // 如果图片已经存在，跳过保存
        {
            Console.WriteLine($"Image already exists: {imagePath}. Skipping save.");
            return;
        }

        await Task.Run(() => albumImage.Save(imagePath)); // 否则保存图片
    }

    public async static Task<MusicItemModel?> ExtractMusicInfoAsync(string filePath)
    {
        try
        {
            var file = TagLib.File.Create(filePath);
            var tag = file.Tag;
            var properties = file.Properties;

            long fileSizeBytes = new FileInfo(filePath).Length; // 获取文件大小
            string fileSize = FormatFileSize(fileSizeBytes); // 格式化文件大小

            string title = tag.Title;
            string singer = string.Join(", ", tag.Performers);
            string album = tag.Album;
            string genre = string.Join(", ", tag.Genres);
            string duration = FormatDuration(properties.Duration);

            string year = tag.Year.ToString();
            string comment = tag.Comment;
            string composer = string.Join(", ", tag.Composers);
            string copyright = tag.Copyright;
            string discNumber = tag.Disc.ToString();
            string trackNumber = tag.Track.ToString();

            if (tag.Pictures.Length <= 0)
                return new MusicItemModel(title, singer, filePath, fileSize, duration, album, genre, null, "00:00", year, comment, composer, copyright, discNumber, trackNumber);

            using var ms = new MemoryStream(tag.Pictures[0].Data.Data);
            var albumImage = new Bitmap(ms);

            string albumImageIndexes = GetAlbumImagePath(Path.GetFileNameWithoutExtension(filePath));

            // 在保存之前检查是否已存在
            await SaveAlbumImageAsync(albumImage, albumImageIndexes);

            return new MusicItemModel(title, singer, filePath, fileSize, duration, album, genre, albumImageIndexes, "00:00", year, comment, composer, copyright, discNumber, trackNumber);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading metadata from {filePath}: {ex.Message}");
            return null;
        }
    }

    public static Bitmap? LoadAlbumImageFromCache(string albumImageIndexes)
    {
        
        if (ImageCache.TryGetValue(albumImageIndexes, out var cachedImage))
        {
            Console.WriteLine($"Image loaded from cache: {albumImageIndexes}");
            return cachedImage;
        }

        string imagePath = GetAlbumImagePath(albumImageIndexes);

        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Image not found in cache: {imagePath}");
            return null;
        }

        try
        {
            using var stream = File.OpenRead(imagePath);
            var bitmap = new Bitmap(stream);
            ImageCache[albumImageIndexes] = bitmap; // 将新加载的图片添加到缓存中
            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading image from cache: {imagePath}, Error: {ex.Message}");
            return null;
        }
    }
    private static string GetAlbumImagePath(string albumImageIndexes)
    {
        string cachePath = Path.Combine(Directory.GetCurrentDirectory(), "MusicCache", "AlbumImages");
        Directory.CreateDirectory(cachePath);
        return Path.Combine(cachePath, $"{albumImageIndexes}.png");
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.Hours > 0
            ?
            // 如果时间跨度大于一小时，则包括小时、分钟和秒（带小数）
            $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}.{duration.Milliseconds:D3}"
            :
            // 否则只包括分钟和秒（带小数）
            $"{duration.Minutes:D2}:{duration.Seconds:D2}.{duration.Milliseconds:D3}";
    }

    private static string FormatFileSize(long bytes)
    {
        const int scale = 1024;
        string[] orders = ["GB", "MB", "KB", "B"];
        double len = bytes;
        int order = orders.Length - 1;

        while (len >= scale && order > 0)
        {
            order--;
            len /= scale;
        }

        return $"{len:0.0}{orders[order]}";
    }
}



