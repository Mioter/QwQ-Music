using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Drawers;

public partial class MusicPlayListViewModel : DataGridViewModelBase
{
    public static DrawerStatusViewModel DrawerStatusViewModel => DrawerStatusViewModel.Default;

    [RelayCommand]
    private static async Task RemoveInPlaylist(IList items)
    {
        var musicItems = items.Cast<MusicItemModel>().ToList();

        await MusicPlayList.Remove(musicItems);
    }

    [RelayCommand]
    private static async Task ClearMusicPlayList()
    {
        await MusicPlayList.Clear();
    }
}
