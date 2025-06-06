using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models.ModelBases;
using QwQ_Music.Services;

namespace QwQ_Music.Models;

public class MusicItemModel : ObservableObject, IModelBase<MusicItemModel>
{
    public MusicItemModel(
        string title = "",
        string? artists = null,
        string? composer = null,
        string? album = null,
        string? coverPath = null,
        string filePath = "",
        string fileSize = "",
        TimeSpan? current = null,
        TimeSpan duration = default,
        string? encodingFormat = null,
        string? comment = null,
        Bitmap? coverImage = null,
        double gain = -1.0f,
        string[]? coverColor = null
    )
    {
        Title = string.IsNullOrWhiteSpace(title) ? "未知标题" : title;
        Artists = string.IsNullOrWhiteSpace(artists) ? "未知歌手" : artists;
        Composer = string.IsNullOrWhiteSpace(composer) ? "未知作曲" : composer;
        Album = string.IsNullOrWhiteSpace(album) ? "未知专辑" : album;
        Current = current ?? TimeSpan.Zero;
        Duration = duration;
        FilePath = filePath;
        FileSize = fileSize;
        Gain = gain;
        EncodingFormat = encodingFormat;
        CoverPath = coverPath;
        CoverColors = coverColor;
        Comment = comment;

        if (coverImage != null)
        {
            CoverImage = coverImage;
        }
    }

    public bool IsInitialized { get; private init; }

    public bool IsLoading { get; set; }

    public bool IsError { get; private set; }

    public bool IsModified { get; set; } = true;

    public string Title
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    public string Artists
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    public string Composer
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    public string Album
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    public TimeSpan Current
    {
        get;
        set => SetPropertyWithModified(ref field, value, true);
    }

