using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.Pages;

public partial class ViewMusicListPageViewModel(MusicListModel musicList) : DataGridViewModelBase(musicList.MusicItems)
{
    [ObservableProperty]
    public partial MusicListModel MusicListModel { get; set; } = musicList;

    protected override void OnSearchTextChanged(string? value)
    {
        var source = string.IsNullOrEmpty(value)
            ? MusicListModel.MusicItems
            : MusicListModel.MusicItems.Where(MatchesSearchCriteria);

        MusicItems = new ObservableCollection<MusicItemModel>(source);
        return;

        bool MatchesSearchCriteria(MusicItemModel item)
        {
            return item.Title.Contains(value, StringComparison.OrdinalIgnoreCase)
                || item.Artists.Contains(value, StringComparison.OrdinalIgnoreCase)
                || item.Album.Contains(value, StringComparison.OrdinalIgnoreCase);
        }
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
