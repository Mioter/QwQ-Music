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
    private static readonly Lazy<MainConfig> _mainConfig = new(
        () =>
            JsonConfigService.Load<MainConfig>(nameof(MainConfig).ToLower(), MainConfigJsonSerializerContext.Default)
            ?? new MainConfig()
    );

    public static MainConfig MainConfig => _mainConfig.Value;

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

    #endregion

    #region 配置保存

    public async static Task SaveMainConfig()
    {
        if (_mainConfig.IsValueCreated)
        {
            await JsonConfigService.SaveAsync(
                MainConfig,
                nameof(MainConfig).ToLower(),
                InterfaceConfigJsonSerializerContext.Default
            );
        }
    }

    public static async Task SaveSoundEffectConfig()
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

    public static async Task SaveLyricConfig()
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

    public static async Task SavePlayerConfig()
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

    public static async Task SaveInterfaceConfig()
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

    // 添加保存所有已加载配置的方法
    public static async Task SaveAllAsync()
    {
        try
        {
            await SaveMainConfig();
            await SaveInterfaceConfig();
            await SavePlayerConfig();
            await SaveLyricConfig();
            await SaveSoundEffectConfig();
        }
        catch (Exception e)
        {
            await LoggerService.ErrorAsync($"保存配置文件时发生错误 : {e.Message}");
        }
    }

    #endregion
}
