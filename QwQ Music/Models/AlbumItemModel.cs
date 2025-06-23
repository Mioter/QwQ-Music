using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Services;

namespace QwQ_Music.Models;

public class AlbumItemModel(string title, string artist, string? coverPath) : ObservableObject
{
    public string Title { get; set; } = title;

    public string Artist { get; set; } = artist;

    // 添加一个标志表示图片是否正在加载
    private CoverStatus? _coverStatus;

    public Bitmap CoverImage
    {
        get
        {
            // 如果封面路径不存在，返回默认封面
            if (string.IsNullOrEmpty(coverPath) || _coverStatus == CoverStatus.NotExist)
                return MusicExtractor.DefaultCover;

            // 如果正在加载中，返回默认封面
            if (_coverStatus == CoverStatus.Loading)
                return MusicExtractor.DefaultCover;

            // 尝试从缓存获取图片
            if (MusicExtractor.ImageCache.TryGetValue(coverPath, out var image))
            {
                _coverStatus = CoverStatus.Loaded;
                return image!;
            }

            // 缓存未命中，标记为正在加载
            _coverStatus = CoverStatus.Loading;

            // 启动异步加载任务
            Task.Run(async () =>
            {
                var bitmap = await MusicExtractor.LoadCompressedBitmap(coverPath);

                if (bitmap != null)
                {
                    MusicExtractor.ImageCache[coverPath] = bitmap;
                    _coverStatus = CoverStatus.Loaded;
                }
                else
                {
                    _coverStatus = CoverStatus.NotExist;
                }

                OnPropertyChanged(); // 通知 UI 更新
            });

            // 首次或加载中时返回默认封面
            return MusicExtractor.DefaultCover;
        }
        set
        {
            if (coverPath == null)
                return;

            MusicExtractor.ImageCache[coverPath] = value;
            _coverStatus = CoverStatus.Loaded;

            OnPropertyChanged();
        }
    }
}
