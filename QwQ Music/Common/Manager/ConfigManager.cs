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

        // DataBaseConfig
        config.DataBaseConfig.EnableVerboseLogging = ini.Get("EnableVerboseLogging", "DataBase")?.ToLower() != "false";

        config.DataBaseConfig.EnablePerformanceMonitoring =
            ini.Get("EnablePerformanceMonitoring", "DataBase")?.ToLower() != "false";

        config.DataBaseConfig.SlowQueryThreshold = int.TryParse(ini.Get("SlowQueryThreshold", "DataBase"), out int sqt)
            ? sqt
            : 500;

        config.DataBaseConfig.CommandTimeout = int.TryParse(ini.Get("CommandTimeout", "DataBase"), out int cto)
            ? cto
            : 30;

        config.DataBaseConfig.MaxRetryCount = int.TryParse(ini.Get("MaxRetryCount", "DataBase"), out int mrc) ? mrc : 3;
        config.DataBaseConfig.RetryDelay = int.TryParse(ini.Get("RetryDelay", "DataBase"), out int rtd) ? rtd : 100;

        config.DataBaseConfig.MaxConnectionPoolSize = int.TryParse(
            ini.Get("MaxConnectionPoolSize", "DataBase"),
            out int mcps
        )
            ? mcps
            : 10;

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

    public static AudioModifierConfig AudioModifierConfig => UserConfig.AudioModifierConfig;

    public static UiConfig UiConfig => UserConfig.UiConfig;

    public static HotkeyConfig HotkeyConfig => UserConfig.HotkeyConfig;

    public static ServiceConfig ServiceConfig => _serviceConfig.Value;

    public static JsonServiceConfig JsonServiceConfig => ServiceConfig.JsonServiceConfig;

    public static LoggerServiceConfig LoggerServiceConfig => ServiceConfig.LoggerServiceConfig;

    public static DataBaseConfig DataBaseConfig => ServiceConfig.DataBaseConfig;

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

            // DataBaseConfig
            ini.Set(
                "EnableVerboseLogging",
                ServiceConfig.DataBaseConfig.EnableVerboseLogging.ToString().ToLower(),
                "DataBase"
            );

            ini.Set(
                "EnablePerformanceMonitoring",
                ServiceConfig.DataBaseConfig.EnablePerformanceMonitoring.ToString().ToLower(),
                "DataBase"
            );

            ini.Set("SlowQueryThreshold", ServiceConfig.DataBaseConfig.SlowQueryThreshold.ToString(), "DataBase");
            ini.Set("CommandTimeout", ServiceConfig.DataBaseConfig.CommandTimeout.ToString(), "DataBase");
            ini.Set("MaxRetryCount", ServiceConfig.DataBaseConfig.MaxRetryCount.ToString(), "DataBase");
            ini.Set("RetryDelay", ServiceConfig.DataBaseConfig.RetryDelay.ToString(), "DataBase");

            ini.Set(
                "MaxConnectionPoolSize",
                ServiceConfig.DataBaseConfig.MaxConnectionPoolSize.ToString(),
                "DataBase"
            );

            ini.Save(_serviceConfigIniPath);
        }
        catch (Exception e)
        {
            LoggerService.Error($"保存配置文件时发生错误 : {e.Message}");
        }
    }
}
