using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class ExitConfirmViewModel : ObservableObject, IDialogContext
{
    public bool IsEnablePrompt { get; set; }

    public ClosingBehavior ClosingBehavior { get; private set; }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;

    [RelayCommand]
    private void Hide()
    {
        ClosingBehavior = ClosingBehavior.HideToTray;
        Close();
    }

    [RelayCommand]
    private void Exit()
    {
        ClosingBehavior = ClosingBehavior.Exit;

        Close(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Close();
    }

    public void Close(bool result)
    {
        RequestClose?.Invoke(this, result);
    }
}
