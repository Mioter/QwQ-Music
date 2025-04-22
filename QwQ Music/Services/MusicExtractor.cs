using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ATL;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using QwQ_Music.Models;
using QwQ_Music.Utilities;
using File = System.IO.File;
using PlayerConfig = QwQ_Music.Models.ConfigModel.PlayerConfig;

namespace QwQ_Music.Services;

public static class MusicExtractor
{
    private const int DefaultThumbnailWidth = 128; // 默认缩略图宽度，降低了原来的256

    public static readonly Dictionary<string, Bitmap> ImageCache = [];

    public static readonly Bitmap DefaultCover = GetDefaultCover();

    /// <summary>
    /// 异步保存专辑封面图片。
    /// </summary>
    /// <param name="cover">专辑封面图片。</param>
    /// <param name="coverFileName">专辑封面文件名 (不含路径)。</param> // 修改注释
    public static async Task<bool> SaveCoverAsync(Bitmap cover, string coverFileName)
    {
        // 构造完整文件路径
        string filePath = Path.Combine(PlayerConfig.CoverSavePath, coverFileName); // 在这里拼接路径

        // 确保目录存在
        // 注意：如果 CoverSavePath 本身就不存在，CreateDirectory 需要能创建多级目录
        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        else
        {
            // 如果无法获取目录路径（例如 coverFileName 是根路径下的文件名），记录错误或采取其他措施
            await LoggerService.ErrorAsync($"Invalid cover file path generated: {filePath}");
            return false;
        }


        // 检查文件是否存在
        if (File.Exists(filePath))
        {
            /*LoggerService.Info($"Cover already exists: {filePath}. Skipping save.");*/
            return true;
        }

        try
        {
            // 将文件流的创建和保存操作移至 Task.Run 内部
            await Task.Run(() =>
            {
                // 在后台线程中创建、使用和释放文件流
                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                cover.Save(fs); // Bitmap.Save 是同步操作
            }).ConfigureAwait(false); // 继续使用 ConfigureAwait(false)

            return true;
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"Failed to save cover {filePath}: {ex.Message}, TypeName: {ex.GetType().Name}");
            return false;
        }
    }

    public static async Task<LyricsData> ExtractMusicLyricsAsync(string? filePath)
    {
        var lyricsData = new LyricsData();
        var track = new Track(filePath);
        string? lyric = track.Lyricist;
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
    /// 异步获取扩展信息
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <returns>包含扩展信息的字典。</returns>
    public static async Task<MusicTagExtensions> ExtractExtensionsInfoAsync(string? filePath)
    {
        return await Task.Run(() =>
        {
            var track = new Track(filePath);
            return new MusicTagExtensions(
                track.Genre ?? string.Empty,
                track.Year,
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
                track.AudioFormat.DataFormat.Name,
                track.Encoder ?? string.Empty
            );
        });
    }

    /// <summary>
    /// 异步获取详细信息
    /// </summary>
    /// <param name="filePath">音频文件路径。</param>
    /// <returns>包含详细信息的字典。</returns>
    public static async Task<MusicDetailedInfo> ExtractDetailedInfoAsync(string filePath)
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
    public static async Task<Dictionary<string, string>> ExtractAdditionalFieldsAsync(string filePath)
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
    public static async Task<MusicItemModel?> ExtractMusicInfoAsync(string filePath)
    {
        try
        {
            var track = new Track(filePath);
            long fileSizeBytes = new FileInfo(filePath).Length;
            string fileSize = FormatFileSize(fileSizeBytes);
            string title = string.IsNullOrWhiteSpace(track.Title)
                ? Path.GetFileNameWithoutExtension(filePath)
                : track.Title;
            string composer = track.Composer;
            string artists = track.Artist;
            string album = track.Album;
            var duration = TimeSpan.FromMilliseconds(track.DurationMs);
            string comment = track.Comment;
            string encodingFormat = track.AudioFormat.ShortName;

            string? coverFileName = null; // 存储文件名，而非完整路径

            if (track.EmbeddedPictures.Count > 0)
            {
                // 获取清理后的文件名
                coverFileName = GetCoverFileName(artists, album);
                // 异步保存封面，传递文件名
                await SaveCoverAsync(
                    new Bitmap(new MemoryStream(track.EmbeddedPictures[0].PictureData)),
                    coverFileName
                );
            }

            return new MusicItemModel(
                title,
                artists,
                composer,
                album,
                coverFileName, // 传递文件名
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
            await LoggerService.ErrorAsync($"Error reading metadata from {filePath}: {ex.Message}");
            return null;
        }
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
        string coverFileName = PathEnsurer.CleanFileName(
            $"{(artists.Length > 20 ? artists[..20] : artists)}-{(album.Length > 20 ? album[..20] : album)}.jpg"
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
        string fullFilePath = PathEnsurer.EnsureFullPath(filePath, PlayerConfig.CoverSavePath);

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
            return stream == null ? null : Bitmap.DecodeToWidth(stream, DefaultThumbnailWidth); // 解码并缩放图片，使用较小的宽度
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

    public static Bitmap GetDefaultCover() =>
        new(AssetLoader.Open(new Uri("avares://QwQ Music/Assets/Images/看我.png")));

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
    /// 从音频文件中提取专辑封面并保存
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <returns>保存后的封面文件名，如果没有封面则返回null</returns> // 返回文件名
    public static async Task<string?> ExtractAndSaveCoverFromAudioAsync(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return null; // 添加空检查

        try
        {
            var track = new Track(filePath);

            if (track.EmbeddedPictures.Count > 0)
            {
                string artists = track.Artist;
                string album = track.Album;
                // 获取清理后的文件名
                string coverFileName = GetCoverFileName(artists, album);

                // 异步保存封面，传递文件名
                bool saved = await SaveCoverAsync(
                    new Bitmap(new MemoryStream(track.EmbeddedPictures[0].PictureData)),
                    coverFileName
                );

                return saved ? coverFileName : null; // 返回文件名或null
            }

            return null;
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

