using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions.Enums;
using QwQ_Music.ViewModels.ViewModelBases;
using Ursa.Controls;

namespace QwQ_Music.ViewModels.UserControls;

public partial class ProgramExitConfirmViewModel(OverlayDialogOptions options) : DialogViewModelBase(options)
{
    public bool IsEnablePrompt { get; set; }

    public ClosingBehavior ClosingBehavior { get; set; }

    [RelayCommand]
    private void Hide()
    {
        ClosingBehavior = ClosingBehavior.HideToTray;
        Close(true);
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
        Close(false);
    }
}
