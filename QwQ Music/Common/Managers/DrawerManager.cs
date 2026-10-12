using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Services;

namespace QwQ_Music.Common.Managers;

public sealed partial class DrawerManager : ObservableObject {
    private DrawerManager() { }
    public static DrawerManager Instance { get; } = new();

    [ObservableProperty]
    public partial double MusicPlayerTrayYAxisOffset { get; set; }

    [ObservableProperty]
    public partial double MusicPlaylistXAxisOffset { get; set; }

    [ObservableProperty]
    public partial double MusicAlbumCoverPanelXAxisOffset { get; set; }

    [ObservableProperty]
    public partial double MusicCoverPageYAxisOffset { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicPlayerTrayWidth), nameof(MusicPlaylistWidth))]
    public partial int WindowWidth { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicPlayerPanelHeight))]
    public partial int WindowHeight { get; set; }

    public int MusicPlayerTrayWidth => WindowWidth / 2;

    public int MusicPlaylistWidth => IsMusicPlaylistVisible ? WindowWidth / 4 : 0;

    public int MusicPlayerPanelHeight => IsMusicPlayerPanelVisible ? WindowHeight : 0;

    [ObservableProperty]
    public partial bool IsMusicPlayerTrayVisible { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicPlaylistWidth))]
    public partial bool IsMusicPlaylistVisible { get; set; }

    public bool IsMusicPlayerPanelVisible {
        get;
        set {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(MusicPlayerPanelHeight));

            IBrush brush;

            if (field) {
                brush = MusicPlayerPanelThemeVariant == "Light" ? Brushes.DimGray : Brushes.GhostWhite;
                MusicPlaylistThemeVariant = MusicPlayerPanelThemeVariant;
            } else {
                if (ConfigManager.UiConfig.ThemeConfig.Theme == "Default") {
                    var color = ResourceAccessor.Get<Color>("SemiGrey0Color");

                    brush = IsBrightColor(color) ? Brushes.DimGray : Brushes.GhostWhite;
                } else {
                    brush = ConfigManager.UiConfig.ThemeConfig.Theme == "Light" ? Brushes.DimGray : Brushes.GhostWhite;
                }

                MusicPlaylistThemeVariant = ConfigManager.UiConfig.ThemeConfig.Theme;
            }

            ResourceAccessor.Set("CaptionButtonForeground", brush);
        }
    }

    public string MusicPlayerPanelThemeVariant {
        get;
        set {
            if (!SetProperty(ref field, value))
                return;

            if (!IsMusicPlayerPanelVisible)
                return;

            MusicPlaylistThemeVariant = field;

            Dispatcher.UIThread.Post(() => {
                ResourceAccessor.Set(
                    "CaptionButtonForeground",
                    field == "Light" ? Brushes.DimGray : Brushes.GhostWhite);
            });

            OnPropertyChanged();
        }
    } = "Default";

    [ObservableProperty]
    public partial string MusicPlaylistThemeVariant { get; set; } = "Default";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigationWidth), nameof(IsMusicAlbumCoverTrayVisible))]
    public partial bool IsNavigationExpand { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMusicAlbumCoverTrayVisible))]
    public partial bool IsMusicAlbumCoverPanelPointerOver { get; set; }

    public bool IsMusicAlbumCoverTrayVisible => IsMusicAlbumCoverPanelPointerOver || IsNavigationExpand;

    public double NavigationWidth => IsNavigationExpand ? 130 : 55;

    [RelayCommand]
    private void ToggleMusicPlaylist() { IsMusicPlaylistVisible = !IsMusicPlaylistVisible; }

    [RelayCommand]
    private void ToggleMusicPlayerPage() { IsMusicPlayerPanelVisible = !IsMusicPlayerPanelVisible; }


    [RelayCommand]
    private void PointerWheelChanged(PointerWheelEventArgs e) {
        IsMusicPlayerTrayVisible = e.Delta.Y switch {
            // 检查滚动的方向
            > 0 =>

                // 向上滚动
                true,
            < 0 =>

                // 向下滚动
                false,
            _ => IsMusicPlayerTrayVisible
        };

        /*
        // 如果支持水平滚动，则可以检查Delta.X
        if (e.Delta.X != 0)
        {
            Console.WriteLine($"Mouse wheel scrolled horizontally by {e.Delta.X}.");
        }
        */
    }

    public static bool IsBrightColor(Color color) {
        // 亮度归一化到0~1
        double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;

        return luminance > 0.5;
    }
}