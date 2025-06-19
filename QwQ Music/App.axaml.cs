using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using QwQ_Music.ViewModels;
using QwQ_Music.Views;
using Ursa.Controls;

namespace QwQ_Music;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new ApplicationViewModel();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_OnUnhandledException;
        Dispatcher.UIThread.UnhandledException += UIThread_OnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_OnUnobservedTaskException;

        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_OnProcessExit;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            TopLevel = desktop.MainWindow = new MainWindow();
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static void CurrentDomain_OnProcessExit(object? sender, EventArgs e)
    {
        AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_OnProcessExit;

        AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_OnUnhandledException;
        Dispatcher.UIThread.UnhandledException -= UIThread_OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= TaskScheduler_OnUnobservedTaskException;
    }

    private static void TaskScheduler_OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        MessageBox.ShowOverlayAsync(
            $"后台任务出现异常: {e.Exception.Message}",
            "异常",
            icon: MessageBoxIcon.Error,
            button: MessageBoxButton.OKCancel
        );
    }

    private static void UIThread_OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        MessageBox.ShowOverlayAsync(
            $"应用程序出现异常: {e.Exception.Message}",
            "异常",
            icon: MessageBoxIcon.Error,
            button: MessageBoxButton.OKCancel
        );
    }

    private static void CurrentDomain_OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        MessageBox.ShowOverlayAsync(
            $"应用域出现异常: {e.ExceptionObject}",
            "异常",
            icon: MessageBoxIcon.Error,
            button: MessageBoxButton.OKCancel
        );
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins
            .DataValidators.OfType<DataAnnotationsValidationPlugin>()
            .ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    public static TopLevel TopLevel { get; private set; } = null!;
}
