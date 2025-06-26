using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ.Avalonia.Utilities.MessageBus;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.ViewModels.Pages;

public partial class AlbumClassPageViewModel : DataGridViewModelBase
{
    
    public readonly ObservableCollection<AlbumItemModel> AllAlbumItems = [];

    [ObservableProperty]
    public partial ObservableCollection<AlbumItemModel> AlbumItems { get; set; } = [];

    [ObservableProperty]
    public partial AlbumItemModel SelectedAlbumItem { get; set; } = null!;
    
    public AlbumClassPageViewModel()
    {
        MessageBus
            .ReceiveMessage<OperateCompletedMessage>(this)
            .WithHandler((_, _) => InitializeAlbumItems())
            .AsWeakReference()
            .Subscribe();
    }

    private void InitializeAlbumItems()
    {
        // 按专辑名和歌手分组，专辑名与歌手相同视为一个专辑项
        var albumGroups = MusicPlayerViewModel
            .MusicItems.Where(music =>
                !string.IsNullOrWhiteSpace(music.Album) && !string.IsNullOrWhiteSpace(music.Artists)
            )
            .GroupBy(music => new { music.Album, music.Artists })
            .ToList();

        foreach (
            var albumItem in albumGroups
                .Select(group => new { group, firstMusic = group.First() })
                .Select(t => new AlbumItemModel(t.group.Key.Album, t.group.Key.Artists, t.firstMusic.CoverPath))
        )
        {
            AllAlbumItems.Add(albumItem);
        }

        if (AllAlbumItems.Count <= 0)
            return;

        AlbumItems = AllAlbumItems;
        SelectedAlbumItem = AllAlbumItems[0];
        MusicItems = new ObservableCollection<MusicItemModel>(SearchMusicItems(SelectedAlbumItem));
    }

    [RelayCommand]
    private async Task PlayAlbumMusic(AlbumItemModel albumItem)
    {
        try
        {
            var albumMusicItems = SearchMusicItems(albumItem);
            if (albumMusicItems.Count < 0)
                return;
            await MusicPlayerViewModel.TogglePlaylist(new ObservableCollection<MusicItemModel>(albumMusicItems));
        }
        catch (Exception ex)
        {
            // 可以在这里添加错误日志记录
            NotificationService.ShowLight(
                new Notification("错误", $"播放专辑时出错: {ex.Message}"),
                NotificationType.Error
            );
        }
    }

    private static List<MusicItemModel> SearchMusicItems(AlbumItemModel albumItem)
    {
        // 找到该专辑对应的所有音乐项
        var albumMusicItems = MusicPlayerViewModel
            .MusicItems.Where(music => music.Album == albumItem.Title && music.Artists == albumItem.Artist)
            .ToList();

        return albumMusicItems;
    }
    
    protected override void OnSearchTextChanged(string? value)
    {
        var source = string.IsNullOrEmpty(value) ? AllAlbumItems : AllAlbumItems.Where(MatchesSearchCriteria);

        AlbumItems = new ObservableCollection<AlbumItemModel>(source);
        return;

        bool MatchesSearchCriteria(AlbumItemModel item) =>
            item.Title.Contains(value, StringComparison.OrdinalIgnoreCase)
            || item.Artist.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void ToggleItem(AlbumItemModel model)
    {
        SelectedAlbumItem = model;
        MusicItems = new ObservableCollection<MusicItemModel>(SearchMusicItems(model));
    }
}
