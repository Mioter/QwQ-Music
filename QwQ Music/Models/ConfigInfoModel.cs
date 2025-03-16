using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services.ConfigIO;

namespace QwQ_Music.Models;

public static class ConfigInfoModel
{
    public const string Version = "0.1.0";

    // MainConfig 实现
    private static MainConfig? _mainConfig;
    public static MainConfig MainConfig
    {
        get
        {
            if (_mainConfig != null)
                return _mainConfig;

            lock (typeof(ConfigInfoModel))
            {
                _mainConfig ??=
                    JsonConfigService.Load<MainConfig>(
                        nameof(MainConfig).ToLower(),
                        MainConfigJsonSerializerContext.Default
                    ) ?? new MainConfig();
            }
            return _mainConfig;
        }
    }

    // PlayerConfig 实现
    private static PlayerConfig? _playerConfig;
    public static PlayerConfig PlayerConfig
    {
        get
        {
            if (_playerConfig != null)
                return _playerConfig;

            lock (typeof(ConfigInfoModel))
            {
                _playerConfig ??=
                    JsonConfigService.Load<PlayerConfig>(
                        nameof(PlayerConfig).ToLower(),
                        PlayerConfigJsonSerializerContext.Default
                    ) ?? new PlayerConfig();
            }
            return _playerConfig;
        }
    }

    // DesktopLyricConfig 实现
    private static DesktopLyricConfig? _desktopLyricConfig;
    public static DesktopLyricConfig DesktopLyricConfig
    {
        get
        {
            if (_desktopLyricConfig != null)
                return _desktopLyricConfig;

            lock (typeof(ConfigInfoModel))
            {
                _desktopLyricConfig ??=
                    JsonConfigService.Load<DesktopLyricConfig>(
                        nameof(DesktopLyricConfig).ToLower(),
                        DesktopLyricConfigJsonSerializerContext.Default
                    ) ?? new DesktopLyricConfig();
            }
            return _desktopLyricConfig;
        }
    }

    private static SoundEffectConfig? _soundEffectConfig;
    public static SoundEffectConfig SoundEffectConfig
    {
        get
        {
            if (_soundEffectConfig != null)
                return _soundEffectConfig;

            lock (typeof(ConfigInfoModel))
            {
                _soundEffectConfig ??=
                    JsonConfigService.Load<SoundEffectConfig>(
                        nameof(SoundEffectConfig).ToLower(),
                        SoundEffectConfigModelJsonSerializerContext.Default
                    ) ?? new SoundEffectConfig();
            }
            return _soundEffectConfig;
        }
    }

    public static void Save()
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
