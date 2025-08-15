using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Views;

namespace QwQ_Music.ViewModels;

public partial class ApplicationViewModel : ObservableObject
{
    public ThemeConfig ThemeConfig { get; set; } = ConfigManager.UiConfig.ThemeConfig;

    [RelayCommand]
    private static void ShowMainWindow()
    {
        if (App.TopLevel is not MainWindow mainWindow)
            return;

        if (mainWindow.IsVisible)
        {
            mainWindow.Activate();
            mainWindow.Topmost = true;
            mainWindow.Topmost = false;

            NotificationService.Info("看我", "窗口已经在显示了~");
        }
        else
        {
            mainWindow.ShowMainWindow();
        }
    }

    [RelayCommand]
    public static void ExitApplication()
    {
        if (App.TopLevel is MainWindow mainWindow)
        {
            mainWindow.CloseMainWindow();
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        desktop.Shutdown();
    }
}
