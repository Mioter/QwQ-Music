using System.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Amusing;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.Pages;

public partial class StatisticsPageViewModel : ViewModelBase
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
}
