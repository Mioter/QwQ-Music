using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Services;
using QwQ_Music.Utilities.MessageBus;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerTrayViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial double AlbumCoverCurrentAngle { get; set; }

    [ObservableProperty]
    public partial double AlbumCoverRecordAngle { get; set; }

    [ObservableProperty]
    public partial string? CurrentViewName { get; set; }

    public MusicPlayerTrayViewModel()
    {
        MusicPlayerViewModel.PlaybackStateChanged += MusicPlayerViewModelOnPlaybackStateChanged;
        MusicPlayerViewModel.CurrentMusicItemChanging += AudioPlayOnTrackIndexChanging;
        NavigateService.CurrentViewChanged += CurrentViewChanged;
        StrongMessageBus.Instance.Subscribe<ExitReminderMessage>(ExitReminderMessageHandler);
    }

    private void CurrentViewChanged(string name) => CurrentViewName = name;

    private void ExitReminderMessageHandler(ExitReminderMessage message)
    {
        MusicPlayerViewModel.CurrentMusicItemChanging -= AudioPlayOnTrackIndexChanging;
        MusicPlayerViewModel.PlaybackStateChanged -= MusicPlayerViewModelOnPlaybackStateChanged;
        NavigateService.CurrentViewChanged -= CurrentViewChanged;
    }

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    [RelayCommand]
    private static void NavigationToSoundEffectView() => NavigateService.NavigateEvent("音效");

    [RelayCommand]
    private static void ResetPlaybackSpeed() => MusicPlayerViewModel.Speed = 1.0f;

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
                MusicPlayerViewModel.Volume += 2;
                break;
            case < 0:
                MusicPlayerViewModel.Volume -= 2;
                break;
        }
    }

    [RelayCommand]
    private static void OnSpeedBarPointerWheelChanged(PointerWheelEventArgs? e)
    {
        if (e == null)
            return;
        e.Handled = true;

        switch (e.Delta.Y)
        {
            case > 0:
                MusicPlayerViewModel.Speed += 0.01f;
                break;
            case < 0:
                MusicPlayerViewModel.Speed -= 0.01f;
                break;
        }
    }

    private void MusicPlayerViewModelOnPlaybackStateChanged(object? sender, bool e)
    {
        if (!e)
            AlbumCoverRecordAngle = AlbumCoverCurrentAngle;
    }

    private void AudioPlayOnTrackIndexChanging(object? sender, CurrentMusicItemChangedCancelEventArgs e)
    {
        AlbumCoverRecordAngle = 0;
    }
}
