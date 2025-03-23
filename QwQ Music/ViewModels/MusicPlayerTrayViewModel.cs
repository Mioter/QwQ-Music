using Avalonia;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Utilities.MessageBus;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerTrayViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _albumCoverCurrentAngle;

    [ObservableProperty]
    private double _albumCoverRecordAngle;

    [ObservableProperty]
    private double _playButtonAngle;

    [ObservableProperty]
    private Thickness _playButtonPadding;

    public MusicPlayerTrayViewModel()
    {
        MusicPlayerViewModel.PlaybackStateChanged += MusicPlayerViewModelOnPlaybackStateChanged;
        MusicPlayerViewModel.CurrentMusicItemChanging += AudioPlayOnTrackIndexChanging;
        StrongMessageBus.Instance.Subscribe<ExitReminderMessage>(ExitReminderMessageChanged);
    }

    private void ExitReminderMessageChanged(ExitReminderMessage message)
    {
        MusicPlayerViewModel.CurrentMusicItemChanging -= AudioPlayOnTrackIndexChanging;
        MusicPlayerViewModel.PlaybackStateChanged -= MusicPlayerViewModelOnPlaybackStateChanged;
    }

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    [RelayCommand]
    private static void NavigationToSoundEffectView()
    {
        NavigateService.NavigateEvent("音效");
    }

    [RelayCommand]
    private static void OnVolumeBarPointerWheelChanged(PointerWheelEventArgs? e)
    {
        if (e == null)
            return;
        // 阻止事件冒泡到父级元素
        e.Handled = true;

        switch (e.Delta.Y)
        {
            // 根据你的需求处理滚轮滚动事件
            case > 0:
                MusicPlayerViewModel.VolumePercent += 2;
                break;
            case < 0:
                MusicPlayerViewModel.VolumePercent -= 2;
                break;
        }
    }

    private void MusicPlayerViewModelOnPlaybackStateChanged(object? sender, bool e)
    {
        if (!e)
            AlbumCoverRecordAngle = AlbumCoverCurrentAngle;
    }

    private void AudioPlayOnTrackIndexChanging(object? sender, MusicItemModel musicItemModel)
    {
        AlbumCoverRecordAngle = 0;
    }
}
