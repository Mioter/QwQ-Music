using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Services;
using QwQ_Music.ViewModels.Bases;
using QwQ_Music.ViewModels.Pages;
using QwQ_Music.Views.Pages;
using static QwQ_Music.Models.LanguageModel;

namespace QwQ_Music.ViewModels;

public partial class MainViewModel : NavigationViewModel
{
    public MainViewModel()
        : base("窗口")
    {
        // 注册热键功能
        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.ViewForward,
            () =>
            {
                if (CanGoForward)
                    ViewForwardCommand.Execute(null);
            }
        );

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.ViewBackward,
            () =>
            {
                if (CanGoBack)
                    ViewBackwardCommand.Execute(null);
            }
        );
    }

    public static DrawerStatusViewModel DrawerStatusViewModel => DrawerStatusViewModel.Default;

    public static string MusicName => Lang[nameof(MusicName)];

    public static string ClassificationName => Lang[nameof(ClassificationName)];

    public static string OtherName => Lang[nameof(OtherName)];

    public static string SettingsName => Lang[nameof(SettingsName)];

    public AvaloniaList<Control> Pages { get; } =
    [
        new AllMusicPage
        {
            DataContext = new AllMusicPageViewModel(),
        },
        new ClassificationPage
        {
            DataContext = new ClassificationPageViewModel(),
        },
        new OtherPage
        {
            DataContext = new OtherPageViewModel(),
        },
        new ConfigMainPage
        {
            DataContext = new ConfigPageViewModel(),
        },
    ];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewBackwardCommand))]
    public partial bool CanGoBack { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewForwardCommand))]
    public partial bool CanGoForward { get; set; }

    protected override void OnNavigateTo(int index)
    {
        base.OnNavigateTo(index);

        if (index >= Pages.Count || index < 0)
            return;

        CanGoBack = NavigateService.CanGoBack;
        CanGoForward = NavigateService.CanGoForward;
    }

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void ViewForward()
    {
        NavigateService.GoForward();
        CanGoBack = NavigateService.CanGoBack;
        CanGoForward = NavigateService.CanGoForward;
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void ViewBackward()
    {
        NavigateService.GoBack();
        CanGoBack = NavigateService.CanGoBack;
        CanGoForward = NavigateService.CanGoForward;
    }

    [RelayCommand]
    private static void PointerWheelChanged(PointerWheelEventArgs e)
    {
        DrawerStatusViewModel.IsMusicPlayerTrayVisible = e.Delta.Y switch
        {
            // 检查滚动的方向
            > 0 =>

                // 向上滚动
                true,
            < 0 =>

                // 向下滚动
                false,
            _ => DrawerStatusViewModel.IsMusicPlayerTrayVisible,
        };

        /*
        // 如果支持水平滚动，则可以检查Delta.X
        if (e.Delta.X != 0)
        {
            Console.WriteLine($"Mouse wheel scrolled horizontally by {e.Delta.X}.");
        }
        */
    }
}
