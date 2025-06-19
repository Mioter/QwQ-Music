using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Services;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.ViewModels;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.Models;

public partial class MusicListModel : ObservableObject
{
    public MusicListModel(
        string name = "",
        string? description = null,
        string? latestPlayedMusic = null,
        string? coverPath = null,
        Bitmap? coverImage = null
    )
    {
        Name = name;
        Description = string.IsNullOrWhiteSpace(description) ? "暂无简介" : description;
        LatestPlayedMusic = latestPlayedMusic;
        CoverPath = coverPath;

        if (coverImage != null)
        {
            CoverImage = coverImage;
        }
        _coverCacheKey = $"歌单-{Name}";
    }

    public readonly string Id = $"歌单-{UniqueIdGenerator.GetNextId()}";

    private string _coverCacheKey;

    public string Name
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _coverCacheKey = $"歌单-{value}";
            }
        }
    }

    [ObservableProperty]
    public partial ObservableCollection<MusicItemModel> MusicItems { get; set; } = [];

    public int Count => MusicItems.Count;

    [ObservableProperty]
    public partial string Description { get; set; }

    // 添加一个标志表示图片是否正在加载
    private CoverStatus? _coverStatus;

    public Bitmap CoverImage
    {
        get
        {
            // 如果正在加载中，返回默认封面
            if (_coverStatus == CoverStatus.Loading)
                return MusicExtractor.DefaultCover;

            // 尝试从缓存获取图片
            if (MusicExtractor.ImageCache.TryGetValue(_coverCacheKey, out var image))
            {
                _coverStatus = CoverStatus.Loaded;
                return image!;
            }

            // 如果封面路径不存在，尝试从 MusicItems 中获取
            if (string.IsNullOrEmpty(CoverPath) || _coverStatus == CoverStatus.NotExist)
            {
                // 遍历 MusicItems 查找第一个有效的封面
                foreach (var musicItem in MusicItems)
                {
                    if (musicItem.CoverImage != MusicExtractor.DefaultCover)
                    {
                        return musicItem.CoverImage;
                    }
                }
                return MusicExtractor.DefaultCover;
            }

            // 缓存未命中，标记为正在加载
            _coverStatus = CoverStatus.Loading;

            // 启动异步加载任务
            Task.Run(async () =>
            {
                var bitmap = await MusicExtractor.LoadCompressedBitmap(CoverPath);

                if (bitmap != null)
                {
                    MusicExtractor.ImageCache[_coverCacheKey] = bitmap;
                    _coverStatus = CoverStatus.Loaded;
                }
                else
                {
                    string? firstPath = await GetFirstSongInPlaylist(Name);

                    var firstMusicCoverImage = MusicPlayerViewModel
                        .Instance.MusicItems.FirstOrDefault(x => x.FilePath == firstPath)
                        ?.CoverImage;

                    if (firstMusicCoverImage != null && firstMusicCoverImage != MusicExtractor.DefaultCover)
                    {
                        MusicExtractor.ImageCache[_coverCacheKey] = firstMusicCoverImage;
                        _coverStatus = CoverStatus.Loaded;
                    }
                    else
                    {
                        _coverStatus = CoverStatus.NotExist;
                    }
                }

                OnPropertyChanged(); // 通知 UI 更新
            });

            // 首次或加载中时返回默认封面
            return MusicExtractor.DefaultCover;
        }
        set
        {
            MusicExtractor.ImageCache[_coverCacheKey] = value;
            _coverStatus = CoverStatus.Loaded;

            OnPropertyChanged();
        }
    }

    public string? LatestPlayedMusic { get; set; }

    public string? CoverPath { get; set; }

    public bool IsInitialized { get; private set; }

    public bool IsError { get; private set; }

    /// <summary>
    /// 初始化音乐列表
    /// </summary>
    public async Task<MusicListModel?> LoadAsync(ObservableCollection<MusicItemModel> allMusicItems)
    {
        // 获取最近播放的音乐列表
        var latestPlayedMusicList = await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.LISTINFO,
            [nameof(LatestPlayedMusic)],
            dict => dict.TryGetValue(nameof(LatestPlayedMusic), out object? value) ? value?.ToString() ?? null : null,
            ..1,
            $"{nameof(Name)} = '{Name.Replace("'", "''")}'"
        );

        // 加载播放列表并获取文件路径列表
        var filePaths = await LoadMusicFilePathsAsync();

        // 根据文件路径从 全部 MusicItems 中查找对应项目
        var musicItems = filePaths
            .Select(filePath => allMusicItems.FirstOrDefault(item => filePath != null && item.FilePath == filePath))
            .OfType<MusicItemModel>();

        MusicItems = new ObservableCollection<MusicItemModel>(musicItems);

        // 设置最近播放的音乐
        if (latestPlayedMusicList is { Count: > 0 })
            LatestPlayedMusic = latestPlayedMusicList[0];

        IsInitialized = true;

        return this;
    }

    /// <summary>
    /// 加载当前歌单中的全部音乐文件路径
    /// </summary>
    /// <returns>音乐文件路径集合</returns>
    private async Task<List<string?>> LoadMusicFilePathsAsync()
    {
        // 创建一个列表来存储文件路径
        var filePaths = await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.MUSICLISTS,
            [nameof(MusicItemModel.FilePath)],
            dict =>
                dict.TryGetValue(nameof(MusicItemModel.FilePath), out object? value) ? value.ToString() ?? null : null,
            search: $"{nameof(Name)} = '{Name.Replace("'", "''")}'"
        );

        if (filePaths?.Count >= 0)
            return filePaths;

        NotificationService.ShowLight(
            new Notification("错误", $"获取歌单《{Name}》中的音乐文件路径失败！"),
            NotificationType.Error
        );
        IsError = true;
        return [];
    }

    public Dictionary<string, string?> Dump()
    {
        return new Dictionary<string, string?>
        {
            [nameof(Name)] = Name,
            [nameof(Description)] = Description,
            [nameof(CoverPath)] = CoverPath,
            [nameof(LatestPlayedMusic)] = LatestPlayedMusic,
        };
    }

    private static async Task<string?> GetFirstSongInPlaylist(string playlistName)
    {
        // 获取歌单中的第一首歌曲路径
        var filePaths = await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.MUSICLISTS,
            [nameof(MusicItemModel.FilePath)],
            dict => dict.TryGetValue(nameof(MusicItemModel.FilePath), out object? path) ? path.ToString() : null,
            search: $"{nameof(Name)} = '{playlistName.Replace("'", "''")}'"
        );

        if (filePaths?.Count > 0)
        {
            return filePaths.FirstOrDefault();
        }

        NotificationService.ShowLight(
            new Notification("错误", $"获取歌单《{playlistName}》第一首音乐失败！"),
            NotificationType.Error
        );
        return null;
    }
}
