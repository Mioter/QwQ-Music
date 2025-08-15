using System.Collections.Generic;
using QwQ_Music.Common.Helper;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Models.Enums;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public class SystemConfigPageViewModel : ViewModelBase
{
    public SystemConfig Config { get; } = ConfigManager.SystemConfig;

    public static LoggerServiceConfig LoggerServiceConfig => ConfigManager.LoggerServiceConfig;

    public static DataBaseConfig DataBaseConfig => ConfigManager.DataBaseConfig;

    public static JsonServiceConfig JsonServiceConfig => ConfigManager.JsonServiceConfig;

    public static Dictionary<ClosingBehavior, string> ClosingBehaviors =>
        EnumHelper<ClosingBehavior>.GetValueDescriptionDictionary();

    public static LogLevel[] LogLevels { get; } = EnumHelper<LogLevel>.ToArray();
}

public record ClosingBehaviorMap(string Key, ClosingBehavior Value);
