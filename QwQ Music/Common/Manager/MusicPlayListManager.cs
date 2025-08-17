using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.Common.Manager;

public class MusicPlayListManager : ObservableObject
{
    public static readonly MusicItemModel DefaultMusicItem = new()
    {
        Title = "听你想听",
        Artists = "YOU",
        FilePath = string.Empty,
    };


    public MusicItemModel CurrentMusicItem { get; set; } = DefaultMusicItem;

    public int CurrentIndex => PlayList.IndexOf(CurrentMusicItem);

    public static MusicPlayListManager Default { get; } = new();

    public AvaloniaList<MusicItemModel> PlayList { get; } = [];

    public int Count => PlayList.Count;

    public async Task Initialize()
    {
        try
        {
            using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);

            var paths = await Task.Run(() => playlistRepository.GetAll());

            // 创建路径到索引的映射
            var pathIndexMap = paths.Select((path, index) => new
                {
                    path,
                    index,
                })
                .ToDictionary(x => x.path, x => x.index);

            var orderedItems = MusicItemManager.Default.MusicItems
                .Where(item => paths.Contains(item.FilePath))
                .OrderBy(item => pathIndexMap[item.FilePath])
                .ToList();

            PlayList.AddRange(orderedItems);
        }
        catch (Exception e)
        {
            NotificationService.Error($"加载播放列表时发生错误！\n{e.Message}");
            await LoggerService.ErrorAsync($"加载播放列表时发生错误！\n{e.Message}\n{e.StackTrace}");
        }
    }

    public MusicItemModel First()
    {
        return PlayList.First();
    }

    public void Add(MusicItemModel musicItem)
    {
        PlayList.Add(musicItem);
        using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);
        playlistRepository.Add(musicItem.FilePath);
    }

    public void AddRange(IList<MusicItemModel> musicItems)
    {
        PlayList.AddRange(musicItems);
        using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);

        var files = musicItems.Select(item => item.FilePath);
        playlistRepository.AddRange(files);
    }

    public void AddToNext(IList<MusicItemModel> musicItems)
    {
        PlayList.RemoveAll(musicItems);

        using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);

        // 从后往前插入，这样可以保持原有顺序
        for (int i = musicItems.Count - 1; i >= 0; i--)
        {
            PlayList.Insert(CurrentIndex + 1, musicItems[i]);
            playlistRepository.Insert(musicItems[i].FilePath, CurrentIndex + 1);
        }

        PlayedIndicesService.ClearPlayedIndices();
    }

    public void Remove(IList<MusicItemModel> musicItems)
    {
        PlayList.RemoveAll(musicItems);

        using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);

        var files = musicItems.Select(item => item.FilePath);
        playlistRepository.Remove(files);

        PlayedIndicesService.ClearPlayedIndices();
    }

    public void Clear()
    {
        PlayList.Clear();

        using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);
        playlistRepository.Clear();
    }

    public void Toggle(
        IList<MusicItemModel> musicItems
        )
    {
        PlayList.Clear();
        PlayList.AddRange(musicItems);

        // 处于随机播放模式，且不为真随机时，在播放列表切换后打乱一次
        if (ConfigManager.PlayerConfig is { PlayMode: PlayMode.Random, IsRealRandom: false })
        {
            Shuffle();
            NotificationService.Info("当前启用了打乱播放列表模式的随机模式，音乐播放列表已经被打乱，请注意哦~");
        }

        PlayedIndicesService.ClearPlayedIndices();

        using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);
        playlistRepository.SetAll(PlayList.Select(item => item.FilePath));
    }

    public void Shuffle()
    {
        if (PlayList.Count <= 1)
            return;

        var random = new Random();

        // Fisher-Yates 洗牌算法
        for (int i = PlayList.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            (PlayList[i], PlayList[j]) = (PlayList[j], PlayList[i]);
        }

        // 如果当前有播放的歌曲，确保它在列表的当前位置
        if (!PlayList.Contains(CurrentMusicItem))
            return;

        int currentIndex = CurrentIndex;
        PlayList.Remove(CurrentMusicItem);
        PlayList.Insert(currentIndex, CurrentMusicItem);

        using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);
        playlistRepository.SetAll(PlayList.Select(item => item.FilePath));
    }
}
