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
                        nameof(ConfigModel.MainConfig),
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
                        nameof(ConfigModel.PlayerConfig),
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
                        nameof(ConfigModel.DesktopLyricConfig),
                        DesktopLyricConfigJsonSerializerContext.Default
                    ) ?? new DesktopLyricConfig();
            }
            return _desktopLyricConfig;
        }
    }
}
