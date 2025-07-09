using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.Pages;

namespace QwQ_Music.ViewModels.Pages;

public partial class MusicListsPageViewModel : ViewModelBase
{
    public static MusicPlayerViewModel MusicPlayerViewModel { get; set; } = MusicPlayerViewModel.Instance;

    public static MusicListsViewModel MusicListsViewModel { get; } = MusicListsViewModel.Instance;

    public static IBrush RandomColor => ColorGenerator.GeneratePastelColor();

    [RelayCommand]
    private static async Task OpenMusicLists(MusicListModel musicList)
    {
        if (!musicList.IsInitialized)
        {
            await musicList.LoadAsync(MusicPlayerViewModel.MusicItems);
        }

        MainWindowViewModel.Instance.AddTabPage(
            musicList.Key,
            musicList.Name,
            musicList.CoverImage,
            new ViewMusicListPage { DataContext = new ViewMusicListPageViewModel(musicList) }
        );
    }

    [RelayCommand]
    private static async Task TogglePlaylist(MusicListModel musicList)
    {
        if (musicList.MusicItems.Count <= 0)
            return;

        if (!musicList.IsInitialized)
        {
            await musicList.LoadAsync(MusicPlayerViewModel.MusicItems);
        }

        MusicItemModel? selectedMusic = null;

        // 如果有最近播放记录，尝试找到对应歌曲
        if (musicList.LatestPlayedMusic != null)
        {
            selectedMusic = musicList.MusicItems.FirstOrDefault(x => x.FilePath == musicList.LatestPlayedMusic);
        }

        await MusicPlayerViewModel.TogglePlaylist(musicList.MusicItems, selectedMusic);
    }
}
