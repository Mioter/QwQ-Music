using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using QwQ_Music.Models;
using File = System.IO.File;

namespace QwQ_Music.Services;

public static class MusicExtractor
{
    private static readonly Dictionary<string, Bitmap> ImageCache = new();

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new();

    private async static Task SaveAlbumImageAsync(Bitmap albumImage, string albumImageIndexes)
    {
        string imagePath = GetAlbumImagePath(albumImageIndexes);

        // 获取或创建每个专辑封面专用的信号量（相当于细粒度锁）
        var fileLock = FileLocks.GetOrAdd(albumImageIndexes, _ => new SemaphoreSlim(1, 1));

        await fileLock.WaitAsync();
        try
        {
            // 双重检查文件是否存在（在获得锁之后再次检查）
            if (File.Exists(imagePath))
            {
                Console.WriteLine($"Image already exists: {imagePath}. Skipping save.");
                return;
            }

            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);

            // 使用临时文件名写入，避免中途被其他进程读取不完整文件
            string? tempPath = Path.ChangeExtension(imagePath, ".tmp");
            await Task.Run(() =>
            {
                using var fs = File.Create(tempPath);
                albumImage.Save(fs);
            });

            // 原子操作重命名文件
            File.Move(tempPath, imagePath, true);
        }
        finally
        {
            fileLock.Release();
        }
    }

    public async static Task<MusicItemModel?> ExtractMusicInfoAsync(string filePath)
    {
        try
        {
            var file = TagLib.File.Create(filePath);
            var tag = file.Tag;
            var properties = file.Properties;

            long fileSizeBytes = new FileInfo(filePath).Length;
            string fileSize = FormatFileSize(fileSizeBytes);

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

            string samplingRate = properties.AudioSampleRate.ToString();
            string bitrate = properties.AudioBitrate.ToString();
            string encodingFormat = properties.Description; // 编码格式

            if (tag.Pictures.Length <= 0)
                return new MusicItemModel(title, singer, filePath, fileSize, duration, album, genre, null, "00:00", year, comment, composer, copyright, discNumber, trackNumber, samplingRate, bitrate, encodingFormat);

            using var ms = new MemoryStream(tag.Pictures[0].Data.Data);
            var albumImage = new Bitmap(ms);

            string albumImageIndexes = CleanFileName($"{singer}-{album}");

            await SaveAlbumImageAsync(albumImage, albumImageIndexes);

            return new MusicItemModel(title, singer, filePath, fileSize, duration, album, genre, albumImageIndexes, "00:00", year, comment, composer, copyright, discNumber, trackNumber, samplingRate, bitrate, encodingFormat);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading metadata from {filePath}: {ex.Message}");
            return null;
        }
    }

    private static FileStream? GetImageStream(string albumImageIndexes)
    {
        string imagePath = GetAlbumImagePath(albumImageIndexes);

        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Image not found: {imagePath}");
            return null;
        }

        try
        {
            return File.OpenRead(imagePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening image file: {imagePath}, Error: {ex.Message}");
            return null;
        }
    }

    public static Bitmap? CreateBitmapFromStream(Stream stream)
    {
        try
        {
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating bitmap from stream: Error: {ex.Message}");
            return null;
        }
    }

    public static Bitmap? LoadCompressedBitmapFromCache(string albumImageIndexes)
    {
        if (ImageCache.TryGetValue(albumImageIndexes, out var cachedImage))
        {
            Console.WriteLine($"Compressed image loaded from cache: {albumImageIndexes}");
            return cachedImage;
        }

        using var stream = GetImageStream(albumImageIndexes);
        if (stream == null)
        {
            return null;
        }

        try
        {
            // 解码并缩放图片
            var bitmap = Bitmap.DecodeToWidth(stream, 256);

            // 更新缓存
            ImageCache[albumImageIndexes] = bitmap;

            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading compressed image from cache: {albumImageIndexes}, Error: {ex.Message}");
            return null;
        }
    }

    private static string CleanFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return invalidChars.Aggregate(fileName, (current, c) => current.Replace(c, '&'));
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
