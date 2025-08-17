using System.Collections;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Drawers;

public partial class MusicPlayListViewModel : DataGridViewModelBase
{
    public static DrawerStatusViewModel DrawerStatusViewModel => DrawerStatusViewModel.Default;

    [RelayCommand]
    private static void RemoveInPlaylist(IList items)
    {
        var musicItems = items.Cast<MusicItemModel>().ToList();

        MusicPlayList.Remove(musicItems);
    }

    [RelayCommand]
    private static void ClearMusicPlayList()
    {
        MusicPlayList.Clear();
    }
}
