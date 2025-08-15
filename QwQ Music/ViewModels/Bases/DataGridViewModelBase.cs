using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;

namespace QwQ_Music.ViewModels.Bases;

public partial class DataGridViewModelBase : ViewModelBase
{
    protected DataGridViewModelBase() { }

    protected DataGridViewModelBase(AvaloniaList<MusicItemModel> musicItems)
    {
        MusicItems = musicItems;
    }

    [ObservableProperty] public partial AvaloniaList<MusicItemModel> MusicItems { get; set; } = [];

    public static MusicPlayerViewModel MusicPlayerViewModel => MusicPlayerViewModel.Default;

    public static MusicPlayListManager MusicPlayList => MusicPlayListManager.Default;

    public static MusicItemManager MusicItemManager => MusicItemManager.Default;

    public static MusicListsManager MusicListsManager => MusicListsManager.Default;

    public string? SearchText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnSearchTextChanged(field);
            }
        }
    }

    [ObservableProperty] public partial MusicItemModel? SelectedItem { get; set; }

    public List<MusicItemModel>? SelectedItems { get; set; }

    protected virtual void OnSearchTextChanged(string? value) { }

    [RelayCommand]
    private void SelectedItemChanged(IList items)
    {
        SelectedItems = items.Cast<MusicItemModel>().ToList();
    }

    [RelayCommand]
    private async Task ToggleMusicAsync()
    {
        if (SelectedItem == null)
            return;

        await MusicPlayerViewModel.ToggleMusicAsync(SelectedItem);
    }

    [RelayCommand]
    private void SelectedCurrentMusicItem()
    {
        SelectedItem = null;
        SelectedItem = MusicPlayerViewModel.CurrentMusicItem;
    }

    [RelayCommand]
    private static async Task AddToPlaylistNext(IList items)
    {
        if (items.Count <= 0)
            return;

        var musicItems = items.Cast<MusicItemModel>().ToList();

        await MusicPlayList.AddToNext(musicItems);
    }

    [RelayCommand]
    private async Task AddToMusicList(MusicListModel? musicListModel)
    {
        if (SelectedItems is { Count: > 0 } && musicListModel?.IdStr != null)
        {
            await MusicListsManager.AddToMusicList(SelectedItems, musicListModel);
        }
    }

    [RelayCommand]
    private async Task RemoveInMusicList(MusicListModel? musicListModel)
    {
        if (SelectedItems is { Count: > 0 } && musicListModel?.IdStr != null)
        {
            await MusicListsManager.RemoveToMusicList(SelectedItems, musicListModel);
        }
    }

    [RelayCommand]
    private async Task DeleteMusicItemsAsync(IList items)
    {
        if (items is not { Count: > 0 })
        {
            NotificationService.Info("提示", "请先选择音乐项哦~");

            return;
        }

        var musicItems = items.Cast<MusicItemModel>().ToList();

        var successItems = await MusicItemManager.Delete(musicItems);

        if (successItems == null)
            return;

        MusicItems.RemoveAll(successItems);
    }
}
