using System;
using System.Threading.Tasks;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;
using QwQ_Music.Services.ConfigIO;

namespace QwQ_Music.Models;

public static class ConfigInfoModel
{
    #region 配置加载

    // 使用Lazy<T>实现真正的懒加载
    private static readonly Lazy<SystemConfig> _systemConfig = new(() =>
    {
        var config =
            JsonConfigService.Load<SystemConfig>(
                nameof(SystemConfig).ToLower(),
                SystemConfigJsonSerializerContext.Default
            ) ?? new SystemConfig();

        LoggerService.SetConfig(config.LoggerServiceConfig);
        JsonConfigService.SetConfig(config.JsonServiceConfig);
        return config;
    });

    public static SystemConfig SystemConfig => _systemConfig.Value;

    // PlayerConfig 实现
    private static readonly Lazy<PlayerConfig> _playerConfig = new(
        () =>
            JsonConfigService.Load<PlayerConfig>(
                nameof(PlayerConfig).ToLower(),
                PlayerConfigJsonSerializerContext.Default
            ) ?? new PlayerConfig()
    );

    public static PlayerConfig PlayerConfig => _playerConfig.Value;

    // DesktopLyricConfig 实现
    private static readonly Lazy<LyricConfig> _lyricConfig = new(
        () =>
            JsonConfigService.Load<LyricConfig>(nameof(LyricConfig).ToLower(), LyricConfigJsonSerializerContext.Default)
            ?? new LyricConfig()
    );

    public static LyricConfig LyricConfig => _lyricConfig.Value;

    // SoundEffectConfig 实现
    private static readonly Lazy<AudioModifierConfig> _audioModifierConfig = new(
        () =>
            JsonConfigService.Load<AudioModifierConfig>(
                nameof(AudioModifierConfig).ToLower(),
                AudioModifierConfigJsonSerializerContext.Default
            ) ?? new AudioModifierConfig()
    );

    public static AudioModifierConfig AudioModifierConfig => _audioModifierConfig.Value;

    private static readonly Lazy<InterfaceConfig> _interfaceConfig = new(
        () =>
            JsonConfigService.Load<InterfaceConfig>(
                nameof(InterfaceConfig).ToLower(),
                InterfaceConfigJsonSerializerContext.Default
            ) ?? new InterfaceConfig()
    );

    public static InterfaceConfig InterfaceConfig => _interfaceConfig.Value;

    private static readonly Lazy<HotkeyConfig> _hotkeyConfig = new(() =>
    {
        var config =
            JsonConfigService.Load<HotkeyConfig>(
                nameof(HotkeyConfig).ToLower(),
                HotkeyConfigJsonSerializerContext.Default
            ) ?? new HotkeyConfig();

        if (config.FunctionToKeyMap.Count == 0)
        {
            config.FunctionToKeyMap = HotkeyConfig.CreateDefaultHotkeyConfig();
        }

        return config;
    });

    public static HotkeyConfig HotkeyConfig => _hotkeyConfig.Value;

    #endregion

    #region 配置保存

    public async static Task SaveSystemConfigAsync()
    {
        if (_systemConfig.IsValueCreated)
        {
            await JsonConfigService.SaveAsync(
                SystemConfig,
                nameof(SystemConfig).ToLower(),
                SystemConfigJsonSerializerContext.Default
            );
        }
    }

    public static async Task SaveAudioModifierConfigAsync()
    {
        // 只有当SoundEffectConfig已经被初始化时才保存
        if (_audioModifierConfig.IsValueCreated)
        {
            await JsonConfigService.SaveAsync(
                AudioModifierConfig,
                nameof(AudioModifierConfig).ToLower(),
                AudioModifierConfigJsonSerializerContext.Default
            );
        }
    }

    public static async Task SaveLyricConfigAsync()
    {
        if (_lyricConfig.IsValueCreated)
        {
            await JsonConfigService.SaveAsync(
                LyricConfig,
                nameof(LyricConfig).ToLower(),
                LyricConfigJsonSerializerContext.Default
            );
        }
    }

    public static async Task SavePlayerConfigAsync()
    {
        if (_playerConfig.IsValueCreated)
        {
            await JsonConfigService.SaveAsync(
                PlayerConfig,
                nameof(PlayerConfig).ToLower(),
                PlayerConfigJsonSerializerContext.Default
            );
        }
    }

    public static async Task SaveInterfaceConfigAsync()
    {
        if (_interfaceConfig.IsValueCreated)
        {
            await JsonConfigService.SaveAsync(
                InterfaceConfig,
                nameof(InterfaceConfig).ToLower(),
                InterfaceConfigJsonSerializerContext.Default
            );
        }
    }

    public static async Task SaveHotkeyConfigAsync()
    {
        if (_hotkeyConfig.IsValueCreated)
        {
            await JsonConfigService.SaveAsync(
                HotkeyConfig,
                nameof(HotkeyConfig).ToLower(),
                HotkeyConfigJsonSerializerContext.Default
            );
        }
    }

    // 添加保存所有已加载配置的方法
    public static async Task SaveAllAsync()
    {
        try
        {
            var saveSystemConfigTask = SaveSystemConfigAsync();
            var saveInterfaceConfigTask = SaveInterfaceConfigAsync();
            var savePlayerConfigTask = SavePlayerConfigAsync();
            var saveLyricConfigTask = SaveLyricConfigAsync();
            var saveAudioModifierConfigTask = SaveAudioModifierConfigAsync();
            var saveHotkeyConfigTask = SaveHotkeyConfigAsync();

            await Task.WhenAll(
                saveSystemConfigTask,
                saveInterfaceConfigTask,
                savePlayerConfigTask,
                saveLyricConfigTask,
                saveAudioModifierConfigTask,
                saveHotkeyConfigTask
            );
        }
        catch (Exception e)
        {
            await LoggerService.ErrorAsync($"保存配置文件时发生错误 : {e.Message}");
        }
    }

    #endregion
}
