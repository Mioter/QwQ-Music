using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Services;
using QwQ_Music.Utilities.MessageBus;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerTrayViewModel : ViewModelBase
{
    public double AlbumCoverCurrentAngle { get; set; }

    public double AlbumCoverRecordAngle { get; set; }

    public string? CurrentViewName { get; set; }

    public MusicPlayerTrayViewModel()
    {
        MusicPlayerViewModel.PlaybackStateChanged += MusicPlayerViewModelOnPlaybackStateChanged;

        NavigateService.CurrentViewChanged += CurrentViewChanged;
        StrongMessageBus.Instance.Subscribe<ExitReminderMessage>(ExitReminderMessageHandler);
    }

    private void CurrentViewChanged(string name)
    {
        CurrentViewName = name;
        OnPropertyChanged(nameof(CurrentViewName));
    }

    private void ExitReminderMessageHandler(ExitReminderMessage message)
    {
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
        if (e)
            return;

        RecordCurrentAngle();
    }

    private void RecordCurrentAngle()
    {
        AlbumCoverRecordAngle = AlbumCoverCurrentAngle;
        OnPropertyChanged(nameof(AlbumCoverRecordAngle));
    }
}
