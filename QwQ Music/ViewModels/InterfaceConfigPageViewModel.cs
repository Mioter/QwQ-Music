using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Helper;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;

namespace QwQ_Music.ViewModels;

public partial class InterfaceConfigPageViewModel() : NavigationViewModel("界面")
{
    public MusicPlayerViewModel MusicPlayerViewModel { get; set; } = MusicPlayerViewModel.Instance;

    public InterfaceConfig InterfaceConfig { get; set; } = ConfigInfoModel.InterfaceConfig;

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
