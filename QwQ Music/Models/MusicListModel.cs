using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Services;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.ViewModels;

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
    }

    private string? _coverCacheKey;

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
            // 如果已有缓存图片，直接返回
            if (MusicExtractor.ImageCache.TryGetValue(_coverCacheKey ??= $"歌单-{Name}", out var cachedImage))
            {
                return cachedImage;
            }

            // 如果正在加载中，暂时返回默认封面
            if (_coverStatus == CoverStatus.Loading)
            {
                return MusicExtractor.DefaultCover;
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

            // 标记为正在加载
            _coverStatus = CoverStatus.Loading;

            // 启动异步加载任务
            Task.Run(async () =>
            {
                var bitmap = await MusicExtractor.LoadCompressedBitmap(CoverPath);

                if (bitmap != null)
                {
                    MusicExtractor.ImageCache[_coverCacheKey ??= $"歌单-{Name}"] = bitmap;
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
                        MusicExtractor.ImageCache[_coverCacheKey ??= $"歌单-{Name}"] = firstMusicCoverImage;
                        _coverStatus = CoverStatus.Loaded;
                    }

                    _coverStatus = CoverStatus.NotExist;
                }

                OnPropertyChanged(); // 通知 UI 更新
            });

            // 首次或加载中时返回默认封面
            return MusicExtractor.DefaultCover;
        }
        set
        {
            MusicExtractor.ImageCache[_coverCacheKey ??= $"歌单-{Name}"] = value;
            _coverStatus = CoverStatus.Loaded;
        }
    }

    public string? LatestPlayedMusic { get; set; }

    public string? CoverPath { get; set; }

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 初始化音乐列表
    /// </summary>
    public async Task LoadAsync()
    {
        // 获取最近播放的音乐列表
        var latestPlayedMusicList = await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.LISTINFO,
            [nameof(LatestPlayedMusic)],
            dict => dict.TryGetValue(nameof(LatestPlayedMusic), out object? value) ? value.ToString() ?? null : null,
            ..1,
            $"{nameof(Name)} = '{Name.Replace("'", "''")}'"
        );

        MusicItems = new ObservableCollection<MusicItemModel>(await LoadMusicItemsAsync());

        // 设置最近播放的音乐
        if (latestPlayedMusicList.Count > 0)
            LatestPlayedMusic = latestPlayedMusicList[0];

        IsInitialized = true;
    }

    /// <summary>
    /// 加载当前歌单音乐项
    /// </summary>
    /// <returns>音乐项集合</returns>
    private async Task<IEnumerable<MusicItemModel>> LoadMusicItemsAsync()
    {
        // 加载播放列表并获取文件路径列表
        var filePaths = await LoadMusicFilePathsAsync();

        // 根据文件路径从 全部 MusicItems 中查找对应项目
        return filePaths
            .Select(filePath =>
                MusicPlayerViewModel.Instance.MusicItems.FirstOrDefault(item =>
                    filePath != null && item.FilePath == filePath
                )
            )
            .OfType<MusicItemModel>();
    }

    /// <summary>
    /// 加载当前歌单中的全部音乐文件路径
    /// </summary>
    /// <returns>音乐文件路径集合</returns>
    private async Task<List<string?>> LoadMusicFilePathsAsync()
    {
        // 创建一个列表来存储文件路径
        return await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.MUSICLISTS,
            [nameof(MusicItemModel.FilePath)],
            dict =>
                dict.TryGetValue(nameof(MusicItemModel.FilePath), out object? value) ? value.ToString() ?? null : null,
            search: $"{nameof(Name)} = '{Name.Replace("'", "''")}'"
        );
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
        try
        {
            // 获取歌单中的第一首歌曲路径
            var filePaths = await DataBaseService.LoadSpecifyFieldsAsync(
                DataBaseService.Table.MUSICLISTS,
                [nameof(MusicItemModel.FilePath)],
                dict => dict.TryGetValue(nameof(MusicItemModel.FilePath), out object? path) ? path.ToString() : null,
                search: $"{nameof(Name)} = '{playlistName.Replace("'", "''")}'"
            );

            return filePaths.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"获取歌单第一首歌曲失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 将当前对象的属性拷贝到目标对象
    /// </summary>
    /// <param name="target">目标对象</param>
    public void CopyTo(MusicListModel target)
    {
        target.Name = Name;
        target.Description = Description;
        target.CoverPath = CoverPath;
        target.LatestPlayedMusic = LatestPlayedMusic;
        target.IsInitialized = IsInitialized;

        // 深拷贝 MusicItems 集合
        target.MusicItems.Clear();
        foreach (var item in MusicItems)
        {
            target.MusicItems.Add(item);
        }

        // 如果当前对象有封面图片，则拷贝到目标对象
        if (CoverPath != null && MusicExtractor.ImageCache.TryGetValue(CoverPath, out var coverImage))
        {
            target.CoverImage = coverImage;
        }
    }

    /// <summary>
    /// 从源对象拷贝属性到当前对象
    /// </summary>
    /// <param name="source">源对象</param>
    public void CopyFrom(MusicListModel source)
    {
        Name = source.Name;
        Description = source.Description;
        CoverPath = source.CoverPath;
        LatestPlayedMusic = source.LatestPlayedMusic;
        IsInitialized = source.IsInitialized;

        // 深拷贝 MusicItems 集合
        MusicItems.Clear();
        foreach (var item in source.MusicItems)
        {
            MusicItems.Add(item);
        }

        // 如果源对象有封面图片，则拷贝到当前对象
        if (source.CoverPath != null && MusicExtractor.ImageCache.TryGetValue(source.CoverPath, out var coverImage))
        {
            CoverImage = coverImage;
        }
    }

    /// <summary>
    /// 创建当前对象的深拷贝
    /// </summary>
    /// <returns>新的 MusicListModel 实例</returns>
    public MusicListModel Clone()
    {
        var clone = new MusicListModel();
        CopyTo(clone);
        return clone;
    }
}
