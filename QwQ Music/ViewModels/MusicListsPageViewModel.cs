using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Services.ConfigIO;

namespace QwQ_Music.ViewModels;

public partial class MusicListsPageViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial ObservableCollection<PlayListItem> PlayListItems { get; set; } = [];

    public MusicListsPageViewModel()
    {
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await LoadPlayListsAsync();
        }
        catch (Exception e)
        {
            await LoggerService.ErrorAsync($"初始化歌单模型出错 : {e.Message}");
        }
    }

    private async Task LoadPlayListsAsync()
    {
        // 从数据库加载所有歌单名称和描述
        var playlistInfos = await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.LISTINFO,
            [nameof(MusicListModel.Name), nameof(MusicListModel.Description), nameof(MusicListModel.LatestPlayedMusic)],
            dict => new
            {
                Name = dict.TryGetValue(nameof(MusicListModel.Name), out object? name)
                    ? name.ToString() ?? string.Empty
                    : string.Empty,
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                Description = dict.TryGetValue(nameof(MusicListModel.Description), out object? desc)
                    ? desc?.ToString()
                    : null,
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                LatestPlayedMusic = dict.TryGetValue(nameof(MusicListModel.LatestPlayedMusic), out object? latest)
                    ? latest?.ToString()
                    : null,
            }
        );

        foreach (var info in playlistInfos.Where(info => !string.IsNullOrEmpty(info.Name)))
        {
            // 获取歌单中的第一首歌曲
            Debug.Assert(info.Name != null);
            string? firstSongPath = await GetFirstSongInPlaylist(info.Name) ?? info.LatestPlayedMusic;

            // 如果歌单为空，使用默认封面
            if (string.IsNullOrEmpty(firstSongPath))
            {
                PlayListItems.Add(
                    new PlayListItem(MusicExtractor.DefaultCover, info.Name, info.Description ?? string.Empty)
                );
                continue;
            }

            // 查找对应的音乐项以获取封面
            var coverImage = string.IsNullOrEmpty(firstSongPath)
                ? null
                : MusicPlayerViewModel
                    .Instance.MusicItems.FirstOrDefault(item => item.FilePath == firstSongPath)
                    ?.CoverImage;

            // 添加到列表中
            PlayListItems.Add(
                new PlayListItem(coverImage ?? MusicExtractor.DefaultCover, info.Name, info.Description ?? string.Empty)
            );
        }
    }

    private static async Task<string?> GetFirstSongInPlaylist(string playlistName)
    {
        // 获取歌单中的第一首歌曲路径
        var filePaths = await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.MUSICLISTS,
            [nameof(MusicItemModel.FilePath)],
            dict => dict.TryGetValue(nameof(MusicItemModel.FilePath), out object? path) ? path.ToString() : null,
            search: $"{nameof(MusicListModel.Name)} = '{playlistName.Replace("'", "''")}'"
        );

        return filePaths.FirstOrDefault();
    }
}

public record PlayListItem(Bitmap CoverImage, string Name, string Description);
