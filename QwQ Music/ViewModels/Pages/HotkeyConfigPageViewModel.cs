using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.UserControls;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.UserControls;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.ViewModels.Pages;

public partial class HotkeyConfigPageViewModel : ViewModelBase
{
    public HotkeyConfig HotkeyConfig { get; } = ConfigInfoModel.HotkeyConfig;

    /// <summary>
    /// 热键配置项列表
    /// </summary>
    public ObservableCollection<HotkeyItemModel> HotkeyItems { get; } = [];

    /*
    /// <summary>
    /// 是否有任何冲突
    /// </summary>
    public bool HasAnyConflict => HotkeyItems.Any(item => item.HasConflict);

    /// <summary>
    /// 所有冲突信息
    /// </summary>
    public string AllConflictMessages =>
        string.Join(
            "\n",
            HotkeyItems.Where(item => item.HasConflict).Select(item => $"{item.FunctionName}: {item.ConflictMessage}")
        );
        */

    public HotkeyConfigPageViewModel()
    {
        InitializeHotkeyItems();

        /*// 监听配置项变化
        foreach (var item in HotkeyItems)
        {
            item.PropertyChanged += OnHotkeyItemPropertyChanged;
        }*/
    }

    /// <summary>
    /// 初始化热键配置项
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

        var model = new KeyGestureInputDialogViewModel(options, item);
        await OverlayDialog.ShowCustomModal<KeyGestureInputDialog, KeyGestureInputDialogViewModel, object>(
            model,
            options: options
        );

        if (!model.IsCancel && model.KeyGesture != null)
        {
            item.AddKeyGesture(model.KeyGesture);
            HotkeyService.RegisterHotkey(item.Function, model.KeyGesture);
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
    private void ClearKeyGestures()
    {
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

        NotificationService.ShowLight(
            new Notification("热键验证", $"热键配置验证结果: {(isValid ? "有效" : "无效")}"),
            isValid ? NotificationType.Success : NotificationType.Warning
        );

        if (!isValid)
        {
            LoggerService.Error($"热键配置验证错误！\n信息: {string.Join("\n", errors)}");
        }
    }
}
