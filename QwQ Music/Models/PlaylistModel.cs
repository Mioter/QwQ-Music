using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Services.ConfigIO;

namespace QwQ_Music.Models;

public partial class PlaylistModel(string name = "") : ObservableObject, IEnumerable<MusicItemModel>
{
    public readonly string Name = name;

    [ObservableProperty]
    public partial ObservableCollection<MusicItemModel> MusicItems { get; set; } = [];

    public string? LatestPlayedMusic;

    public int Count => MusicItems.Count;
    public bool IsInitialized { get; private set; }
    public bool IsError { get; private set; }

    // 添加一个列表来跟踪已播放的歌曲索引
    private static readonly List<int> PlayedIndices = [];

    public async Task<List<string?>> LoadAsync()
    {
        // 获取最近播放的音乐列表
        var latestPlayedMusicList = await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.LISTNAMES,
            [nameof(LatestPlayedMusic)],
            dict => dict.TryGetValue(nameof(LatestPlayedMusic), out object? value) ? value.ToString() ?? null : null,
            ..1,
            search: $"{nameof(Name)} = '{Name.Replace("'", "''")}'"
        );

        // 创建一个列表来存储文件路径
        var filePaths = await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.PLAYLISTS,
            [nameof(MusicItemModel.FilePath)],
            dict =>
                dict.TryGetValue(nameof(MusicItemModel.FilePath), out object? value) ? value.ToString() ?? null : null,
            search: $"{nameof(Name)} = '{Name.Replace("'", "''")}'"
        );

        // 设置最近播放的音乐
        if (latestPlayedMusicList.Count > 0)
            LatestPlayedMusic = latestPlayedMusicList[0];

        IsError = false;
        IsInitialized = true;

        return filePaths;
    }

    public Dictionary<string, string?> Dump() =>
        new()
        {
            [nameof(Name)] = Name,
            [nameof(Count)] = Count.ToString(),
            [nameof(LatestPlayedMusic)] = LatestPlayedMusic,
        };

    public IEnumerator<MusicItemModel> GetEnumerator() => MusicItems.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static int GetNonRepeatingRandomIndex(int current, int count)
    {
        // 如果所有歌曲都已播放过（或者播放列表发生了变化），重置已播放列表
        if (PlayedIndices.Count >= count || PlayedIndices.Any(i => i >= count))
        {
            PlayedIndices.Clear();
            PlayedIndices.Add(current); // 将当前播放的歌曲添加到已播放列表
        }

        // 获取所有未播放的索引
        var availableIndices = Enumerable.Range(0, count).Where(i => !PlayedIndices.Contains(i)).ToList();

        // 如果没有可用的索引（理论上不应该发生），返回一个随机索引
        if (availableIndices.Count == 0)
        {
            PlayedIndices.Clear();
            PlayedIndices.Add(current);
            availableIndices = Enumerable.Range(0, count).Where(i => !PlayedIndices.Contains(i)).ToList();
        }

        // 从可用索引中随机选择一个
        var random = new Random();
        int randomIndex = availableIndices[random.Next(0, availableIndices.Count)];

        // 将选中的索引添加到已播放列表
        PlayedIndices.Add(randomIndex);

        return randomIndex;
    }

    public static void ClearPlayedIndices()
    {
        PlayedIndices.Clear();
    }
}
