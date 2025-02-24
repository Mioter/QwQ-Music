using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Color = Avalonia.Media.Color;
using Avalonia;
using Microsoft.Data.Sqlite;
using QwQ_Music.Utilities;

namespace QwQ_Music.Models;

public static class ConfigInfoModel {
    public const string Version = "0.1.0";
}

public abstract class PlayerConfig : IConfigBase {
    public static string FileName => nameof(PlayerConfig);
    public static bool IsInitialized { get; private set; }
    public static bool IsError { get; private set; }
    public static int Volume;
    public static bool IsMuted;
    public static string ConfigSavePath = EnsureExists.Path(Path.Combine(Directory.GetCurrentDirectory(), "config"));
    public static string CoverSavePath = EnsureExists.Path(Path.Combine(Directory.GetCurrentDirectory(), "cache", "cover"));
    public static string LyricsSavePath = EnsureExists.Path(Path.Combine(Directory.GetCurrentDirectory(), "cache", "lyrics"));
    public static ulong FadeInTime = 1000;
    public static ulong FadeOutTime = 1000;
    public static string LatestPlayListName = "";

    public static async Task<bool> LoadAsync() =>
        await ConfigIO.LoadFromJsonAsync<PlayerConfig>().ConfigureAwait(false);


    public static bool Parse(in JsonNode? config) {
        if (config is null) {
            IsInitialized = true;
            IsError = true;
            return false;
        }

        IsError = ConfigIO.TryParse(config, nameof(Volume), ref Volume) |
                  ConfigIO.TryParse(config, nameof(IsMuted), ref IsMuted) |
                  ConfigIO.TryParse(config, nameof(ConfigSavePath), ref ConfigSavePath) |
                  ConfigIO.TryParse(config, nameof(CoverSavePath), ref CoverSavePath) |
                  ConfigIO.TryParse(config, nameof(LyricsSavePath), ref LyricsSavePath) |
                  ConfigIO.TryParse(config, nameof(FadeInTime), ref FadeInTime) |
                  ConfigIO.TryParse(config, nameof(FadeOutTime), ref FadeOutTime) |
                  ConfigIO.TryParse(config, nameof(LatestPlayListName), ref LatestPlayListName);

        IsInitialized = true;
        return !IsError;
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
            [nameof(LatestPlayListName)] = LatestPlayListName,
        };
}

public abstract class MainConfig : IConfigBase {
    public static string FileName => nameof(MainConfig);
    public static bool IsInitialized { get; private set; }
    public static bool IsError { get; private set; }

    public static string? Skin;
    public static bool FollowSystemTheme;

    public static string DatabaseSavePath = Path.Combine(Directory.GetCurrentDirectory(), "config", "data.db");

    public static async Task<bool> Load() => await ConfigIO.LoadFromJsonAsync<MainConfig>().ConfigureAwait(false);

    public static async Task<bool> LoadAsync() {
        return await ConfigIO.LoadFromJsonAsync<MainConfig>().ConfigureAwait(false);
    }

    public static bool Parse(in JsonNode? config) {
        if (config is null) {
            IsInitialized = true;
            IsError = true;
            return false;
        }

        IsError = ConfigIO.TryParse(config, nameof(Skin), ref Skin) |
                  ConfigIO.TryParse(config, nameof(FollowSystemTheme), ref FollowSystemTheme) |
                  ConfigIO.TryParse(config, nameof(DatabaseSavePath), ref DatabaseSavePath);
        IsInitialized = true;
        return !IsError;
    }

    public static JsonObject Dump() =>
        new() {
            [nameof(ConfigInfoModel.Version)] = ConfigInfoModel.Version,
            [nameof(Skin)] = Skin,
            [nameof(FollowSystemTheme)] = FollowSystemTheme,
            [nameof(DatabaseSavePath)] = DatabaseSavePath
        };
}

public abstract class DesktopLyricConfig : IConfigBase {
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

    public static async Task<bool> LoadAsync() =>
        await ConfigIO.LoadFromJsonAsync<DesktopLyricConfig>().ConfigureAwait(false);

    public static bool Parse(in JsonNode? config) {
        if (config is null) {
            IsError = true;
            IsInitialized = true;
            return false;
        }

        IsError = ConfigIO.TryParse(config, nameof(IsEnabled), ref IsEnabled) |
                  ConfigIO.TryParse(config, nameof(IsDoubleLine), ref IsDoubleLine) |
                  ConfigIO.TryParse(config, nameof(IsDualLang), ref IsDualLang) |
                  ConfigIO.TryParse(config, nameof(IsVertical), ref IsVertical) |
                  ConfigIO.TryParse(config, nameof(Offset), ref Offset) |
                  ConfigIO.TryParse(config, nameof(MainTopColor), ref MainTopColor) |
                  ConfigIO.TryParse(config, nameof(MainBottomColor), ref MainBottomColor) |
                  ConfigIO.TryParse(config, nameof(MainBorderColor), ref MainBorderColor) |
                  ConfigIO.TryParse(config, nameof(AltTopColor), ref AltTopColor) |
                  ConfigIO.TryParse(config, nameof(AltBottomColor), ref AltBottomColor) |
                  ConfigIO.TryParse(config, nameof(AltBorderColor), ref AltBorderColor) |
                  ConfigIO.TryParse(config, nameof(BackgroundColor), ref BackgroundColor) |
                  ConfigIO.TryParse(config, nameof(Position), ref Position) |
                  ConfigIO.TryParse(config, nameof(Size), ref Size) |
                  ConfigIO.TryParse(config, nameof(MainFontSize), ref MainFontSize) |
                  ConfigIO.TryParse(config, nameof(AltFontSize), ref AltFontSize);
        IsInitialized = true;
        return !IsError;
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

public interface IConfigBase {
    static abstract string FileName { get; }
    static abstract bool IsInitialized { get; }
    static abstract bool IsError { get; }
    static abstract Task<bool> LoadAsync();
    static abstract bool Parse(in JsonNode? config);
    static abstract JsonObject Dump();
}

public interface IModelBase<out TConfig> where TConfig : IModelBase<TConfig> {
    bool IsInitialized { get; }
    bool IsError { get; }
    static abstract TConfig Parse(in SqliteDataReader config);
    Dictionary<string, string> Dump();
}