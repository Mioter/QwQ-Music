using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;
using QwQ_Music.ViewModels.Dialogs;
using Ursa.Controls;
using KeyGestureInput = QwQ_Music.Views.Dialogs.KeyGestureInput;

namespace QwQ_Music.ViewModels.Pages;

public partial class HotkeyConfigPageViewModel : NavigableViewModel {
    public HotkeyConfigPageViewModel() : base(nameof(HotkeyConfigPageViewModel)) { InitializeHotkeyItems(); }

    public HotkeyConfig HotkeyConfig { get; } = ConfigManager.HotkeyConfig;

    /// <summary>
    ///     热键配置项列表
    /// </summary>
    public AvaloniaList<HotkeyItemModel> HotkeyItems { get; } = [];

    /// <summary>
    ///     初始化热键配置项
    /// </summary>
    private void InitializeHotkeyItems() {
        HotkeyItems.Clear();

        // 为每个功能创建配置项
        foreach (HotkeyFunction function in Enum.GetValues<HotkeyFunction>()) {
            var item = new HotkeyItemModel(function, HotkeyConfig);
            HotkeyItems.Add(item);
        }
    }

    [RelayCommand]
    private void AddNewHotkey(HotkeyFunction function) {
        HotkeyItemModel? item = HotkeyItems.FirstOrDefault(item => item.Function == function);

        if (item == null)
            return;

        var options = new OverlayDialogOptions { Title = "添加按键" };

        OverlayDialog.ShowCustomAsync<KeyGestureInput, KeyGestureInputViewModel, KeyGesture>(
                         new KeyGestureInputViewModel(item, options.Title),
                         options: options)
                     .ContinueWith(task => {
                         if (task is not { IsCompletedSuccessfully: true, Result: { } keyGesture })
                             return;
                         item.AddKeyGesture(keyGesture);
                         HotkeyService.RegisterHotkey(item.Function, keyGesture);
                     })
                     .ConfigureAwait(false);
    }

    [RelayCommand]
    private void ResetToDefault() {
        HotkeyService.ResetToDefaultHotkeys();

        // 重新初始化所有配置项
        foreach (HotkeyItemModel? item in HotkeyItems)
            item.UpdateKeyGestures();
    }

    [RelayCommand]
    private void ClearKeyGestures() {
        MessageBox.ShowAsync(
                      "你真的要清除使用热键配置吗?",
                      "警告",
                      icon: MessageBoxIcon.Warning,
                      button: MessageBoxButton.YesNo)
                  .ContinueWith(task => {
                      if (task is not { IsCompletedSuccessfully: true, Result: MessageBoxResult.Yes })
                          return;
                      foreach (HotkeyItemModel item in HotkeyItems)
                          item.ClearKeyGestures();

                      HotkeyService.ClearKeyGestures();
                  })
                  .ConfigureAwait(false);
    }

    [RelayCommand]
    private static void ValidateConfig() {
        var (isValid, errors) = HotkeyService.ValidateConfiguration();

        NotificationService.Show(
            "热键验证",
            $"热键配置验证结果: {(isValid ? "有效" : "无效")}",
            isValid ? NotificationType.Success : NotificationType.Warning);

        if (!isValid)
            LoggerService.Error($"热键配置验证错误！\n信息: {string.Join("\n", errors)}");
    }
}