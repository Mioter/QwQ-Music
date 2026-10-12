using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.ViewModels;

public partial class ApplicationViewModel : ObservableObject {
    public ThemeConfig ThemeConfig { get; } = ConfigManager.UiConfig.ThemeConfig;

    public static void ShowMainWindow(bool onlyActivate) {
        if (App.TopLevel is not { } mainWindow)
            return;

        if (mainWindow.IsVisible) {
            if (onlyActivate) {
                mainWindow.Activate();
            } else {
                mainWindow.Topmost = true;
                mainWindow.Topmost = false;

                NotificationService.Info("看我", "窗口已经在显示了~");
            }
        } else {
            mainWindow.ShowMainWindow();
        }
    }

    [RelayCommand]
    private static void ShowMainWindow() { ShowMainWindow(false); }

    [RelayCommand]
    public static void Shutdown() {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) {
            Environment.Exit(1);
            return;
        }

        desktop.Shutdown();
    }
}