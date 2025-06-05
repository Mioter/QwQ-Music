using System.Collections.ObjectModel;
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
        if (SelectedItem != null)
            await MusicPlayerViewModel.MusicListsViewModel.AddToMusicList(SelectedItem, musicListName);
    }

    [RelayCommand]
    private async Task RemoveToMusicList(string musicListName)
    {
        if (SelectedItem != null)
            await MusicPlayerViewModel.MusicListsViewModel.RemoveToMusicList(SelectedItem, musicListName);
    }
}
