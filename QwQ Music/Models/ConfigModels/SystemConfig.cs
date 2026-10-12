using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.Models.ConfigModels;

public partial class SystemConfig : ObservableObject {
    [ObservableProperty]
    public partial bool IsPlayOnStart { get; set; } = false;

    [ObservableProperty]
    public partial bool KeepSystemAwake { get; set; } = true;

    [ObservableProperty]
    public partial bool KeepDisplay { get; set; } = false;

    public string Language {
        get;
        set {
            if (SetProperty(ref field, value)) {
                I18NService.Lang.LoadLanguage(value);
            }
        }
    } = "zh_CN";


    [ObservableProperty]
    public partial bool IsDebugMode { get; set; }

    [ObservableProperty]
    public partial ClosingBehavior ClosingBehavior { get; set; } = ClosingBehavior.Ask;
}