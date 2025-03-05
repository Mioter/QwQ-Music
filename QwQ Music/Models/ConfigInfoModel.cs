using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Data.Sqlite;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.Utilities;
using Color = Avalonia.Media.Color;

namespace QwQ_Music.Models;

public static class ConfigInfoModel
{
    public const string Version = "0.1.0";
}

public abstract class PlayerConfig : IConfigBase
{
    public static string FileName => nameof(PlayerConfig);
    public static bool IsInitialized { get; private set; }
    public static bool IsError { get; private set; }
    public static int Volume;
    public static bool IsMuted;
    public static string ConfigSavePath = EnsureExists.Path(Path.Combine(Directory.GetCurrentDirectory(), "config"));
    public static string CoverSavePath = EnsureExists.Path(
        Path.Combine(Directory.GetCurrentDirectory(), "cache", "cover")
    );
    public static string LyricsSavePath = EnsureExists.Path(
        Path.Combine(Directory.GetCurrentDirectory(), "cache", "lyrics")
    );
    public static string LatestPlayListName = "";

    public async static Task<bool> LoadAsync() =>
        await JsonService.LoadFromJsonAsync<PlayerConfig>().ConfigureAwait(false);

    public static bool Parse(in JsonNode? config)
    {
        if (config is null)
        {
            IsInitialized = true;
            IsError = true;
            return false;
        }

        IsError =
            JsonService.TryParse(config, nameof(Volume), ref Volume)
            | JsonService.TryParse(config, nameof(IsMuted), ref IsMuted)
            | JsonService.TryParse(config, nameof(ConfigSavePath), ref ConfigSavePath)
            | JsonService.TryParse(config, nameof(CoverSavePath), ref CoverSavePath)
            | JsonService.TryParse(config, nameof(LyricsSavePath), ref LyricsSavePath)
            | JsonService.TryParse(config, nameof(LatestPlayListName), ref LatestPlayListName);

        IsInitialized = true;
        return !IsError;
    }

    public static JsonObject Dump() =>
        new()
        {
            [nameof(ConfigInfoModel.Version)] = ConfigInfoModel.Version,
            [nameof(Volume)] = Volume,
            [nameof(IsMuted)] = IsMuted,
            [nameof(ConfigSavePath)] = ConfigSavePath,
            [nameof(CoverSavePath)] = CoverSavePath,
            [nameof(LyricsSavePath)] = LyricsSavePath,
            [nameof(LatestPlayListName)] = LatestPlayListName,
        };
}

public abstract class MainConfig : IConfigBase
{
    public static string FileName => nameof(MainConfig);
    public static bool IsInitialized { get; private set; }
    public static bool IsError { get; private set; }

    public static string? Skin;
    public static bool FollowSystemTheme;

    public static string DatabaseSavePath = Path.Combine(Directory.GetCurrentDirectory(), "config", "data.db");

    public async static Task<bool> LoadAsync()
    {
        return await JsonService.LoadFromJsonAsync<MainConfig>().ConfigureAwait(false);
    }

    public static bool Parse(in JsonNode? config)
    {
        if (config is null)
        {
            IsInitialized = true;
            IsError = true;
            return false;
        }

        IsError =
            JsonService.TryParse(config, nameof(Skin), ref Skin)
            | JsonService.TryParse(config, nameof(FollowSystemTheme), ref FollowSystemTheme)
            | JsonService.TryParse(config, nameof(DatabaseSavePath), ref DatabaseSavePath);
        IsInitialized = true;
        return !IsError;
    }

    public static JsonObject Dump() =>
        new()
        {
            [nameof(ConfigInfoModel.Version)] = ConfigInfoModel.Version,
            [nameof(Skin)] = Skin,
            [nameof(FollowSystemTheme)] = FollowSystemTheme,
            [nameof(DatabaseSavePath)] = DatabaseSavePath,
        };
}

public abstract class DesktopLyricConfig : IConfigBase
{
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

    public async static Task<bool> LoadAsync() =>
        await JsonService.LoadFromJsonAsync<DesktopLyricConfig>().ConfigureAwait(false);

    public static bool Parse(in JsonNode? config)
    {
        if (config is null)
        {
            IsError = true;
            IsInitialized = true;
            return false;
        }

        IsError =
            JsonService.TryParse(config, nameof(IsEnabled), ref IsEnabled)
            | JsonService.TryParse(config, nameof(IsDoubleLine), ref IsDoubleLine)
            | JsonService.TryParse(config, nameof(IsDualLang), ref IsDualLang)
            | JsonService.TryParse(config, nameof(IsVertical), ref IsVertical)
            | JsonService.TryParse(config, nameof(Offset), ref Offset)
            | JsonService.TryParse(config, nameof(MainTopColor), ref MainTopColor)
            | JsonService.TryParse(config, nameof(MainBottomColor), ref MainBottomColor)
            | JsonService.TryParse(config, nameof(MainBorderColor), ref MainBorderColor)
            | JsonService.TryParse(config, nameof(AltTopColor), ref AltTopColor)
            | JsonService.TryParse(config, nameof(AltBottomColor), ref AltBottomColor)
            | JsonService.TryParse(config, nameof(AltBorderColor), ref AltBorderColor)
            | JsonService.TryParse(config, nameof(BackgroundColor), ref BackgroundColor)
            | JsonService.TryParse(config, nameof(Position), ref Position)
            | JsonService.TryParse(config, nameof(Size), ref Size)
            | JsonService.TryParse(config, nameof(MainFontSize), ref MainFontSize)
            | JsonService.TryParse(config, nameof(AltFontSize), ref AltFontSize);
        IsInitialized = true;
        return !IsError;
    }

    public static JsonObject Dump() =>
        new()
        {
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

public interface IConfigBase
{
    abstract static string FileName { get; }
    abstract static bool IsInitialized { get; }
    abstract static bool IsError { get; }
    abstract static Task<bool> LoadAsync();
    abstract static bool Parse(in JsonNode? config);
    abstract static JsonObject Dump();
}

public interface IModelBase<out TConfig>
    where TConfig : IModelBase<TConfig>
{
    bool IsInitialized { get; }
    bool IsError { get; }
    abstract static TConfig Parse(in SqliteDataReader config);
    Dictionary<string, string> Dump();
}
