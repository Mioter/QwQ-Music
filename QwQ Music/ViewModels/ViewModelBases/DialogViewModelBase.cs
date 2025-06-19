using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Avalonia.Shared.Contracts;
using Ursa.Controls;

namespace QwQ_Music.ViewModels.ViewModelBases;

public partial class DialogViewModelBase(OverlayDialogOptions options) : ViewModelBase, IDialogContext
{
    [ObservableProperty]
    public partial string Title { get; set; } = options.Title ?? "(*^▽^*)";

    public bool IsCancel { get; set; }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    protected void Close(bool dialogResult)
    {
        IsCancel = !dialogResult;
        RequestClose?.Invoke(this, dialogResult);
    }

    public event EventHandler<object?>? RequestClose;
}
