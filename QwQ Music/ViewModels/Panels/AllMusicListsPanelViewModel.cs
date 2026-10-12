using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Panels;

public partial class AllMusicListsPanelViewModel : ItemsViewModelBase<MusicListModel> {
    public AllMusicListsPanelViewModel() : base(nameof(AllMusicListsPanelViewModel)) {
        Update(null, EventArgs.Empty);
        MusicListsManager.CollectionChanged += Update;
    }

    [NotNullIfNotNull(nameof(SelectedItem))]
    public MusicItemsViewModelBase? Current { get; set; }

    public MusicListModel? SelectedItem { get; set; }

    public int CurrentStatus => SelectedItem is null ? 0 : 1;

    public static MusicListsManager MusicListsManager => MusicListsManager.Instance;

    private void Update(object? sender, EventArgs args) {
        SetCurrentList(nameof(MusicListsManager), MusicListsManager.MusicLists);
    }

    [RelayCommand]
    public async Task SelectMusicList(MusicListModel musicList) {
        await musicList.LoadCurrentAsync().ConfigureAwait(false);
        Current = new MusicItemsViewModelBase($"{musicList.Name} - {musicList.Creator}");
        Current.ChangeAllItems(null, musicList.Musics);
        SelectedItem = musicList;
        OnPropertyChanged(nameof(SelectedItem));
        OnPropertyChanged(nameof(Current));
        OnPropertyChanged(nameof(CurrentStatus));
    }

    [RelayCommand]
    public void BackToPanel() {
        SelectedItem?.DisposeCurrent();
        SelectedItem = null;
        Current = null;
        OnPropertyChanged(nameof(CurrentStatus));
    }

    protected override bool CustomFilter(in string value, in MusicListModel item) {
        //TODO TAGS
        return item.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
               item.Description.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private void PlayCurrentList() {
        Debug.Assert(SelectedItem != null);
        if (SelectedItem?.IsLoaded != true)
            return;
        if ($"{SelectedItem.Name} - {SelectedItem.Creator}" == PlaylistManager.Instance.CurrentListName)
            return;
        Debug.Assert(SelectedItem.Musics != null);
        PlaylistManager.Instance
                       .ReplaceAsync($"{SelectedItem.Name} - {SelectedItem.Creator}", SelectedItem.Musics!, 0, true)
                       .ContinueWith(LoggerService.HandleException)
                       .ConfigureAwait(false);
    }

    ~AllMusicListsPanelViewModel() { MusicListsManager.CollectionChanged -= Update; }
}