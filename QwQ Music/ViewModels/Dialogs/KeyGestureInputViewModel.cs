using System;
using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class KeyGestureInputViewModel(
    HotkeyItemModel hotkeyItem,
    string title,
    KeyGesture? oldKeyGesture = null
) : DataVerifyModelBase, IDialogContext
{
    public string Title { get; set; } = title;

    public KeyGesture? KeyGesture
    {
        get;
        set
        {
            field = value;
            VerifyKeyGesture(value);
        }
    }

    private void VerifyKeyGesture(KeyGesture? gesture)
    {
        ClearErrors(nameof(KeyGesture));

        if (gesture == null)
        {
            AddError(nameof(KeyGesture), "按键不能为空！");

            return;
        }

        if (oldKeyGesture != null && gesture.Key != oldKeyGesture.Key)
        {
            AddError(nameof(KeyGesture), "新按键不能与旧按键相同！");
        }

        // 检查冲突
        string? conflict = HotkeyService.CheckKeyConflict(hotkeyItem.Function, gesture);

        if (conflict != null)
        {
            AddError(nameof(KeyGesture), conflict);

            return;
        }

        if (!hotkeyItem.KeyGestures.Any(g => g.Equals(gesture)))
            return;

        AddError(nameof(KeyGesture), "按键正在被当前功能使用");
    }

    #region 接口实现

    [RelayCommand]
    private void Ok()
    {
        Close(KeyGesture);
    }

    [RelayCommand]
    private void Cancel()
    {
        Close();
    }

    public void Close(object? result)
    {
        RequestClose?.Invoke(this, result);
    }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;

    #endregion
}
