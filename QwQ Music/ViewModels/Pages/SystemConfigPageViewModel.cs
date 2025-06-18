using QwQ_Music.Definitions.Enums;
using QwQ_Music.Helper;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.Pages;

public class SystemConfigPageViewModel : ViewModelBase
{
    public SystemConfig Config { get; } = ConfigInfoModel.SystemConfig;

    public static ClosingBehaviorMap[] ClosingBehaviors { get; } =
        [
            new("每次都询问", ClosingBehavior.AskAbout),
            new("直接退出", ClosingBehavior.Exit),
            new("隐藏到系统托盘", ClosingBehavior.HideToTray),
        ];

    public static LogLevel[] LogLevels { get; } = EnumHelper<LogLevel>.ToArray();
}

public record ClosingBehaviorMap(string Key, ClosingBehavior Value);
