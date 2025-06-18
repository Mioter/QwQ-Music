using System.Linq;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.ViewModelBases;
using Ursa.Controls;

namespace QwQ_Music.ViewModels.UserControls;

public partial class KeyGestureInputDialogViewModel(
    OverlayDialogOptions options,
    HotkeyItemModel hotkeyItem,
    KeyGesture? oldKeyGesture = null
) : DialogViewModelBase(options)
{
    [ObservableProperty]
    public partial string? ErrorMessage { get; set; } = "请输入按键";

    [ObservableProperty]
    public partial KeyGesture? KeyGesture { get; set; }

    partial void OnKeyGestureChanged(KeyGesture? value)
    {
        if (value == null)
        {
            return;
        }

        if (value == oldKeyGesture)
        {
            ErrorMessage = "与旧的按键相同";
            return;
        }

        if (!VerifyKeyGesture(value))
            return;

        ErrorMessage = null;
    }

    private bool VerifyKeyGesture(KeyGesture gesture)
    {
        // 检查冲突
        string? conflict = HotkeyService.CheckKeyConflict(hotkeyItem.Function, gesture);

        if (conflict == null)
        {
            if (!hotkeyItem.KeyGestures.Any(g => g.Equals(gesture)))
                return true;

            ErrorMessage = "按键正在被当前功能使用";
            return false;
        }

        ErrorMessage = conflict;
        return false;
    }

    [RelayCommand]
    private void Ok()
    {
        Close(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Close(false);
    }
}
