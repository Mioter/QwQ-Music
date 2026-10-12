using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class ImportConfirmViewModel : ObservableObject, IDialogContext {
    public void Close() { Close(false); }
    public event EventHandler<object?>? RequestClose;


    [RelayCommand]
    private void Confirm() { Close(true); }

    [RelayCommand]
    private void Cancel() { Close(false); }

    public void Close(bool result) { RequestClose?.Invoke(this, result); }
}