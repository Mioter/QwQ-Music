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

    [RelayCommand]
    private async Task ToggleMusic()
    {
        if (SelectedItem == null)
            return;

        MusicPlayerViewModel.IsPlaying = false;
        await MusicPlayerViewModel.SetCurrentMusicItem(SelectedItem);
    }

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
}
