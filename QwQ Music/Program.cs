using System;
using Avalonia;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.ViewModels;
using QwQ.Avalonia.Utilities.MessageBus;

namespace QwQ_Music;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        throw new NotImplementedException();
    }

    private static void CurrentDomainOnProcessExit(object? sender, EventArgs e)
    {          
        AppDomain.CurrentDomain.ProcessExit -= CurrentDomainOnProcessExit;

        try
        {
            ConfigInfoModel.SaveAll();

            MessageBus.CreateMessage(new ExitReminderMessage(true))
                .SetAsOneTime()
                .WaitForCompletion()
                .Publish();

            MusicPlayerViewModel.Instance.SaveAsync().Wait();

            DataBaseService.CloseConnection();
            LoggerService.Shutdown();
        }
        catch (Exception ex)
        {
            // Log the exception or handle it as needed
            LoggerService.Error($"An error occurred: {ex.Message}");
            LoggerService.Shutdown();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
    }
}
