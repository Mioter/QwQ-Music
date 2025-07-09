using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.UserControls;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.UserControls;
using QwQ.Avalonia.Utilities.MessageBus;

namespace QwQ_Music.ViewModels.Pages;

public partial class AlbumClassPageViewModel : NavigationViewModel
{
    public AlbumClassPageViewModel()
        : base("专辑")
    {
        AllAlbumsPanelViewModel = new AllAlbumsPanelViewModel(MusicPlayerViewModel.Instance);
        _userControls = [new AllAlbumsPanel { DataContext = AllAlbumsPanelViewModel }, new AlbumDetailsPanel()];

        CurrentControl = _userControls[0];

        MessageBus
            .ReceiveMessage<OperateCompletedMessage>(this)
            .WithHandler((_, _) => AllAlbumsPanelViewModel.InitializeAlbumItems())
            .AsWeakReference()
            .Subscribe();
    }

    public AllAlbumsPanelViewModel AllAlbumsPanelViewModel { get; set; }

    private readonly AlbumDetailsPanelViewModel _albumDetailsPanelViewModel = new();

    public string? SearchText
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            BackAllAlbum();
            var source = string.IsNullOrEmpty(value)
                ? AllAlbumsPanelViewModel.AllAlbumItems
                : AllAlbumsPanelViewModel.AllAlbumItems.Where(MatchesSearchCriteria);

            AllAlbumsPanelViewModel.AlbumItems = new ObservableCollection<AlbumItemModel>(source);
            return;

            bool MatchesSearchCriteria(AlbumItemModel item) =>
                item.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
                || item.Artist.Contains(value, StringComparison.OrdinalIgnoreCase);
        }
    }

    private readonly UserControl[] _userControls;

    [ObservableProperty]
    public partial UserControl CurrentControl { get; set; }

    [RelayCommand]
    private void ToggleItem(AlbumItemModel model)
    {
        AllAlbumsPanelViewModel.SelectedAlbumItem = model;
        _userControls[1].DataContext = _albumDetailsPanelViewModel.UpdateAlbumItemModel(model);
        NavigationIndex = 1;
        CurrentControl = _userControls[1];
    }

    [RelayCommand]
    private static void BackAllAlbum()
    {
        NavigateService.NavigateTo("全部专辑");
    }

    protected override void OnNavigateTo(int index)
    {
        CurrentControl = _userControls[index];
    }
}
