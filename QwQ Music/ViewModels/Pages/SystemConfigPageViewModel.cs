using System.Collections.Frozen;
using QwQ_Music.Common.Helpers;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Models.Enums;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public class SystemConfigPageViewModel() : NavigableViewModel(nameof(SystemConfigPageViewModel)) {
    public SystemConfig Config { get; } = ConfigManager.SystemConfig;

    public I18NService Lang => I18NService.Lang;

    public static LoggerServiceConfig LoggerServiceConfig => ConfigManager.LoggerServiceConfig;

    public static FrozenDictionary<string, ClosingBehavior> ClosingBehaviors =>
        EnumHelper<ClosingBehavior>.ToDictionary();

    public static FrozenDictionary<string, LogLevel> LogLevels { get; } = EnumHelper<LogLevel>.ToDictionary();
}