using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Helper;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Services;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.UserControls;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.Pages;

public partial class InterfaceConfigPageViewModel() : NavigationViewModel("界面")
{
    public MusicPlayerViewModel MusicPlayerViewModel { get; set; } = MusicPlayerViewModel.Instance;

    public InterfaceConfig InterfaceConfig { get; set; } = ConfigManager.InterfaceConfig;

    public string LightDarkMode
    {
        get => InterfaceConfig.ThemeConfig.LightDarkMode;
        set
        {
            InterfaceConfig.ThemeConfig.LightDarkMode = value;

            if (MainWindowViewModel.Instance.IsMusicCoverPageVisible)
                return;

            object brush;
            if (ConfigManager.InterfaceConfig.ThemeConfig.LightDarkMode == "Default")
            {
                var color = ResourceDictionaryManager.Get<Color>("SemiGrey0Color");

                brush = MainWindowViewModel.IsBrightColor(color) ? Brushes.DimGray : Brushes.GhostWhite;
            }
            else
            {
                brush =
                    ConfigManager.InterfaceConfig.ThemeConfig.LightDarkMode == "Light"
                        ? Brushes.DimGray
                        : Brushes.GhostWhite;
            }

            ResourceDictionaryManager.Set("CaptionButtonForeground", brush);

            MusicPlayListViewModel.Instance.ThemeVariant = ConfigManager.InterfaceConfig.ThemeConfig.LightDarkMode;
        }
    }

    [RelayCommand]
    private async Task ClearCoverColor()
    {
        await Task.Run(() =>
        {
            foreach (var item in MusicPlayerViewModel.MusicItems)
            {
                if (item.CoverColors == null)
                    continue;
                item.CoverColors = null;
            }
        });
    }

    public static ColorExtractionAlgorithm[] ColorExtractionAlgorithms =>
        EnumHelper<ColorExtractionAlgorithm>.ToArray();

    public static string[] LightDarkModes => ["Light", "Dark", "Default"];
}
