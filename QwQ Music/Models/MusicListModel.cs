using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Services.ConfigIO;

namespace QwQ_Music.Models;

public partial class MusicListModel(string name = "", string? description = null, string? coverPath = null)
    : ObservableObject
{
    public readonly string Name = name;

    [ObservableProperty]
    public partial ObservableCollection<MusicItemModel> MusicItems { get; set; } = [];

    public string? LatestPlayedMusic;

    public string? Description = description;

    public string? CoverPath = coverPath;

    public int Count => MusicItems.Count;

    public bool IsInitialized { get; private set; }
    public bool IsError { get; private set; }

    public async Task<MusicListModel> LoadAsync()
    {
        // 获取最近播放的音乐列表
        var latestPlayedMusicList = await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.LISTINFO,
            [nameof(LatestPlayedMusic)],
            dict => dict.TryGetValue(nameof(LatestPlayedMusic), out object? value) ? value.ToString() ?? null : null,
            ..1,
            $"{nameof(Name)} = '{Name.Replace("'", "''")}'"
        );

        // 设置最近播放的音乐
        if (latestPlayedMusicList.Count > 0)
            LatestPlayedMusic = latestPlayedMusicList[0];

        IsError = false;
        IsInitialized = true;

        return this;
    }

    public async Task<List<string?>> LoadMusicFilePathsAsync()
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
            [nameof(Count)] = Count.ToString(),
            [nameof(LatestPlayedMusic)] = LatestPlayedMusic,
        };
    }
}
