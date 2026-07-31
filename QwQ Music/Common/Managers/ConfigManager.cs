using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.ConfigIO;
using QwQ_Music.Models.ConfigModels;
using UserConfigJsonSerializerContext = QwQ_Music.Models.ConfigModels.UserConfigJsonSerializerContext;

namespace QwQ_Music.Common.Managers;

public static class ConfigManager {
    private static readonly string _serviceConfigIniPath = Path.Combine(
        StaticConfig.ConfigSavePath,
        $"{nameof(ServiceConfig).ToLower()}.QwQ.ini");

    private static JsonConfigService JsonConfigService =>
        new(UserConfigJsonSerializerContext.Default, StaticConfig.ConfigSavePath);

    public static UserConfig UserConfig { get; } =
        JsonConfigService.Load<UserConfig>(nameof(UserConfig).ToLower()) ?? new UserConfig();

    public static SystemConfig SystemConfig => UserConfig.SystemConfig;

    public static PlayerConfig PlayerConfig => UserConfig.PlayerConfig;

    public static DesktopControlConfig DesktopControlConfig => UserConfig.DesktopControlConfig;
    
    public static LyricConfig LyricConfig => UserConfig.LyricConfig;

    public static SoundModifierConfig SoundModifierConfig => UserConfig.SoundModifierConfig;

    public static UiConfig UiConfig => UserConfig.UiConfig;

    public static HotkeyConfig HotkeyConfig => UserConfig.HotkeyConfig;

    public static ServiceConfig ServiceConfig { get; } = GetServiceConfig();

    public static LoggerServiceConfig LoggerServiceConfig => ServiceConfig.LoggerServiceConfig;

    private static ServiceConfig GetServiceConfig() {
        var config = new ServiceConfig();
        var ini = new IniConfigService(_serviceConfigIniPath);

        // LoggerServiceConfig
        config.LoggerServiceConfig.RetryCount =
            int.TryParse(ini.Get("RetryCount", "LoggerService"), out int lrc) ? lrc : 3;

        config.LoggerServiceConfig.Level = Enum.TryParse(ini.Get("Level", "LoggerService"), out LogLevel level) ?
            level :
            LogLevel.Basic;

        return config;
    }

    public static void SaveConfig() {
        try {
            JsonConfigService.Save(UserConfig, nameof(UserConfig).ToLower());

            SaveServiceConfig();
        } catch (Exception e) {
            LoggerService.Error($"保存配置文件时发生错误 : {e.Message}");
        }
    }

    private static void SaveServiceConfig() {
        var ini = new IniConfigService();

        ini.Set("RetryCount", ServiceConfig.LoggerServiceConfig.RetryCount.ToString(), "LoggerService");
        ini.Set("Level", ServiceConfig.LoggerServiceConfig.Level.ToString(), "LoggerService");

        ini.Save(_serviceConfigIniPath);
    }
}