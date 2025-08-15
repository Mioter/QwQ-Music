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

namespace QwQ_Music.Common.Manager;

public class MusicPlayListManager : ObservableObject
{
    public readonly MusicItemModel DefaultMusicItem = new()
    {
        Title = "听你想听",
        Artists = "YOU",
        FilePath = "",
    };

    public MusicPlayListManager()
    {
        CurrentMusicItem = DefaultMusicItem;
        Initialize();
    }

    public MusicItemModel CurrentMusicItem { get; set; }

    public int CurrentIndex => PlayList.IndexOf(CurrentMusicItem);

    public static MusicPlayListManager Default { get; } = new();

    public AvaloniaList<MusicItemModel> PlayList { get; } = [];

    public int Count => PlayList.Count;

    public async void Initialize()
    {
        try
        {
            await using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);

            var paths = await playlistRepository.GetAllAsync();

            // 创建路径到索引的映射
            var pathIndexMap = paths.Select((path, index) => new { path, index })
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

    public async Task Add(MusicItemModel musicItem)
    {
        PlayList.Add(musicItem);
        await using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);
        await playlistRepository.AddAsync(musicItem.FilePath);
    }

    public async Task AddRange(IList<MusicItemModel> musicItems)
    {
        PlayList.AddRange(musicItems);
        await using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);

        var files = musicItems.Select(item => item.FilePath);
        await playlistRepository.AddRangeAsync(files);
    }

    public async Task AddToNext(IList<MusicItemModel> musicItems)
    {
        PlayList.RemoveAll(musicItems);
    
        await using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);

        // 从后往前插入，这样可以保持原有顺序
        for (int i = musicItems.Count - 1; i >= 0; i--)
        {
            PlayList.Insert(CurrentIndex + 1, musicItems[i]);
            await playlistRepository.InsertAsync(musicItems[i].FilePath, CurrentIndex + 1);
        }

        PlayedIndicesService.ClearPlayedIndices();
    }

    public async Task Remove(IList<MusicItemModel> musicItems)
    {
        PlayList.RemoveAll(musicItems);

        await using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);

        var files = musicItems.Select(item => item.FilePath);
        await playlistRepository.RemoveAsync(files);

        PlayedIndicesService.ClearPlayedIndices();
    }

    public async Task Clear()
    {
        PlayList.Clear();

        await using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);
        await playlistRepository.ClearAsync();
    }

    public async Task Toggle(
        IList<MusicItemModel> musicItems
        )
    {
        PlayList.Clear();
        PlayList.AddRange(musicItems);

        PlayedIndicesService.ClearPlayedIndices();

        await using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);
        await playlistRepository.SetAllAsync(PlayList.Select(item => item.FilePath));
    }

    public async Task Shuffle()
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

        await using var playlistRepository = new PlaylistRepository(StaticConfig.DatabasePath);
        await playlistRepository.SetAllAsync(PlayList.Select(item => item.FilePath));
    }
}
