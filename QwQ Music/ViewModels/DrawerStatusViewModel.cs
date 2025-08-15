using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;

namespace QwQ_Music.ViewModels;

public partial class DrawerStatusViewModel : ObservableObject
{
    public static DrawerStatusViewModel Default { get; } = new();

    [ObservableProperty] public partial double MusicPlayerTrayYaxisOffset { get; set; }

    [ObservableProperty] public partial double MusicPlayListXaxisOffset { get; set; }

    [ObservableProperty] public partial double MusicAlbumCoverPanelXaxisOffset { get; set; }

    [ObservableProperty] public partial double MusicCoverPageYaxisOffset { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicPlayerTrayWidth), nameof(MusicPlayListWidth))]
    public partial int WindowWidth { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicPlayerPanelHeight))]
    public partial int WindowHeight { get; set; }

    public int MusicPlayerTrayWidth => WindowWidth / 2;

    public int MusicPlayListWidth => IsMusicPlayListVisible ? WindowWidth / 4 : 0;

    public int MusicPlayerPanelHeight => IsMusicPlayerPanelVisible ? WindowHeight : 0;

    [ObservableProperty] public partial bool IsMusicPlayerTrayVisible { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicPlayListWidth))]
    public partial bool IsMusicPlayListVisible { get; set; }

    public bool IsMusicPlayerPanelVisible
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(MusicPlayerPanelHeight));

            object brush;

            if (field)
            {
                brush = MusicPlayerPanelThemeVariant == "Light" ? Brushes.DimGray : Brushes.GhostWhite;
                MusicPlayListThemeVariant = MusicPlayerPanelThemeVariant;
            }
            else
            {
                if (ConfigManager.UiConfig.ThemeConfig.Theme == "Default")
                {
                    var color = ResourceAccessor.Get<Color>("SemiGrey0Color");

                    brush = IsBrightColor(color) ? Brushes.DimGray : Brushes.GhostWhite;
                }
                else
                {
                    brush =
                        ConfigManager.UiConfig.ThemeConfig.Theme == "Light"
                            ? Brushes.DimGray
                            : Brushes.GhostWhite;
                }

                MusicPlayListThemeVariant = ConfigManager.UiConfig.ThemeConfig.Theme;
            }

            ResourceAccessor.Set("CaptionButtonForeground", brush);
        }
    }

    public string MusicPlayerPanelThemeVariant
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            if (!IsMusicPlayerPanelVisible)
                return;

            if (IsMusicPlayListVisible)
            {
                MusicPlayListThemeVariant = field;
            }

            Dispatcher.UIThread.Post(() =>
            {
                ResourceAccessor.Set(
                    "CaptionButtonForeground",
                    field == "Light" ? Brushes.DimGray : Brushes.GhostWhite
                );
            });

            OnPropertyChanged();
        }
    } = "Default";

    [ObservableProperty] public partial string MusicPlayListThemeVariant { set; get; } = "Default";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigationWidth), nameof(IsMusicAlbumCoverTrayVisible))]
    public partial bool IsNavigationExpand { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMusicAlbumCoverTrayVisible))]
    public partial bool IsMusicAlbumCoverPanelPointerOver { get; set; }

    public bool IsMusicAlbumCoverTrayVisible => IsMusicAlbumCoverPanelPointerOver || IsNavigationExpand;

    public double NavigationWidth => IsNavigationExpand ? 130 : 55;

    [RelayCommand]
    private void ShowMusicPlaylist()
    {
        IsMusicPlayListVisible = !IsMusicPlayListVisible;
    }

    [RelayCommand]
    private void ShowMusicPlayerPage()
    {
        IsMusicPlayerPanelVisible = !IsMusicPlayerPanelVisible;
    }

    public static bool IsBrightColor(Color color)
    {
        // 亮度归一化到0~1
        double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;

        return luminance > 0.5;
    }
}
