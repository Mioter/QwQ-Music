using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.ViewModels.Bases;
using QwQ_Music.Views.Pages;

namespace QwQ_Music.ViewModels;

public partial class MainViewViewModel : NavigableViewModel {
    public MainViewViewModel() : base(nameof(MainViewViewModel)) {
        // 注册热键功能
        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.NextPage,
            () => {
                if (CanGoForward)
                    ViewForwardCommand.Execute(null);
            });

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.PrevPage,
            () => {
                if (CanGoBack)
                    ViewBackwardCommand.Execute(null);
            });
    }

    public static DrawerManager DrawerManager => DrawerManager.Instance;

    public static string LibraryV => I18NService.Lang.Translation[nameof(LibraryV), "NavBar"];

    public static string ClassificationV => I18NService.Lang.Translation[nameof(ClassificationV), "NavBar"];

    public static string OtherV => I18NService.Lang.Translation[nameof(OtherV), "NavBar"];

    public static string SettingsV => I18NService.Lang.Translation[nameof(SettingsV), "NavBar"];

    public AvaloniaList<Control> Pages { get; } = [
        new MusicLibraryPage(), new ClassificationPage(), new OtherPage(), new ConfigMainPage()
    ];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewBackwardCommand))]
    public partial bool CanGoBack { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewForwardCommand))]
    public partial bool CanGoForward { get; set; }

    protected override void OnNavigateTo(int index) {
        base.OnNavigateTo(index);

        if (index >= Pages.Count || index < 0)
            return;

        CanGoBack = NavigateService.CanGoBack;
        CanGoForward = NavigateService.CanGoForward;

        // TODO TIER 2
        // Refactor Navigate Service.
        // Further Consideration Needed.
    }

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void ViewForward() {
        NavigateService.GoForward();
        CanGoBack = NavigateService.CanGoBack;
        CanGoForward = NavigateService.CanGoForward;
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void ViewBackward() {
        NavigateService.GoBack();
        CanGoBack = NavigateService.CanGoBack;
        CanGoForward = NavigateService.CanGoForward;
    }
}