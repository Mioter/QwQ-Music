using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Helper;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.Pages;

public partial class InterfaceConfigPageViewModel() : NavigationViewModel("界面")
{
    public MusicPlayerViewModel MusicPlayerViewModel { get; set; } = MusicPlayerViewModel.Instance;

    public InterfaceConfig InterfaceConfig { get; set; } = ConfigManager.InterfaceConfig;

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
}
