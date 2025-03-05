using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using QwQ_Music.Models;
using QwQ_Music.Utilities;

namespace QwQ_Music.Services.ConfigIO;

public static class JsonService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

    private static readonly string ConfigDefaultPath = EnsureExists.Path(
        Path.Combine(Directory.GetCurrentDirectory(), "config")
    );

    private const string FileExtension = ".QwQ.json";

    /// <summary>
    /// 异步从 JSON 文件加载数据。
    /// </summary>
    public async static Task<bool> LoadFromJsonAsync<TConfig>(string? configPath = null)
        where TConfig : IConfigBase
    {
        if (!File.Exists(configPath ?? ConfigDefaultPath))
        {
            LoggerService.Error(
                $"Config file {Path.Combine(configPath ?? ConfigDefaultPath, TConfig.FileName + FileExtension)
                } not found"
            );

            EnsureExists.File(Path.Combine(configPath ?? ConfigDefaultPath, TConfig.FileName + FileExtension));
            TConfig.Parse(null);
            return false;
        }

        try
        {
            await using var fileStream = new FileStream(
                Path.Combine(configPath ?? ConfigDefaultPath, TConfig.FileName + FileExtension),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );
            return TConfig.Parse(await JsonNode.ParseAsync(fileStream).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            LoggerService.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 同步从 JSON 文件加载数据。
    /// </summary>
    public static bool LoadFromJson<TConfig>(string? configPath = null)
        where TConfig : IConfigBase
    {
        if (!File.Exists(Path.Combine(configPath ?? ConfigDefaultPath, TConfig.FileName + FileExtension)))
        {
            LoggerService.Error(
                $"Config file {Path.Combine(configPath ?? ConfigDefaultPath, TConfig.FileName + FileExtension)
                } not found"
            );
            EnsureExists.File(Path.Combine(configPath ?? ConfigDefaultPath, TConfig.FileName + FileExtension));
            TConfig.Parse(null);
            return false;
        }

        try
        {
            using var fileStream = new FileStream(
                Path.Combine(configPath ?? ConfigDefaultPath, TConfig.FileName + FileExtension),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );
            return TConfig.Parse(JsonNode.Parse(fileStream));
        }
        catch (Exception ex)
        {
            LoggerService.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 异步保存数据到 JSON 文件。
    /// </summary>
    public async static Task<bool> SaveToJsonAsync<TConfig>(string? configPath = null)
        where TConfig : IConfigBase
    {
        try
        {
            await new StreamWriter(
                new FileStream(
                    Path.Combine(configPath ?? ConfigDefaultPath, TConfig.FileName + FileExtension),
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read
                )
            )
                .WriteAsync(TConfig.Dump().ToJsonString(JsonSerializerOptions))
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 同步保存数据到 JSON 文件。
    /// </summary>
    public static bool SaveToJson<TConfig>(string? configPath = null)
        where TConfig : IConfigBase
    {
        try
        {
            new StreamWriter(
                new FileStream(
                    Path.Combine(configPath ?? ConfigDefaultPath, TConfig.FileName + FileExtension),
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read
                )
            ).Write(TConfig.Dump().ToJsonString(JsonSerializerOptions));
            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Error(ex.Message);
            return false;
        }
    }

    public static bool TryParse<TType>(in JsonNode node, string name, ref TType value)
    {
        try
        {
            value = node[name]!.GetValue<TType>();
            return true;
        }
        catch (Exception)
        {
            LoggerService.Error(
                $"Cannot Load {typeof(TType)}. Config file broken or version inconsistent? (file version {
                    node[nameof(ConfigInfoModel.Version)]?.GetValue<string>()}, app version {ConfigInfoModel.Version
                    })"
            );
            return false;
        }
    }

    public static bool TryParse<TBasic, TType>(
        in JsonNode node,
        string name,
        ref TType value,
        Func<TBasic, TType> converter
    )
    {
        try
        {
            value = converter(node[name]!.GetValue<TBasic>());
            return true;
        }
        catch (Exception)
        {
            LoggerService.Error(
                $"Cannot Load {typeof(TType)}. Config file broken or version inconsistent? (file version {
                    node[nameof(ConfigInfoModel.Version)]?.GetValue<string>()}, app version {ConfigInfoModel.Version
                    })"
            );
            return false;
        }
    }
}
