using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.Models.ConfigModels;

public partial class SystemConfig : ObservableObject
{
    [ObservableProperty] public partial bool IsDebugMode { get; set; }

    [ObservableProperty] public partial ClosingBehavior ClosingBehavior { get; set; } = ClosingBehavior.AskAbout;
}
