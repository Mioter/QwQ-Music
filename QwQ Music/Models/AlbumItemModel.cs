using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.Models;

public partial class AlbumItemModel(string name, string artist, string? coverFileName = null) : ObservableObject
{
    // 添加一个标志表示图片是否正在加载
    private LoadingState? _coverStatus;

    public string Name { get; } = name;

    public string Artist { get; } = artist;

    [ObservableProperty] public partial string? Description { get; set; }

    [ObservableProperty] public partial string? PublishTime { get; set; }

    [ObservableProperty] public partial string? Company { get; set; }

    public Bitmap CoverImage
    {
        get
        {
            // 如果封面路径不存在，返回默认封面
            if (string.IsNullOrEmpty(coverFileName) || _coverStatus == LoadingState.NotExist)
                return CacheManager.Default;

            // 如果正在加载中，返回默认封面
            if (_coverStatus == LoadingState.Loading)
                return CacheManager.Loading;

            // 尝试从缓存获取图片
            if (CacheManager.ImageCache.TryGetValue(coverFileName, out var image))
            {
                _coverStatus = LoadingState.Loaded;

                return image ?? CacheManager.Default;
            }

            // 缓存未命中，标记为正在加载
            _coverStatus = LoadingState.Loading;

            // 启动异步加载任务
            Task.Run(async () =>
            {
                var bitmap = await MusicExtractor.LoadBitmapFromFileAsync(MusicExtractor.GetMusicCoverFullPath(coverFileName));

                if (bitmap != null)
                {
                    CacheManager.ImageCache[coverFileName] = bitmap;
                    _coverStatus = LoadingState.Loaded;
                }
                else
                {
                    _coverStatus = LoadingState.NotExist;
                }

                OnPropertyChanged(); // 通知 UI 更新
            });

            // 首次或加载中时返回默认封面
            return CacheManager.Default;
        }
        set
        {
            if (coverFileName == null)
                return;

            CacheManager.ImageCache[coverFileName] = value;
            _coverStatus = LoadingState.Loaded;

            OnPropertyChanged();
        }
    }
}
