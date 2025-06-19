using System;
using Avalonia;
using QwQ_Music.Services;
using QwQ_Music.Services.Audio;
using QwQ_Music.ViewModels;

namespace QwQ_Music;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            AudioEngineManager.Create();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            LoggerService.Error($"捕捉到未处理异常:\n {e.Message}\n {e.StackTrace}");
            ApplicationViewModel.OnShutdown().Wait();
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
    }
}
