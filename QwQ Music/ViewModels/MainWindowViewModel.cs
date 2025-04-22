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
        new AllMusicPageViewModel(),
        new ClassificationPageViewModel(),
        new StatisticsPageViewModel(),
        new ConfigPageViewModel(),
    ];
    */


    public int WindowWidth
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(MusicPlayerTrayWidth));
            OnPropertyChanged(nameof(MusicPlayListWidth));
        }
    }

    public int WindowHeight
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(MusicCoverPageHeight));
        }
    }

    public int MusicPlayerListHeight
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public int MusicPlayerTrayWidth => WindowWidth / 2;

    public int MusicPlayListWidth => IsMusicPlayListVisible ? WindowWidth / 4 : 0;

    public int MusicCoverPageHeight => IsMusicCoverPageVisible ? WindowHeight : 0;

    [ObservableProperty]
    public partial bool IsMusicPlayerTrayVisible { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBackgroundLayerVisible), nameof(MusicPlayListWidth))]
    public partial bool IsMusicPlayListVisible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicCoverPageHeight))]
    public partial bool IsMusicCoverPageVisible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigationWidth),nameof(IsMusicAlbumCoverTrayVisible))]
    public partial bool IsNavigationExpand { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMusicAlbumCoverTrayVisible))]
    public partial bool IsMusicAlbumCoverPanelPointerOver { get; set; }

    public bool IsMusicAlbumCoverTrayVisible => IsMusicAlbumCoverPanelPointerOver || IsNavigationExpand;

    [ObservableProperty]
    public partial double MusicPlayerTrayYaxisOffset { get; set; }

    [ObservableProperty]
    public partial double MusicPlayListXaxisOffset { get; set; }

    [ObservableProperty]
    public partial double MusicAlbumCoverPanelXaxisOffset { get; set; }
    
    [ObservableProperty]
    public partial double MusicCoverPageYaxisOffset { get; set; }

    public double NavigationWidth => IsNavigationExpand ? 150 : 75;

    public static bool IsBackgroundLayerVisible => false;

    [RelayCommand]
    private void ShowMusicPlaylist() => IsMusicPlayListVisible = !IsMusicPlayListVisible;

    [RelayCommand]
    private void ShowMusicPlayerPage() => IsMusicCoverPageVisible = !IsMusicCoverPageVisible;

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
