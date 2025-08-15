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

public partial class MusicListModel : ObservableObject
{
    // 添加一个标志表示图片是否正在加载
    private LoadingState? _loadingState;

    public required string IdStr { get; set; }

    [ObservableProperty] public partial string Name { get; set; } = "未命名歌单";

    [ObservableProperty] public partial string Description { get; set; } = "暂无简介";

    public string? CoverId { get; set; }

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
                var dbBitmap = await MusicExtractor.LoadCompressedBitmapFromFileAsync(
                    MusicExtractor.GetMusicListCoverFullPath(CoverId));

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
                CoverId ??= Guid.NewGuid().ToString();
                CacheManager.SetImage(CoverId, value);

                Task.Run(async () =>
                {
                    await FileOperationService.SaveImageAsync(value, Path.Combine(StaticConfig.MusicListCoverSavePath, CoverId), true);
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
}
