using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;
using QwQ_Music.ViewModels.Panels;
using QwQ_Music.Views.Panels;

namespace QwQ_Music.ViewModels.Pages;

public partial class AlbumClassPageViewModel : NavigationViewModel
{
    private readonly AlbumDetailsPanelViewModel _albumDetailsPanelViewModel = new();

    private readonly AllAlbumsPanelViewModel _allAlbumsPanelViewModel = new();

    public AlbumClassPageViewModel()
        : base("专辑")
    {
        Panels =
        [
            new AllAlbumsPanel
            {
                DataContext = _allAlbumsPanelViewModel,
            },
            new AlbumDetailsPanel
            {
                DataContext = _albumDetailsPanelViewModel,
            },
        ];
    }

    public AvaloniaList<Control> Panels { get; set; }

    [RelayCommand]
    private void ToggleItem(AlbumItemModel model)
    {
        _allAlbumsPanelViewModel.SelectedAlbumItem = model;
        _albumDetailsPanelViewModel.UpdateAlbumItemModel(model);
        NavigationIndex = 1;
    }
}
