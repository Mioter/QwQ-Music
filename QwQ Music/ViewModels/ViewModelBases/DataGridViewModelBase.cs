using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;

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

        MusicPlayerViewModel.IsPlaying = false;
        await MusicPlayerViewModel.SetCurrentMusicItem(SelectedItem);
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
}
