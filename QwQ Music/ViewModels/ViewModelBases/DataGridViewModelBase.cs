using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Services;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.ViewModels.ViewModelBases;

public partial class DataGridViewModelBase(ObservableCollection<MusicItemModel> musicItems) : ViewModelBase
{
    [ObservableProperty]
    public partial ObservableCollection<MusicItemModel> MusicItems { get; set; } = musicItems;

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

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

    protected virtual void OnSearchTextChanged(string? value) { }

    [ObservableProperty]
    public partial MusicItemModel? SelectedItem { get; set; }

    public IList<MusicItemModel>? SelectedItems { get; set; }

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

        await MusicPlayerViewModel.ToggleMusicAsync(SelectedItem).ConfigureAwait(false);
    }

    [RelayCommand]
    private void SelectedCurrentMusicItem()
    {
        SelectedItem = null;
        SelectedItem = MusicPlayerViewModel.CurrentMusicItem;
    }

    [RelayCommand]
    private async Task AddToMusicList(string musicListName)
    {
        if (SelectedItems is { Count: > 0 })
        {
            await MusicPlayerViewModel.MusicListsViewModel.AddToMusicList(SelectedItems.ToList(), musicListName);
        }
    }

    [RelayCommand]
    private async Task RemoveToMusicList(string musicListName)
    {
        if (SelectedItems is { Count: > 0 })
        {
            await MusicPlayerViewModel.MusicListsViewModel.RemoveToMusicList(SelectedItems.ToList(), musicListName);
        }
    }

    [RelayCommand]
    private async Task DeleteMusicItemsAsync(IList items)
    {
        if (items is not { Count: > 0 })
        {
            NotificationService.ShowLight(new Notification("提示", "请先选择音乐项哦~"), NotificationType.Success);
            return;
        }

        var musicItems = items.Cast<MusicItemModel>().ToList();

        if (await MusicPlayerViewModel.DeleteMusicItemsAsync(musicItems))
        {
            foreach (var item in musicItems)
            {
                MusicItems.Remove(item);
            }
        }
    }
}
