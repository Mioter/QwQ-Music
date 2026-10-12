using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Amusing;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Utilities;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public partial class JoyViewModel : ViewModelBase {
    [RelayCommand]
    private static void ClickMeButton() {
        new Love().GenerateHeart().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    [RelayCommand]
    private static void LagButtonClick() { Thread.Sleep(5000); }

    [RelayCommand]
    private static void IceButtonClick() {
        MidiSpring.Spring().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    [RelayCommand]
    private static void ExecuteMemoryCleaner() {
        string info = MemoryCleaner.CleanAndGetInfo();
        NotificationService.Info("提示", info);
    }
}