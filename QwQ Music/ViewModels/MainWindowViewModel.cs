using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using static QwQ_Music.Models.LanguageModel;

namespace QwQ_Music.ViewModels;

public partial class MainWindowViewModel() : NavigationViewModel("窗口")
{
    public static string MusicName => Lang[nameof(MusicName)];
    public static string ClassificationName => Lang[nameof(ClassificationName)];
    public static string StatisticsName => Lang[nameof(StatisticsName)];
    public static string SettingsName => Lang[nameof(SettingsName)];

    /*
    public ObservableCollection<ViewModelBase> PagesModels { get; } = [
        new MusicPageViewModel(),
        new ClassificationPageViewModel(),
        new StatisticsPageViewModel(),
        new ConfigPageViewModel(),
    ];
    */

    [ObservableProperty]
    private bool _isMusicPlayerTrayVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBackgroundLayerVisible))]
    private bool _isMusicPlayListVisible;

    [ObservableProperty]
    private bool _isNavigationExpand;

    [ObservableProperty]
    private bool _isWindowMaximizedOrFullScreen;

    [ObservableProperty]
    private WindowState _mainWindowState;

    [ObservableProperty]
    private double _musicPlayerTrayYaxisOffset;

    [ObservableProperty]
    private double _musicPlayListXaxisOffset;

    public bool IsBackgroundLayerVisible => false;

    partial void OnMainWindowStateChanged(WindowState value) =>
        IsWindowMaximizedOrFullScreen = MainWindowState is WindowState.Maximized or WindowState.FullScreen;

    [RelayCommand]
    private void ShowMusicPlaylist() => IsMusicPlayListVisible = !IsMusicPlayListVisible;

    [RelayCommand]
    private void GlobalButtonClick() => IsMusicPlayListVisible = false;
}
