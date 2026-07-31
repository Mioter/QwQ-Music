using System.Collections;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Drawers;

public partial class MusicPlaylistViewModel : ViewModelBase {
    public static DrawerManager DrawerManager => DrawerManager.Instance;
    public static AudioPlayManager AudioPlayManager => AudioPlayManager.Instance;
    public static MusicItemsManager MusicItemsManager => MusicItemsManager.All;
    public AvaloniaList<PlaylistItemModel> MusicList => PlaylistManager.Instance.ActualPlaylist;

    public List<PlaylistItemModel> SelectedItems { get; set; } = [];

    [RelayCommand]
    private void Remove(IList items) {
        List<PlaylistItemModel> musicItems = items.Cast<PlaylistItemModel>().ToList();
        PlaylistManager.Instance.Remove(musicItems);
    }

    [RelayCommand]
    private void ClearMusic() {
        AudioPlayManager.ClearPlaylist();
        AudioPlayManager.Instance.Stop();
    }

    [RelayCommand]
    private void JumpToTop(ListBox listBox) {
        // 滚动到第一行（第一行数据）
        if (MusicList.Count > 0)
            listBox.ScrollIntoView(MusicList.First());
    }

    [RelayCommand]
    public static void ScrollToCurrent(ListBox listbox) {
        listbox.SelectedItem = AudioPlayManager.Instance.CurrentMusicItem;
        listbox.ScrollIntoView(AudioPlayManager.Instance.CurrentMusicItem);
    }
}