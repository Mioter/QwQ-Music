using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public partial class UiConfigPageViewModel() : NavigationViewModel("界面")
{
    public UiConfig UiConfig { get; set; } = ConfigManager.UiConfig;

    public static AppResources AppResources => AppResources.Default;

    public string ThemeMode
    {
        get => UiConfig.ThemeConfig.Theme;
        set
        {
            UiConfig.ThemeConfig.Theme = value;

            if (DrawerStatusViewModel.Default.IsMusicPlayerPanelVisible)
                return;

            object brush;

            if (UiConfig.ThemeConfig.Theme == "Default")
            {
                var color = ResourceAccessor.Get<Color>("SemiGrey0Color");

                brush = DrawerStatusViewModel.IsBrightColor(color) ? Brushes.DimGray : Brushes.GhostWhite;
            }
            else
            {
                brush =
                    ConfigManager.UiConfig.ThemeConfig.Theme == "Light"
                        ? Brushes.DimGray
                        : Brushes.GhostWhite;
            }

            ResourceAccessor.Set("CaptionButtonForeground", brush);
        }
    }

    public Dictionary<ColorExtractionAlgorithm, string> ColorExtractionAlgorithms { get; set; } = new()
    {
        [ColorExtractionAlgorithm.KMeans] = "K-means 聚类算法 —— 精确取色",
        [ColorExtractionAlgorithm.OctTree] = "八叉树算法 —— 快速取色",
    };

    public Dictionary<string, string> ThemeModes { get; set; } = new()
    {
        ["Default"] = "跟随系统",
        ["Light"] = "亮色",
        ["Dark"] = "暗色",
    };

    [RelayCommand]
    private static async Task ClearCoverColor()
    {
        await Task.Run(() =>
        {
            foreach (var item in MusicItemManager.Default.MusicItems)
            {
                if (item.CoverColors == null)
                    continue;

                item.CoverColors = null;
            }
        });
    }

    /*
    public static string[] ThemeModes => ["Light", "Dark", "Default"];*/
}
