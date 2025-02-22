using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Color = Avalonia.Media.Color;
using Avalonia;
using Microsoft.Data.Sqlite;
using Log = QwQ_Music.Services.LoggerService;
using static QwQ_Music.Utilities.ConfigIO;

namespace QwQ_Music.Models;

public static class ConfigInfoModel {
    public const string Version = "0.1.0";
}

public abstract class PlayerConfig : IStaticConfigBase {
    public static string FileName => nameof(PlayerConfig);
    public static bool IsInitialized { get; private set; }
    public static bool IsError { get; private set; }
    public static int Volume;
    public static bool IsMuted;
    public static string ConfigSavePath = Path.Combine(Directory.GetCurrentDirectory(), "config");
    public static string CoverSavePath = Path.Combine(Directory.GetCurrentDirectory(), "cache", "cover");
    public static string LyricsSavePath = Path.Combine(Directory.GetCurrentDirectory(), "cache", "lyrics");
    public static ulong FadeInTime = 1000;
    public static ulong FadeOutTime = 1000;
    public static async Task<bool> LoadAsync() => await LoadFromJsonAsync<PlayerConfig>().ConfigureAwait(false);


    public static bool Parse(JsonNode? config) {
        if (config is null) {
            IsInitialized = true;
            IsError = true;
            return false;
        }

        try {
            Volume = config[nameof(Volume)]!.GetValue<int>();
            IsMuted = config[nameof(IsMuted)]!.GetValue<bool>();
            IsError = config[nameof(IsError)]!.GetValue<bool>();
            ConfigSavePath = config[nameof(ConfigSavePath)]!.GetValue<string>();
            CoverSavePath = config[nameof(CoverSavePath)]!.GetValue<string>();
            LyricsSavePath = config[nameof(LyricsSavePath)]!.GetValue<string>();
            FadeInTime = config[nameof(FadeInTime)]!.GetValue<ulong>();
            FadeOutTime = config[nameof(FadeOutTime)]!.GetValue<ulong>();
            IsError = false;
            IsInitialized = true;
            return true;
        } catch (NullReferenceException) {
            Log.Error(
                $"Cannot Load {nameof(PlayerConfig)}. Config file broken or version inconsistent? (file version {
                    config[nameof(ConfigInfoModel.Version)]?.GetValue<string>()}, app version {ConfigInfoModel.Version
                    })");
            IsInitialized = true;
            IsError = true;
            return false;
        }
    }

    public static JsonObject Dump() =>
        new() {
            [nameof(ConfigInfoModel.Version)] = ConfigInfoModel.Version,
            [nameof(Volume)] = Volume,
            [nameof(IsMuted)] = IsMuted,
            [nameof(ConfigSavePath)] = ConfigSavePath,
            [nameof(CoverSavePath)] = CoverSavePath,
            [nameof(LyricsSavePath)] = LyricsSavePath,
            [nameof(FadeInTime)] = FadeInTime,
            [nameof(FadeOutTime)] = FadeOutTime,
        };
}

public abstract class MainConfig : IStaticConfigBase {
    public static string FileName => nameof(MainConfig);
    public static bool IsInitialized { get; private set; }
    public static bool IsError { get; private set; }

    public static string? Skin;
    public static bool FollowSystemTheme;
    public static string DatabaseSavePath = Path.Combine(Directory.GetCurrentDirectory(), "data.db");

    public static async Task<bool> Load() => await LoadFromJsonAsync<MainConfig>().ConfigureAwait(false);

    public static async Task<bool> LoadAsync() { return await LoadFromJsonAsync<MainConfig>().ConfigureAwait(false); }

    public static bool Parse(JsonNode? config) {
        if (config is null) {
            IsInitialized = true;
            IsError = true;
            return false;
        }

        try {
            Skin = config[nameof(Skin)]!.GetValue<string>();
            FollowSystemTheme = config[nameof(FollowSystemTheme)]!.GetValue<bool>();
            DatabaseSavePath = config[nameof(DatabaseSavePath)]!.GetValue<string>();
            IsInitialized = true;
            IsError = false;
            return true;
        } catch (NullReferenceException) {
            Log.Error(
                $"Cannot Load {nameof(MainConfig)}. Config file broken or version inconsistent? (file version {
                    config[nameof(ConfigInfoModel.Version)]?.GetValue<string>()}, app version {ConfigInfoModel.Version
                    })");
            IsInitialized = true;
            IsError = true;
            return false;
        }
    }

    public static JsonObject Dump() =>
        new() {
            [nameof(ConfigInfoModel.Version)] = ConfigInfoModel.Version,
            [nameof(Skin)] = Skin,
            [nameof(FollowSystemTheme)] = FollowSystemTheme,
            [nameof(DatabaseSavePath)] = DatabaseSavePath
        };
}

