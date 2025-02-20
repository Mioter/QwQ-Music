using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using QwQ_Music.Models;

namespace QwQ_Music.Services;

public static class MusicExtractor
{
    private static readonly Dictionary<string, Bitmap> ImageCache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> FileLocks = new();

    /// <summary>
    /// 异步保存专辑封面图片。
    /// </summary>
    /// <param name="albumImage">专辑封面图片。</param>
    /// <param name="albumImageIndexes">专辑封面索引。</param>
    private async static Task SaveAlbumImageAsync(Bitmap albumImage, string albumImageIndexes)
    {
        string imagePath = GetAlbumImagePath(albumImageIndexes);

        // 获取或创建每个专辑封面专用的信号量（细粒度锁）
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
            string tempPath = Path.ChangeExtension(imagePath, ".tmp");

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
            {
                return new MusicItemModel(
                    title, singer, filePath, fileSize, duration, album, genre, null, "00:00",
                    year, comment, composer, copyright, discNumber, trackNumber, samplingRate, bitrate, encodingFormat);
            }

            using var ms = new MemoryStream(tag.Pictures[0].Data.Data);
            var albumImage = new Bitmap(ms);
            string albumImageIndexes = CleanFileName($"{singer}-{album}");

            await SaveAlbumImageAsync(albumImage, albumImageIndexes);

            return new MusicItemModel(
                title, singer, filePath, fileSize, duration, album, genre, albumImageIndexes, "00:00",
                year, comment, composer, copyright, discNumber, trackNumber, samplingRate, bitrate, encodingFormat);
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
    /// <param name="albumImageIndexes">专辑封面索引。</param>
    /// <returns>压缩后的位图。</returns>
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

    /// <summary>
    /// 获取专辑封面图片的文件流。
    /// </summary>
    /// <param name="albumImageIndexes">专辑封面索引。</param>
    /// <returns>文件流。</returns>
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

    /// <summary>
    /// 格式化文件大小为人类可读的形式。
    /// </summary>
    /// <param name="bytes">文件大小（字节）。</param>
    /// <returns>格式化后的文件大小。</returns>
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

    /// <summary>
    /// 格式化持续时间为字符串。
    /// </summary>
    /// <param name="duration">持续时间。</param>
    /// <returns>格式化后的时间字符串。</returns>
    private static string FormatDuration(TimeSpan duration)
    {
        return duration.Hours > 0
            ? $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}.{duration.Milliseconds:D3}"
            : $"{duration.Minutes:D2}:{duration.Seconds:D2}.{duration.Milliseconds:D3}";
    }

    /// <summary>
    /// 清理文件名中的非法字符。
    /// </summary>
    /// <param name="fileName">原始文件名。</param>
    /// <returns>清理后的文件名。</returns>
    private static string CleanFileName(string fileName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        return invalidChars.Aggregate(fileName, (current, c) => current.Replace(c, '&'));
    }

    /// <summary>
    /// 获取专辑封面图片的存储路径。
    /// </summary>
    /// <param name="albumImageIndexes">专辑封面索引。</param>
    /// <returns>专辑封面图片的路径。</returns>
    private static string GetAlbumImagePath(string albumImageIndexes)
    {
        string cachePath = Path.Combine(Directory.GetCurrentDirectory(), "MusicCache", "AlbumImages");
        Directory.CreateDirectory(cachePath);
        return Path.Combine(cachePath, $"{albumImageIndexes}.png");
    }
}
