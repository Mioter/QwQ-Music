using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public partial class ConfigPageViewModel() : NavigableViewModel(nameof(ConfigPageViewModel)) {

    [ObservableProperty]
    public partial DateTime? LastSavedTime { get; set; } = null;

    [RelayCommand]
    private void SaveConfigImmediately() {
        ConfigManager.SaveConfig();
        DateTime now = DateTime.Now;
        ToastService.Success($"保存成功！在{now:HH:mm:ss}");
        LastSavedTime = now;
    }
}