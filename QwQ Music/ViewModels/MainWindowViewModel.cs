using Avalonia.Input;
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
    [NotifyPropertyChangedFor(nameof(MusicPlayerTrayWidth), nameof(MusicPlayListWidth))]
    public partial int WindowWidth { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicPlayerPageHeight))]
    public partial int WindowHeight { get; set; }

    public int MusicPlayerTrayWidth => WindowWidth / 2;

    public int MusicPlayListWidth => IsMusicPlayListVisible ? WindowWidth / 4 : 0;

    public int MusicPlayerPageHeight => IsMusicPlayerPageVisible ? WindowHeight : 0;

    [ObservableProperty]
    public partial bool IsMusicPlayerTrayVisible { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBackgroundLayerVisible), nameof(MusicPlayListWidth))]
    public partial bool IsMusicPlayListVisible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicPlayerPageHeight))]
    public partial bool IsMusicPlayerPageVisible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigationWidth))]
    public partial bool IsNavigationExpand { get; set; }

    [ObservableProperty]
    public partial double MusicPlayerTrayYaxisOffset { get; set; }

    [ObservableProperty]
    public partial double MusicPlayListXaxisOffset { get; set; }

    [ObservableProperty]
    public partial double MusicPlayerPageYaxisOffset { get; set; }

    public double NavigationWidth => IsNavigationExpand ? 150 : 75;

    public static bool IsBackgroundLayerVisible => false;

    [RelayCommand]
    private void ShowMusicPlaylist() => IsMusicPlayListVisible = !IsMusicPlayListVisible;

    [RelayCommand]
    private void ShowMusicPlayerPage() => IsMusicPlayerPageVisible = !IsMusicPlayerPageVisible;

    [RelayCommand]
    private void GlobalButtonClick() => IsMusicPlayListVisible = false;

    [RelayCommand]
    private void PointerWheelChanged(PointerWheelEventArgs e)
    {
        IsMusicPlayerTrayVisible = e.Delta.Y switch
        {
            // 检查滚动的方向
            > 0 =>
            // 向上滚动
            true,
            < 0 =>
            // 向下滚动
            false,
            _ => IsMusicPlayerTrayVisible,
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
