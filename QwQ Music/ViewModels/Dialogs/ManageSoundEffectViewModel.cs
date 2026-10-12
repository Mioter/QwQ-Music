using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using QwQ_Music.Common.Managers;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class ManageSoundEffectViewModel : DataVerifyModelBase, IDialogContext {
    public Dictionary<string, bool> BuiltInSoundEffects { get; } =
        ConfigManager.SoundModifierConfig.SoundEffectConfig.BuiltInSoundEffects;


    public void Close() { RequestClose?.Invoke(this, null); }

    public event EventHandler<object?>? RequestClose;


    [RelayCommand]
    private void ChangeEnabledStatus(string soundEffectName) {
        BuiltInSoundEffects[soundEffectName] = !BuiltInSoundEffects[soundEffectName];
    }

    [RelayCommand]
    private void Ok() { Close(true); }

    [RelayCommand]
    private void Cancel() { Close(); }

    public void Close(bool result) { RequestClose?.Invoke(this, result); }
}