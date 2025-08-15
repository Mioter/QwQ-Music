using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Dialogs;
using Ursa.Controls;
using KeyGestureInput = QwQ_Music.Views.Dialogs.KeyGestureInput;

namespace QwQ_Music.Models;

/// <summary>
///     热键配置项，用于在View中绑定单个功能的热键配置
/// </summary>
public partial class HotkeyItemModel : ObservableObject
{
    private readonly HotkeyConfig _config;

    public HotkeyItemModel(HotkeyFunction function, HotkeyConfig config)
    {
        Function = function;
        _config = config;
        FunctionName = HotkeyService.GetFunctionDescription(function);

        // 初始化按键列表
        UpdateKeyGestures();
    }

    /// <summary>
    ///     功能名称
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    ///     功能枚举
    /// </summary>
    public HotkeyFunction Function { get; }

    /// <summary>
    ///     按键列表
    /// </summary>
    public AvaloniaList<KeyGesture> KeyGestures { get; } = [];

    /// <summary>
    ///     添加按键
    /// </summary>
    /// <param name="gesture">按键组合</param>
    /// <returns>是否添加成功</returns>
    public bool AddKeyGesture(KeyGesture gesture)
    {
        KeyGestures.Add(gesture);

        return true;
    }

    /// <summary>
    ///     移除按键
    /// </summary>
    /// <param name="gesture">按键组合</param>
    /// <returns>是否移除成功</returns>
    [RelayCommand]
    public void RemoveHotkey(KeyGesture gesture)
    {
        KeyGestures.Remove(gesture);
        HotkeyService.RemoveHotkey(Function, gesture);
    }

    /// <summary>
    ///     修改按键命令
    /// </summary>
    /// <param name="oldKeyGesture"></param>
    [RelayCommand]
    private async Task ModifyGesture(KeyGesture oldKeyGesture)
    {
        var options = new OverlayDialogOptions
        {
            Title = "修改按键",
            Mode = DialogMode.Question,
            Buttons = DialogButton.OKCancel,
            CanDragMove = true,
            CanResize = false,
        };

        var keyGesture = await OverlayDialog.ShowCustomModal<KeyGestureInput, KeyGestureInputViewModel, KeyGesture>(
            new KeyGestureInputViewModel(this, options.Title, oldKeyGesture),
            options: options
        );

        if (keyGesture != null)
        {
            int index = KeyGestures.IndexOf(oldKeyGesture);

            if (index == -1)
                return;

            KeyGestures[index] = keyGesture;
            HotkeyService.ModifyHotkey(Function, oldKeyGesture, keyGesture);
        }
    }

    /// <summary>
    ///     清空所有按键
    /// </summary>
    public void ClearKeyGestures()
    {
        KeyGestures.Clear();
    }

    /// <summary>
    ///     更新按键列表
    /// </summary>
    public void UpdateKeyGestures()
    {
        KeyGestures.Clear();

        if (!_config.FunctionToKeyMap.TryGetValue(Function, out var gestures))
            return;

        foreach (var gesture in gestures)
        {
            KeyGestures.Add(gesture.ToKeyGesture());
        }
    }
}
