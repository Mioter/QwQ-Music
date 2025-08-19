using System;
using System.IO;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.ConfigIO;
using QwQ_Music.Models.ConfigModels;
using UserConfigJsonSerializerContext = QwQ_Music.Models.ConfigModels.UserConfigJsonSerializerContext;

namespace QwQ_Music.Common.Manager;

public static class ConfigManager
{
    // 使用Lazy<T>实现真正的懒加载
    private static readonly Lazy<UserConfig> _config = new(() =>
        JsonConfigService.Load<UserConfig>(nameof(UserConfig).ToLower(), UserConfigJsonSerializerContext.Default)
     ?? new UserConfig()
    );

    private static readonly Lazy<ServiceConfig> _serviceConfig = new(() =>
    {
        var config = new ServiceConfig();
        var ini = new IniConfigService(_serviceConfigIniPath);

        // JsonServiceConfig
        config.JsonServiceConfig.EnableBackup = ini.Get("EnableBackup", "JsonService")?.ToLower() == "true";

        config.JsonServiceConfig.EnablePerformanceLogging =
            ini.Get("EnablePerformanceLogging", "JsonService")?.ToLower() != "false";

        config.JsonServiceConfig.MaxBackupCount = int.TryParse(ini.Get("MaxBackupCount", "JsonService"), out int jmax)
            ? jmax
            : 3;

        // LoggerServiceConfig
        config.LoggerServiceConfig.IsKeepOpen = ini.Get("IsKeepOpen", "LoggerService")?.ToLower() != "false";

        config.LoggerServiceConfig.RetryCount = int.TryParse(ini.Get("RetryCount", "LoggerService"), out int lrc)
            ? lrc
            : 3;

        string? levelStr = ini.Get("Level", "LoggerService");

        if (Enum.TryParse(levelStr, out LogLevel level))
            config.LoggerServiceConfig.Level = level;

        return config;
    });

    private static readonly string _serviceConfigIniPath = Path.Combine(
        StaticConfig.ConfigSavePath,
        $"{nameof(ServiceConfig).ToLower()}.QwQ.ini"
    );

    public static UserConfig UserConfig => _config.Value;

    public static SystemConfig SystemConfig => UserConfig.SystemConfig;

    public static PlayerConfig PlayerConfig => UserConfig.PlayerConfig;

    public static LyricConfig LyricConfig => UserConfig.LyricConfig;

    public static SoundModifierConfig SoundModifierConfig => UserConfig.SoundModifierConfig;

    public static UiConfig UiConfig => UserConfig.UiConfig;

    public static HotkeyConfig HotkeyConfig => UserConfig.HotkeyConfig;

    public static ServiceConfig ServiceConfig => _serviceConfig.Value;

    public static JsonServiceConfig JsonServiceConfig => ServiceConfig.JsonServiceConfig;

    public static LoggerServiceConfig LoggerServiceConfig => ServiceConfig.LoggerServiceConfig;

    public static void SaveConfig()
    {
        try
        {
            if (_config.IsValueCreated)
            {
                JsonConfigService.Save(
                    UserConfig,
                    nameof(UserConfig).ToLower(),
                    UserConfigJsonSerializerContext.Default
                );
            }

            // 保存ServiceConfig到ini
            if (!_serviceConfig.IsValueCreated)
                return;

            var ini = new IniConfigService();

            // JsonServiceConfig
            ini.Set(
                "EnableBackup",
                ServiceConfig.JsonServiceConfig.EnableBackup.ToString().ToLower(),
                "JsonService"
            );

            ini.Set(
                "EnablePerformanceLogging",
                ServiceConfig.JsonServiceConfig.EnablePerformanceLogging.ToString().ToLower(),
                "JsonService"
            );

            ini.Set("MaxBackupCount", ServiceConfig.JsonServiceConfig.MaxBackupCount.ToString(), "JsonService");

            // LoggerServiceConfig
            ini.Set(
                "IsKeepOpen",
                ServiceConfig.LoggerServiceConfig.IsKeepOpen.ToString().ToLower(),
                "LoggerService"
            );

            ini.Set("RetryCount", ServiceConfig.LoggerServiceConfig.RetryCount.ToString(), "LoggerService");
            ini.Set("Level", ServiceConfig.LoggerServiceConfig.Level.ToString(), "LoggerService");

            ini.Save(_serviceConfigIniPath);
        }
        catch (Exception e)
        {
            LoggerService.Error($"保存配置文件时发生错误 : {e.Message}");
        }
    }
}
