using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using QwQ_Music.Common;
using QwQ_Music.Common.Services;
using QwQ_Music.ViewModels;
using QwQ_Music.Windows;
using Ursa.Controls;

namespace QwQ_Music;

public class App : Application {
    public static MainWindow? TopLevel { get; private set; }
    public static Assembly CurrentAssembly { get; } = Assembly.GetExecutingAssembly();

    public override void Initialize() {
        AvaloniaXamlLoader.Load(this);
        AppResources.Default.Initialize();
        DataContext = new ApplicationViewModel();
    }

    public override void OnFrameworkInitializationCompleted() {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_OnUnhandledException;
        Dispatcher.UIThread.UnhandledException += UIThread_OnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_OnUnobservedTaskException;

        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_OnProcessExit;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = TopLevel = new MainWindow { DataContext = new MainWindowViewModel() };
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
        if (Program.OpenWithFiles is not null)
            Task.Run(async Task? () => await AudioFileService.FileOpenAsync(Program.OpenWithFiles)
                                                             .ConfigureAwait(false))
                .ConfigureAwait(false);

        DesktopPlayControlService.Start();
        DesktopLyricsService.Create();
    }

    private static void CurrentDomain_OnProcessExit(object? sender, EventArgs e) {
        AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_OnProcessExit;

        AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_OnUnhandledException;
        Dispatcher.UIThread.UnhandledException -= UIThread_OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= TaskScheduler_OnUnobservedTaskException;
    }

    private static volatile int _errors;

    private static void ShowExceptionOverlay(
        string message,
        string title = "异常",
        MessageBoxIcon icon = MessageBoxIcon.Error,
        MessageBoxButton button = MessageBoxButton.OK) {
        if (Interlocked.Increment(ref _errors) == 11) {
            throw new InvalidOperationException();
        }

        MessageBox.ShowAsync(message, title, icon: icon, button: button)
                  .ContinueWith(_ => Interlocked.Decrement(ref _errors))
                  .ConfigureAwait(false);
    }

    private static void TaskScheduler_OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) {
        HandleException($"后台任务出现异常: {e.Exception.Message}", e.Exception);
    }

    private static void UIThread_OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
        HandleException($"应用程序出现异常: {e.Exception.Message}", e.Exception);
        e.Handled = true;
    }

    private static void CurrentDomain_OnUnhandledException(object sender, UnhandledExceptionEventArgs e) {
        // LoggerService.Error("应用域错误: ", (e.ExceptionObject as Exception)!);
    }

    private static void HandleException(string message, Exception? exception = null) {
        string fullMessage = exception != null ? $"{message}\n\n详细信息:\n{exception}" : message;

        if (Dispatcher.UIThread.CheckAccess())
            ShowExceptionOverlay(fullMessage);
        else
            Dispatcher.UIThread.Post(() => ShowExceptionOverlay(fullMessage));

        LoggerService.Error(fullMessage);
    }
}