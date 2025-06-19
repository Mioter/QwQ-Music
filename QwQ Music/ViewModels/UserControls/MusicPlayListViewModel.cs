using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.UserControls;

public partial class MusicPlayListViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial MusicItemModel? SelectedItem { get; set; }
    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    [RelayCommand]
    private async Task ToggleMusicAsync()
    {
        if (SelectedItem == null)
            return;

        await MusicPlayerViewModel.ToggleMusicAsync(SelectedItem).ConfigureAwait(false);
    }

    [RelayCommand]
    private static void ClearMusicPlayList()
    {
        MusicPlayerViewModel.PlayList.MusicItems.Clear();
    }

    [RelayCommand]
    private void SelectedCurrentMusicItem()
    {
        SelectedItem = null;
        SelectedItem = MusicPlayerViewModel.CurrentMusicItem;
    }
}
