using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ.Avalonia.Utilities.MessageBus;

namespace QwQ_Music.ViewModels.UserControls;

public partial class MusicPlayerTrayViewModel : ViewModelBase
{
    public RolledLyricConfig RolledLyric { get; } = ConfigManager.LyricConfig.RolledLyric;

    public double AlbumCoverCurrentAngle { get; set; }

    public double AlbumCoverRecordAngle { get; set; }

    [ObservableProperty]
    public partial bool IsSoundEffectView { get; set; }

    public MusicPlayerTrayViewModel()
    {
        MusicPlayerViewModel.PlaybackStateChanged += MusicPlayerViewModelOnPlaybackStateChanged;

        NavigateService.CurrentViewChanged += CurrentViewChanged;
        MessageBus
            .ReceiveMessage<ExitReminderMessage>(this)
            .WithHandler(ExitReminderMessageHandler)
            .AsWeakReference()
            .Subscribe();
    }

    private void CurrentViewChanged(string name)
    {
        IsSoundEffectView = name == "音效" || NavigateService.GetParentView(name) == "音效";
    }

    private void ExitReminderMessageHandler(ExitReminderMessage message, object _)
    {
        MusicPlayerViewModel.PlaybackStateChanged -= MusicPlayerViewModelOnPlaybackStateChanged;
        NavigateService.CurrentViewChanged -= CurrentViewChanged;
    }

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    [RelayCommand]
    private static void NavigationToSoundEffectView() => NavigateService.NavigateTo("音效");

    [RelayCommand]
    private static void ResetPlaybackSpeed() => MusicPlayerViewModel.Speed = 1.0f;

    [RelayCommand]
    private static void OnVolumeBarPointerWheelChanged(PointerWheelEventArgs e)
    {
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
    private static void OnSpeedBarPointerWheelChanged(PointerWheelEventArgs e)
    {
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

    [RelayCommand]
    private static void PlaySpeedUp()
    {
        MusicPlayerViewModel.Speed += 0.1f;
    }

    [RelayCommand]
    private static void PlaySpeedDown()
    {
        MusicPlayerViewModel.Speed -= 0.1f;
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
