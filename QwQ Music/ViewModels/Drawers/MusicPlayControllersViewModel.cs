using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Drawers;

public partial class MusicPlayControllersViewModel : ViewModelBase {
    public static DrawerManager DrawerManager => DrawerManager.Instance;

    public static AudioPlayManager AudioPlayManager => AudioPlayManager.Instance;

    [RelayCommand]
    private static void NavigationToSoundEffectView() { NavigateService.NavigateTo("音效"); }

    [RelayCommand]
    private static void ResetPlaybackSpeed() { AudioPlayManager.Speed = 1.0f; }

    [RelayCommand]
    private static void PlaySpeedUp() { AudioPlayManager.Speed += 0.1f; }

    [RelayCommand]
    private static void PlaySpeedDown() { AudioPlayManager.Speed -= 0.1f; }

    [RelayCommand]
    private static void OnVolumeBarPointerWheelChanged(PointerWheelEventArgs e) {
        // 阻止事件冒泡到父级元素
        e.Handled = true;

        switch (e.Delta.Y) {
            // 根据你的需求处理滚轮滚动事件
            case > 0:
                AudioPlayManager.Volume += 5;

                break;
            case < 0:
                AudioPlayManager.Volume -= 5;

                break;
        }
    }

    [RelayCommand]
    private static void OnSpeedBarPointerWheelChanged(PointerWheelEventArgs e) {
        e.Handled = true;

        switch (e.Delta.Y) {
            case > 0:
                AudioPlayManager.Speed += 0.05f;

                break;
            case < 0:
                AudioPlayManager.Speed -= 0.05f;

                break;
        }
    }

    [RelayCommand]
    private static void OpenVolumeFlyout(Button volumeButton) { FlyoutBase.ShowAttachedFlyout(volumeButton); }
}