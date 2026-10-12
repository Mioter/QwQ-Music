using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Windows;

public partial class DesktopPlayControlWindowViewModel : ViewModelBase {
    public static AudioPlayManager AudioPlayManager { get; } = AudioPlayManager.Instance;

    public DrawerManager DrawerManager { get; } = DrawerManager.Instance;

    [RelayCommand]
    private void ShowMusicPlayerPage() {
        ApplicationViewModel.ShowMainWindow(true);
        DrawerManager.IsMusicPlayerPanelVisible = true;
    }
}