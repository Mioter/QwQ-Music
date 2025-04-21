using System;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services.ConfigIO;

namespace QwQ_Music.Models;

public static class ConfigInfoModel
{
    public const string Version = "0.1.0";

    #region 配置加载

    // 使用Lazy<T>实现真正的懒加载
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<MainConfig> _mainConfig = new(
        () =>
            JsonConfigService.Load<MainConfig>(nameof(MainConfig).ToLower(), MainConfigJsonSerializerContext.Default)
            ?? new MainConfig()
    );

    public static MainConfig MainConfig => _mainConfig.Value;

    // PlayerConfig 实现
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<PlayerConfig> _playerConfig = new(
        () =>
            JsonConfigService.Load<PlayerConfig>(
                nameof(PlayerConfig).ToLower(),
                PlayerConfigJsonSerializerContext.Default
            ) ?? new PlayerConfig()
    );

    public static PlayerConfig PlayerConfig => _playerConfig.Value;

    // DesktopLyricConfig 实现
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<LyricConfig> _lyricConfig = new(
        () =>
            JsonConfigService.Load<LyricConfig>(nameof(LyricConfig).ToLower(), LyricConfigJsonSerializerContext.Default)
            ?? new LyricConfig()
    );

    public static LyricConfig LyricConfig => _lyricConfig.Value;

    // SoundEffectConfig 实现
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<AudioModifierConfig> _audioModifierConfig = new(
        () =>
            JsonConfigService.Load<AudioModifierConfig>(
                nameof(AudioModifierConfig).ToLower(),
                AudioModifierConfigJsonSerializerContext.Default
            ) ?? new AudioModifierConfig()
    );

    public static AudioModifierConfig AudioModifierConfig => _audioModifierConfig.Value;

    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<InterfaceConfig> _interfaceConfig = new(
        () =>
            JsonConfigService.Load<InterfaceConfig>(
                nameof(InterfaceConfig).ToLower(),
                InterfaceConfigJsonSerializerContext.Default
            ) ?? new InterfaceConfig()
    );

    public static InterfaceConfig InterfaceConfig => _interfaceConfig.Value;

    #endregion

    #region 配置保存

    public static void SaveMainConfig()
    {
        if (_mainConfig.IsValueCreated)
        {
            JsonConfigService
                .SaveAsync(MainConfig, nameof(MainConfig).ToLower(), InterfaceConfigJsonSerializerContext.Default)
                .Wait();
        }
    }

    public static void SaveSoundEffectConfig()
    {
        // 只有当SoundEffectConfig已经被初始化时才保存
        if (_audioModifierConfig.IsValueCreated)
        {
            JsonConfigService
                .SaveAsync(
                    AudioModifierConfig,
                    nameof(AudioModifierConfig).ToLower(),
                    AudioModifierConfigJsonSerializerContext.Default
                )
                .ConfigureAwait(false);
        }
    }

    public static void SaveDesktopLyricConfig()
    {
        if (_lyricConfig.IsValueCreated)
        {
            JsonConfigService
                .SaveAsync(LyricConfig, nameof(LyricConfig).ToLower(), LyricConfigJsonSerializerContext.Default)
                .Wait();
        }
    }

    public static void SavePlayerConfig()
    {
        if (_playerConfig.IsValueCreated)
        {
            JsonConfigService
                .SaveAsync(PlayerConfig, nameof(PlayerConfig).ToLower(), PlayerConfigJsonSerializerContext.Default)
                .Wait();
        }
    }

    public static void SaveInterfaceConfig()
    {
        if (_interfaceConfig.IsValueCreated)
        {
            JsonConfigService
                .SaveAsync(
                    InterfaceConfig,
                    nameof(InterfaceConfig).ToLower(),
                    InterfaceConfigJsonSerializerContext.Default
                )
                .Wait();
        }
    }

    // 添加保存所有已加载配置的方法
    public static void SaveAll()
    {
        SaveMainConfig();
        SaveInterfaceConfig();
        SavePlayerConfig();
        SaveDesktopLyricConfig();
        SaveSoundEffectConfig();
    }

    #endregion
}
