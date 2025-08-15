using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;
using QwQ_Music.ViewModels.Panels;
using QwQ_Music.Views.Panels;

namespace QwQ_Music.ViewModels.Pages;

public partial class MusicListClassPageViewModel : NavigationViewModel
{
    private readonly AllMusicListPanelViewModel _allMusicListPanelViewModel = new();
    private readonly MusicListDetailsPanelViewModel _musicListDetailsPanelViewModel = new();

    public MusicListClassPageViewModel()
        : base("歌单")
    {
        Panels =
        [
            new AllMusicListPanel
            {
                DataContext = _allMusicListPanelViewModel,
            },
            new MusicListDetailsPanel
            {
                DataContext = _musicListDetailsPanelViewModel,
            },
        ];
    }

    public AvaloniaList<Control> Panels { get; set; }

    [RelayCommand]
    private void ToggleItem(MusicListModel model)
    {
        _musicListDetailsPanelViewModel.UpdateMusicListModel(model);
        NavigationIndex = 1;
    }
}
