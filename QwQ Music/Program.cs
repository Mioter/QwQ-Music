using System;
using Avalonia;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Utilities;
using QwQ_Music.ViewModels;
using QwQ.Avalonia.Utilities.MessageBus;

namespace QwQ_Music;

public static class Program
{
    public static string VersionText => "0.9.1+build.2508016.1";

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            LoggerService.Info("Starting up\n" +
                $"""
                 ===========================================

                  ___ ___  ___   ___ ___ _  _ 
                 | _ ) _ \/ _ \ / __| __| \| |
                 | _ \   / (_) | (__| _|| .` |
                 |___/_|_\\___/ \___|___|_|\_|
                                              
                        ▶  QwQ Music v{VersionText}  🔊
                      "Where emotions meet melody"

                 ===========================================
                 """
            );

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            LoggerService.Error($"程序异常退出！\n捕捉到未处理异常:\n {e.Message}\n {e.StackTrace}");

            throw;
        }
        finally
        {
            ShutdownApplication();
        }
    }

    private static void ShutdownApplication()
    {
        try
        {
            LoggerService.Info("开始执行应用程序关闭流程...");

            Shutdown();
        }
        catch (Exception ex)
        {
            LoggerService.Error($"关闭App时发生错误: {ex.Message}");
        }
        finally
        {
            LoggerService.Shutdown();
            LoggerService.Info("应用程序已完全关闭");
        }
    }

    private static void Shutdown()
    {
        MusicPlayerViewModel.Default.Shutdown();

        ConfigManager.SaveConfig();

        MousePenetrate.ClearCache();
        HotkeyService.ClearCache();
        NavigateService.ClearCache();

        MessageBus.Dispose();
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
    }
}
