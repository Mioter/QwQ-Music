using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayListViewModel : ViewModelBase
{

    [ObservableProperty] private MusicItemModel? _selectedItem;
    public MusicPlayerViewModel MusicPlayerViewModel => MusicPlayerViewModel.Instance;

    [RelayCommand]
    private void ToggleMusic()
    {
        if (SelectedItem == null) return;

        MusicPlayerViewModel.IsPlaying = false;
        MusicPlayerViewModel.SetCurrentMusicItem(SelectedItem);

    }

    [RelayCommand]
    private void ClearMusicPlayList()
    {
        MusicPlayerViewModel.MusicPlaylist.Clear();
    }

    [RelayCommand]
    private void SelectedCurrentMusicItem()
    {
        SelectedItem = null;
        SelectedItem = MusicPlayerViewModel.CurrentMusicItem;
    }
}
