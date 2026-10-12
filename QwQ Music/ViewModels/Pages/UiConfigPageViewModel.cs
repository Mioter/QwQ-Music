using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;
using MusicItemsManager = QwQ_Music.Common.Managers.MusicItemsManager;

namespace QwQ_Music.ViewModels.Pages;

public partial class UiConfigPageViewModel() : NavigableViewModel("界面") {
    public UiConfig UiConfig { get; } = ConfigManager.UiConfig;

    public static AppResources AppResources => AppResources.Default;

    public string ThemeMode {
        get => UiConfig.ThemeConfig.Theme;
        set {
            UiConfig.ThemeConfig.Theme = value;

            if (DrawerManager.Instance.IsMusicPlayerPanelVisible)
                return;

            IBrush brush;

            if (UiConfig.ThemeConfig.Theme == "Default") {
                var color = ResourceAccessor.Get<Color>("SemiGrey0Color");

                brush = DrawerManager.IsBrightColor(color) ? Brushes.DimGray : Brushes.GhostWhite;
            } else {
                brush = ConfigManager.UiConfig.ThemeConfig.Theme == "Light" ? Brushes.DimGray : Brushes.GhostWhite;
            }

            ResourceAccessor.Set("CaptionButtonForeground", brush);
        }
    }

    public Dictionary<ColorExtractionAlgorithm, string> ColorExtractionAlgorithms { get; set; } = new() {
        [ColorExtractionAlgorithm.KMeans] = "K-means 聚类算法 —— 精确取色", [ColorExtractionAlgorithm.OctTree] = "八叉树算法 —— 快速取色"
    };

    public Dictionary<string, string> ThemeModes { get; set; } =
        new() { ["Default"] = "跟随系统", ["Light"] = "亮色", ["Dark"] = "暗色" };

    [RelayCommand]
    private static void ClearCoverColor() {
        Task.Run(() => {
                foreach (MusicItemModel item in MusicItemsManager.All.MusicItems.Values) {
                    if (item.CoverColors == null)
                        continue;

                    item.CoverColors = null;
                }
            })
            .ContinueWith(LoggerService.HandleException)
            .ConfigureAwait(false)
            .GetAwaiter()
            .OnCompleted(() => NotificationService.Info("封面颜色缓存已经清空，切换音乐时将会重新提取并缓存~"));
    }
}