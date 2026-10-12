using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Panels;

public partial class AllAlbumsPanelViewModel : ItemsViewModelBase<AlbumModel> {
    public AllAlbumsPanelViewModel() : base(nameof(AllAlbumsPanelViewModel)) {
        _ = AlbumRepository.Instance.GetAsync()
                           .ContinueWith(result => ChangeAllItems(null, result.Result))
                           .ContinueWith(LoggerService.HandleException)
                           .ConfigureAwait(false);
    }

    public StyleConfig StyleConfig { get; } = ConfigManager.UiConfig.StyleConfig;
    public AlbumModel? SelectedItem { get; set; }

    [NotNullIfNotNull(nameof(SelectedItem))]
    public MusicItemsViewModelBase? CurrentAlbum { get; private set; }

    public int CurrentState => SelectedItem is null ? 0 : 1;

    [RelayCommand]
    public async Task SelectAlbum(AlbumModel model) {
        await model.LoadCurrentAsync().ConfigureAwait(true);
        SelectedItem = model;
        CurrentAlbum = new MusicItemsViewModelBase(model.Name);
        CurrentAlbum.ChangeAllItems(
            null,
            MusicItemsManager.All.MusicItems.Values.Where(item => item.Album == model.Name &&
                                                                  item.AlbumArtists == model.Artists));
        OnPropertyChanged(nameof(CurrentState));
        OnPropertyChanged(nameof(CurrentAlbum));
        OnPropertyChanged(nameof(SelectedItem));
    }

    [RelayCommand]
    public void AddToNext(MusicItemModel item) { PlaylistManager.Instance.AddSelectedToNext(item); }

    [RelayCommand]
    public void BackToPanel() {
        SelectedItem?.DisposeCurrent();
        SelectedItem = null;
        CurrentAlbum = null;
        OnPropertyChanged(nameof(CurrentState));
    }

    [RelayCommand]
    public async Task FetchMoreAlbumsAsync() {
        ChangeAllItems(
            null,
            await AlbumRepository.Instance.GetAsync(skip: AllItemsList.Count, limit: 50).ConfigureAwait(false));
    }


    [RelayCommand]
    private void PlayAlbumMusic() {
        if (SelectedItem is null)
            return;

        Debug.Assert(CurrentAlbum != null);
        PlaylistManager.Instance
                       .ReplaceAsync(
                           $"{SelectedItem.Name} - {SelectedItem.Artists}",
                           CurrentAlbum.FilteredList,
                           0,
                           true)
                       .ContinueWith(LoggerService.HandleException)
                       .ConfigureAwait(false);
    }

    protected override bool CustomFilter(in string value, in AlbumModel item) {
        return item.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
               item.Artists.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}