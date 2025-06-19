using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Services.Audio;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.Utilities;
using QwQ_Music.Views;
using QwQ.Avalonia.Utilities.MessageBus;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.ViewModels;

public partial class ApplicationViewModel : ObservableObject
{
    [RelayCommand]
    private static void ShowMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is not MainWindow mainWindow)
            return;

        if (mainWindow.IsVisible)
        {
            mainWindow.Activate();
            mainWindow.Topmost = true;
            mainWindow.Topmost = false;

            NotificationService.ShowLight(new Notification("看我", "窗口已经在显示了~"), NotificationType.Information);
        }
        else
        {
            mainWindow.ShowMainWindow();
        }
    }

    [RelayCommand]
    public static async Task ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        if (desktop.MainWindow is MainWindow mainWindow)
        {
            mainWindow.CloseMainWindow();
        }

        await OnShutdown();
        desktop.Shutdown();
    }

    internal static async Task OnShutdown()
    {
        try
        {
            await ConfigManager.SaveConfigAsync();

            await MusicPlayerViewModel.Instance.ShutdownAsync();

            await MessageBus
                .CreateMessage(new ExitReminderMessage(true))
                .SetAsOneTime()
                .WaitForCompletion()
                .PublishAsync();

            IconService.ClearCache();
            MousePenetrate.ClearCache();
            HotkeyService.ClearCache();

            AudioEngineManager.Dispose();
            MessageBus.Dispose();

            await DataBaseService.DisposeAsync();
            await LoggerService.InfoAsync(
                "\n"
                    + "===========================================\n"
                    + "应用程序已退出\n"
                    + "===========================================\n"
            );

            LoggerService.Shutdown();
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"程序退出时发生错误: {ex.Message}");
            LoggerService.Shutdown();
        }
    }
}