    public TimeSpan Duration
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    public string FilePath
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    public string FileSize
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    public double Gain
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    public string? EncodingFormat
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    public string? CoverPath
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    } // 初始值来自构造函数

    public string[]? CoverColors
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    public string? Comment
    {
        get;
        set => SetPropertyWithModified(ref field, value);
    }

    // 添加一个标志表示图片是否正在加载
    private CoverStatus? _coverStatus;

    public Bitmap CoverImage
    {
        get
        {
            // 如果封面路径不存在，返回默认封面
            if (string.IsNullOrEmpty(CoverPath) || _coverStatus == CoverStatus.NotExist)
                return MusicExtractor.DefaultCover;

            // 如果正在加载中，返回默认封面
            if (_coverStatus == CoverStatus.Loading)
                return MusicExtractor.DefaultCover;

            // 尝试从缓存获取图片
            if (MusicExtractor.ImageCache.TryGetValue(CoverPath, out var image))
            {
                _coverStatus = CoverStatus.Loaded;
                return image;
            }

            // 缓存未命中，标记为正在加载
            _coverStatus = CoverStatus.Loading;

            // 启动异步加载任务
            Task.Run(async () =>
            {
                var bitmap = await MusicExtractor.LoadCompressedBitmap(CoverPath);

                if (bitmap != null)
                {
                    MusicExtractor.ImageCache[CoverPath] = bitmap;
                    _coverStatus = CoverStatus.Loaded;
                }
                else
                {
                    string? newCoverPath = await MusicExtractor.ExtractAndSaveCoverFromAudioAsync(FilePath);

                    if (newCoverPath != null)
                        CoverPath = newCoverPath;
                    else
                        _coverStatus = CoverStatus.NotExist;
                }

                OnPropertyChanged(); // 通知 UI 更新
            });

            // 首次或加载中时返回默认封面
            return MusicExtractor.DefaultCover;
        }
        set
        {
            if (CoverPath == null)
                return;

            MusicExtractor.ImageCache[CoverPath] = value;
            _coverStatus = CoverStatus.Loaded;
        }
    }

    public string? Remarks
    {
        get;
        set => SetPropertyWithModified(ref field, value, true);
    }

    // 通用的设置属性并标记修改的方法
    private bool SetPropertyWithModified<T>(
        ref T field,
        T value,
        bool isNotify = false,
        [CallerMemberName] string? propertyName = null
    )
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;

        if (isNotify)
            OnPropertyChanged(propertyName);

        if (!IsModified && !IsLoading && propertyName != nameof(IsModified))
            IsModified = true;

        return true;
    }

    public async Task<MusicTagExtensions> GetExtensionsInfo() =>
        await MusicExtractor.ExtractExtensionsInfoAsync(FilePath);

    public Task<LyricsData> Lyrics => MusicExtractor.ExtractMusicLyricsAsync(FilePath);

    public static MusicItemModel FromDictionary(Dictionary<string, object> data)
    {
        MusicItemModel result = new()
        {
            IsInitialized = true,
            IsError = false,
            IsLoading = true,
        };

        // 使用辅助方法安全地提取属性
        SafeExtract(data, nameof(Title), val => result.Title = val?.ToString() ?? "未知标题");
        SafeExtract(data, nameof(Artists), val => result.Artists = val?.ToString() ?? "未知歌手");
        SafeExtract(data, nameof(Album), val => result.Album = val?.ToString() ?? "未知专辑");
        SafeExtract(data, nameof(Composer), val => result.Composer = val?.ToString() ?? "未知作曲");
        SafeExtract(data, nameof(CoverPath), val => result.CoverPath = val?.ToString()); // CoverPath 现在是文件名或null
        SafeExtract(
            data,
            nameof(Current),
            val =>
                result.Current =
                    val != null
                        ? TimeSpan.Parse(val.ToString() ?? "00:00:00", CultureInfo.InvariantCulture)
                        : TimeSpan.Zero
        );
        SafeExtract(
            data,
            nameof(Duration),
            val =>
                result.Duration =
                    val != null
                        ? TimeSpan.Parse(val.ToString() ?? "00:00:00", CultureInfo.InvariantCulture)
                        : TimeSpan.Zero
        );
        SafeExtract(data, nameof(FilePath), val => result.FilePath = val?.ToString() ?? "");
        SafeExtract(data, nameof(FileSize), val => result.FileSize = val?.ToString() ?? "");
        SafeExtract(data, nameof(Gain), val => result.Gain = val != null ? Convert.ToDouble(val) : -1.0);
        SafeExtract(data, nameof(CoverColors), val => result.CoverColors = val?.ToString()?.Split("、"));
        SafeExtract(data, nameof(Comment), val => result.Comment = val?.ToString());
        SafeExtract(data, nameof(EncodingFormat), val => result.EncodingFormat = val?.ToString());
        SafeExtract(data, nameof(Remarks), val => result.Remarks = val?.ToString());

        result.IsLoading = false;

        result.IsModified = false; // 使IsModified为false

        return result;

        // 辅助方法：安全地从字典中提取值并应用转换
        void SafeExtract(Dictionary<string, object> converter, string key, Action<object?> setter)
        {
            try
            {
                if (converter.TryGetValue(key, out object? value))
                {
                    setter(value);
                }
            }
            catch (Exception)
            {
                result.IsError = true;
            }
        }
    }

    public Dictionary<string, string?> Dump()
    {
        var result = new Dictionary<string, string?>
        {
            [nameof(Title)] = Title,
            [nameof(Artists)] = Artists,
            [nameof(Album)] = Album,
            [nameof(Composer)] = Composer,
            [nameof(CoverPath)] = CoverPath, // 保存文件名或null
            [nameof(Current)] = Current.ToString(),
            [nameof(Duration)] = Duration.ToString(),
            [nameof(FilePath)] = FilePath,
            [nameof(FileSize)] = FileSize,
            [nameof(Gain)] = Gain.ToString(CultureInfo.InvariantCulture),
            [nameof(CoverColors)] = CoverColors != null ? string.Join("、", CoverColors) : null,
            [nameof(Comment)] = Comment,
            [nameof(EncodingFormat)] = EncodingFormat,
            [nameof(Remarks)] = Remarks,
        };
        return result;
    }
}

public readonly record struct MusicTagExtensions(
    string Genre,
    int? Year,
    string Copyright,
    uint Disc,
    uint Track,
    int SamplingRate,
    int Channels,
    int Bitrate,
    int BitsPerSample,
    // 添加更多基本信息
    string OriginalAlbum,
    string OriginalArtist,
    string AlbumArtist,
    string Publisher,
    string Description,
    string Language,
    // 添加技术信息
    bool IsVbr,
    string AudioFormat,
    string EncoderInfo
);

// 添加扩展结构体用于获取更多详细信息
public readonly record struct MusicDetailedInfo(
    // 发布信息
    DateTime? ReleaseDate,
    DateTime? OriginalReleaseDate,
    DateTime? PublishingDate,
    // 专业信息
    string Isrc,
    string CatalogNumber,
    string ProductId,
    // 其他信息
    float? Bpm,
    float? Popularity,
    string SeriesTitle,
    string SeriesPart,
    string LongDescription,
    string Group,
    // 技术信息
    long AudioDataOffset,
    long AudioDataSize
);

internal enum CoverStatus
{
    Loading,
    Loaded,
    NotExist,
}
