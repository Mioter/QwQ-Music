using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Amusing;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Utilities;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public partial class MiaomiaoLittleToyPageViewModel : ViewModelBase
{
    [RelayCommand]
    private static async Task ClickMeButton()
    {
        await new Love().GenerateHeart();
    }

    [RelayCommand]
    private static void LagButtonClick()
    {
        Thread.Sleep(5000);
    }

    [RelayCommand]
    private static async Task IceButtonClick()
    {
        await MidiSpring.Spring();
    }

    [RelayCommand]
    private static void ExecuteMemoryCleaner()
    {
        string info = MemoryCleaner.CleanAndGetInfo();
        NotificationService.Info("提示", info);
    }
}
