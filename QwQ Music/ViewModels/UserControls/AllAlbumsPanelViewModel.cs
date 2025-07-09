using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Services;

namespace QwQ_Music.ViewModels.UserControls;

public partial class AllAlbumsPanelViewModel(MusicPlayerViewModel musicPlayerViewModel) : ObservableObject
{
    public readonly ObservableCollection<AlbumItemModel> AllAlbumItems = [];

    [ObservableProperty]
    public partial ObservableCollection<AlbumItemModel> AlbumItems { get; set; } = [];

    [ObservableProperty]
    public partial AlbumItemModel? SelectedAlbumItem { get; set; }

    internal void InitializeAlbumItems()
    {
        // 按专辑名和歌手分组，专辑名与歌手相同视为一个专辑项
        var albumGroups = musicPlayerViewModel
            .MusicItems.Where(music =>
                !string.IsNullOrWhiteSpace(music.Album) && !string.IsNullOrWhiteSpace(music.Artists)
            )
            .GroupBy(music => new { music.Album, music.Artists })
            .ToList();

        foreach (
            var albumItem in albumGroups
                .Select(group => new { group, firstMusic = group.First() })
                .Select(t => new AlbumItemModel(t.group.Key.Album, t.group.Key.Artists, t.firstMusic.CoverFileName))
        )
        {
            AllAlbumItems.Add(albumItem);
        }

        if (AllAlbumItems.Count <= 0)
            return;

        AlbumItems = AllAlbumItems;
    }

    [RelayCommand]
    private async Task PlayAlbumMusic(AlbumItemModel albumItem)
    {
        try
        {
            var albumMusicItems = SearchMusicItems(albumItem);
            if (albumMusicItems.Count < 0)
                return;
            await musicPlayerViewModel.TogglePlaylist(new ObservableCollection<MusicItemModel>(albumMusicItems));
        }
        catch (Exception ex)
        {
            // 可以在这里添加错误日志记录
            NotificationService.ShowLight("错误", $"播放专辑时出错: {ex.Message}", NotificationType.Error);
        }
    }

    private List<MusicItemModel> SearchMusicItems(AlbumItemModel albumItem)
    {
        // 找到该专辑对应的所有音乐项
        var albumMusicItems = musicPlayerViewModel
            .MusicItems.Where(music => music.Album == albumItem.Name && music.Artists == albumItem.Artist)
            .ToList();

        return albumMusicItems;
    }
}
