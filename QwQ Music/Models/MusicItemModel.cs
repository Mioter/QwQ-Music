using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.Models;

public partial class MusicItemModel : ObservableObject
{
    [ObservableProperty] public partial string Title { get; set; } = "未知标题";

    [ObservableProperty] public partial string Artists { get; set; } = "未知歌手";

    [ObservableProperty] public partial string? Composer { get; set; }

    [ObservableProperty] public partial string Album { get; set; } = "未知专辑";

    [ObservableProperty] public partial TimeSpan Current { get; set; }

    public TimeSpan Duration { get; set; }

    public required string FilePath { get; set; }

    public string? FileSize { get; set; }

    [ObservableProperty] public partial double Gain { get; set; }

    public string? EncodingFormat { get; set; }

    public string? CoverId { get; set; }

    public string[]? CoverColors { get; set; }

    public string? Comment { get; set; }
    
    // 添加一个标志表示图片是否正在加载
    private LoadingState? _loadingState;

    public Bitmap? CoverImage
    {
        get
        {
            // 如果封面路径不存在，返回不存在封面
            if (string.IsNullOrEmpty(CoverId) || _loadingState == LoadingState.NotExist)
                return CacheManager.NotExist;

            // 如果正在加载中，返回加载中封面
            if (_loadingState == LoadingState.Loading)
                return CacheManager.Loading;

            // 尝试从缓存获取图片
            if (CacheManager.ImageCache.TryGetValue(CoverId, out var bitmap) && bitmap != null)
            {
                _loadingState = LoadingState.Loaded;

                return bitmap;
            }

            // 缓存未命中，标记为正在加载
            _loadingState = LoadingState.Loading;

            // 启动异步加载任务
            Task.Run(async () =>
            {
                var dbBitmap = await MusicExtractor.LoadBitmapFromFileAsync(
                    MusicExtractor.GetMusicCoverFullPath(CoverId));

                if (dbBitmap == null)
                {
                    _loadingState = LoadingState.NotExist;
                    OnPropertyChanged();

                    return;
                }

                CacheManager.ImageCache.Add(CoverId, dbBitmap);
                _loadingState = LoadingState.Loaded;
                OnPropertyChanged(); // 通知 UI 更新
            });

            // 首次或加载中时返回加载中封面
            return CacheManager.Loading;
        }
        set
        {
            if (value != null)
            {
                Task.Run(async () =>
                {
                    CoverId = MusicExtractor.PrepareCoverInfo(Artists, Album, FilePath);
                    CacheManager.SetImage(CoverId, value);

                    await FileOperationService.SaveImageAsync(value, Path.Combine(StaticConfig.MusicCoverSavePath, CoverId));

                    OnPropertyChanged();
                });
            }
            else
            {
                if (CoverId == null)
                    return;

                Task.Run(() => CacheManager.DeleteImage(CoverId));
                CoverId = null;
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty] public partial string? Remarks { get; set; }

    public int LyricOffset { get; set; }
    
    public DateTime InsertTime { get; set; }
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