public abstract class DesktopLyricConfig : IStaticConfigBase {
    public static string FileName => nameof(DesktopLyricConfig);
    public static bool IsInitialized { get; private set; }
    public static bool IsError { get; private set; }

    public static bool IsEnabled;
    public static bool IsDoubleLine;
    public static bool IsDualLang;
    public static bool IsVertical;
    public static int Offset;
    public static int MainFontSize;
    public static int AltFontSize;
    public static Color MainTopColor;
    public static Color MainBottomColor;
    public static Color MainBorderColor;
    public static Color AltTopColor;
    public static Color AltBottomColor;
    public static Color AltBorderColor;
    public static Color BackgroundColor;
    public static PixelPoint Position;
    public static Size Size;

    public static async Task<bool> LoadAsync() => await LoadFromJsonAsync<DesktopLyricConfig>().ConfigureAwait(false);

    public static bool Parse(JsonNode? config) {
        if (config is null) {
            IsError = true;
            IsInitialized = true;
            return false;
        }

        try {
            IsEnabled = config[nameof(IsEnabled)]!.GetValue<bool>();
            IsDoubleLine = config[nameof(IsDoubleLine)]!.GetValue<bool>();
            IsDualLang = config[nameof(IsDualLang)]!.GetValue<bool>();
            IsVertical = config[nameof(IsVertical)]!.GetValue<bool>();
            Offset = config[nameof(Offset)]!.GetValue<int>();
            MainTopColor = config[nameof(MainTopColor)]!.GetValue<Color>();
            MainBottomColor = config[nameof(MainBottomColor)]!.GetValue<Color>();
            MainBorderColor = config[nameof(MainBorderColor)]!.GetValue<Color>();
            AltTopColor = config[nameof(AltTopColor)]!.GetValue<Color>();
            AltBottomColor = config[nameof(AltBottomColor)]!.GetValue<Color>();
            AltBorderColor = config[nameof(AltBorderColor)]!.GetValue<Color>();
            BackgroundColor = config[nameof(BackgroundColor)]!.GetValue<Color>();
            Position = config[nameof(Position)]!.GetValue<PixelPoint>();
            Size = config[nameof(Size)]!.GetValue<Size>();
            MainFontSize = config[nameof(MainFontSize)]!.GetValue<int>();
            AltFontSize = config[nameof(AltFontSize)]!.GetValue<int>();
            IsError = false;
            return true;
        } catch (NullReferenceException) {
            Log.Error(
                $"Cannot Load {nameof(DesktopLyricConfig)}. Config file broken or version inconsistent? (file version {
                    config[nameof(ConfigInfoModel.Version)]?.GetValue<string>()}, app version {ConfigInfoModel.Version
                    })");
            IsError = true;
            return false;
        } finally { IsInitialized = true; }
    }

    public static JsonObject Dump() =>
        new() {
            [nameof(ConfigInfoModel.Version)] = ConfigInfoModel.Version,
            [nameof(IsEnabled)] = IsEnabled,
            [nameof(IsDoubleLine)] = IsDoubleLine,
            [nameof(IsDualLang)] = IsDualLang,
            [nameof(IsVertical)] = IsVertical,
            [nameof(Offset)] = Offset,
            [nameof(MainTopColor)] = MainTopColor.ToString(),
            [nameof(MainBottomColor)] = MainBottomColor.ToString(),
            [nameof(MainBorderColor)] = MainBorderColor.ToString(),
            [nameof(AltTopColor)] = AltTopColor.ToString(),
            [nameof(AltBottomColor)] = AltBottomColor.ToString(),
            [nameof(AltBorderColor)] = AltBorderColor.ToString(),
            [nameof(BackgroundColor)] = BackgroundColor.ToString(),
            [nameof(Position)] = Position.ToString(),
            [nameof(Size)] = Size.ToString(),
        };
}

public interface IStaticConfigBase {
    static abstract string FileName { get; }
    static abstract bool IsInitialized { get; }
    static abstract bool IsError { get; }
    static abstract Task<bool> LoadAsync();
    static abstract bool Parse(JsonNode? config);
    static abstract JsonObject Dump();
}

public interface IConfigBase<out TConfig> where TConfig : IConfigBase<TConfig> {
    string FileName { get; }
    bool IsInitialized { get; }
    bool IsError { get; }
    static abstract TConfig Parse(SqliteDataReader config);
    string Dump();
}