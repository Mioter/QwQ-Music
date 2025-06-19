using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Definitions.Enums;

namespace QwQ_Music.Models.ConfigModels;

public partial class SystemConfig : ObservableObject
{
    public bool IsDebugMode { get; set; }

    [ObservableProperty]
    public partial ClosingBehavior ClosingBehavior { get; set; } = ClosingBehavior.AskAbout;
}
