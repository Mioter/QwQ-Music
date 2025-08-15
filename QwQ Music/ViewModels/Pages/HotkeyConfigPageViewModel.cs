using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;
using QwQ_Music.ViewModels.Dialogs;
using Ursa.Controls;
using KeyGestureInput = QwQ_Music.Views.Dialogs.KeyGestureInput;

namespace QwQ_Music.ViewModels.Pages;

public partial class HotkeyConfigPageViewModel : ViewModelBase
{
    public HotkeyConfigPageViewModel()
    {
        InitializeHotkeyItems();
    }

    public HotkeyConfig HotkeyConfig { get; } = ConfigManager.HotkeyConfig;

    /// <summary>
    ///     热键配置项列表
    /// </summary>
    public AvaloniaList<HotkeyItemModel> HotkeyItems { get; } = [];

    /// <summary>
    ///     初始化热键配置项
    /// </summary>
    private void InitializeHotkeyItems()
    {
        HotkeyItems.Clear();

        // 为每个功能创建配置项
        foreach (var function in Enum.GetValues<HotkeyFunction>())
        {
            var item = new HotkeyItemModel(function, HotkeyConfig);
            HotkeyItems.Add(item);
        }
    }

    /*
    /// <summary>
    /// 热键配置项属性变化处理
    /// </summary>
    private void OnHotkeyItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HotkeyItemModel.HasConflict))
            return;

        OnPropertyChanged(nameof(HasAnyConflict));
        OnPropertyChanged(nameof(AllConflictMessages));
    }
    */

    [RelayCommand]
    private async Task AddNewHotkey(HotkeyFunction function)
    {
        var item = HotkeyItems.FirstOrDefault(item => item.Function == function);

        if (item == null)
            return;

        var options = new OverlayDialogOptions
        {
            Title = "添加按键",
            Mode = DialogMode.Question,
            Buttons = DialogButton.OKCancel,
            CanDragMove = true,
            CanResize = false,
        };

        var keyGesture = await OverlayDialog.ShowCustomModal<KeyGestureInput, KeyGestureInputViewModel, KeyGesture>(
            new KeyGestureInputViewModel(item, options.Title),
            options: options
        );

        if (keyGesture != null)
        {
            item.AddKeyGesture(keyGesture);
            HotkeyService.RegisterHotkey(item.Function, keyGesture);
        }
    }

    [RelayCommand]
    private void ResetToDefault()
    {
        HotkeyService.ResetToDefaultHotkeys();

        // 重新初始化所有配置项
        foreach (var item in HotkeyItems)
        {
            item.UpdateKeyGestures();
        }
    }

    [RelayCommand]
    private async Task ClearKeyGestures()
    {
        var result = await MessageBox.ShowOverlayAsync(
            "你真的要清除使用热键配置吗?",
            "警告",
            icon: MessageBoxIcon.Warning,
            button: MessageBoxButton.YesNo
        );

        if (result != MessageBoxResult.Yes)
            return;

        foreach (var item in HotkeyItems)
        {
            item.ClearKeyGestures();
        }

        HotkeyService.ClearKeyGestures();
    }

    [RelayCommand]
    private static void ValidateConfig()
    {
        (bool isValid, var errors) = HotkeyService.ValidateConfiguration();

        NotificationService.Show(
            "热键验证",
            $"热键配置验证结果: {(isValid ? "有效" : "无效")}",
            isValid ? NotificationType.Success : NotificationType.Warning
        );

        if (!isValid)
        {
            LoggerService.Error($"热键配置验证错误！\n信息: {string.Join("\n", errors)}");
        }
    }
}
