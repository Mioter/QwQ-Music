using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services.ConfigIO;
using System;

namespace QwQ_Music.Models;

public static class ConfigInfoModel
{
    public const string Version = "0.1.0";

    // 使用Lazy<T>实现真正的懒加载
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<MainConfig> _mainConfig = new(() =>
        JsonConfigService.Load<MainConfig>(
            nameof(MainConfig).ToLower(),
            MainConfigJsonSerializerContext.Default
        ) ?? new MainConfig()
    );

    public static MainConfig MainConfig => _mainConfig.Value;

    // PlayerConfig 实现
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<PlayerConfig> _playerConfig = new(() =>
        JsonConfigService.Load<PlayerConfig>(
            nameof(PlayerConfig).ToLower(),
            PlayerConfigJsonSerializerContext.Default
        ) ?? new PlayerConfig()
    );

    public static PlayerConfig PlayerConfig => _playerConfig.Value;

    // DesktopLyricConfig 实现
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<DesktopLyricConfig> _desktopLyricConfig = new(() =>
        JsonConfigService.Load<DesktopLyricConfig>(
            nameof(DesktopLyricConfig).ToLower(),
            DesktopLyricConfigJsonSerializerContext.Default
        ) ?? new DesktopLyricConfig()
    );

    public static DesktopLyricConfig DesktopLyricConfig => _desktopLyricConfig.Value;

    // SoundEffectConfig 实现
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<SoundEffectConfig> _soundEffectConfig = new(() =>
        JsonConfigService.Load<SoundEffectConfig>(
            nameof(SoundEffectConfig).ToLower(),
            SoundEffectConfigModelJsonSerializerContext.Default
        ) ?? new SoundEffectConfig()
    );

    public static SoundEffectConfig SoundEffectConfig => _soundEffectConfig.Value;

    public static void SaveSoundEffectConfig()
    {
        // 只有当SoundEffectConfig已经被初始化时才保存
        if (_soundEffectConfig.IsValueCreated)
        {
            JsonConfigService
                .SaveAsync(
                    SoundEffectConfig,
                    nameof(SoundEffectConfig).ToLower(),
                    SoundEffectConfigModelJsonSerializerContext.Default
                )
                .ConfigureAwait(false);
        }
    }
    
    // 添加保存所有已加载配置的方法
    public static void SaveAll()
    {
        if (_mainConfig.IsValueCreated)
        {
            JsonConfigService
                .SaveAsync(
                    MainConfig,
                    nameof(MainConfig).ToLower(),
                    MainConfigJsonSerializerContext.Default
                )
                .ConfigureAwait(false);
        }
        
        if (_playerConfig.IsValueCreated)
        {
            JsonConfigService
                .SaveAsync(
                    PlayerConfig,
                    nameof(PlayerConfig).ToLower(),
                    PlayerConfigJsonSerializerContext.Default
                )
                .ConfigureAwait(false);
        }
        
        if (_desktopLyricConfig.IsValueCreated)
        {
            JsonConfigService
                .SaveAsync(
                    DesktopLyricConfig,
                    nameof(DesktopLyricConfig).ToLower(),
                    DesktopLyricConfigJsonSerializerContext.Default
                )
                .ConfigureAwait(false);
        }
        
        if (_soundEffectConfig.IsValueCreated)
        {
            JsonConfigService
                .SaveAsync(
                    SoundEffectConfig,
                    nameof(SoundEffectConfig).ToLower(),
                    SoundEffectConfigModelJsonSerializerContext.Default
                )
                .ConfigureAwait(false);
        }
    }
}
