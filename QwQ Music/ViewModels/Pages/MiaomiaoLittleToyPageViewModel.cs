using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Amusing;
using QwQ_Music.Services;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.ViewModelBases;
using Notification = Ursa.Controls.Notification;

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
        NotificationService.ShowLight(new Notification("提示", info), NotificationType.Information);
    }
}
