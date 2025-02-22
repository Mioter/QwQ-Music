using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using QwQ_Music.Models;
using Microsoft.Data.Sqlite;
using Log = QwQ_Music.Services.LoggerService;

namespace QwQ_Music.Utilities;

/// <summary>
/// Json 配置类，提供读取和写入 JSON 的方法。
/// </summary>
public static class ConfigIO {
    #region StaticConfig JsonReadWrite

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
    private static readonly string ConfigDefaultPath = Path.Join(Directory.GetCurrentDirectory(), "config");


    /// <summary>
    /// 异步从 JSON 文件加载数据。
    /// </summary>
    /// <returns>加载的数据对象。</returns>
    /// <exception cref="FileNotFoundException">当 JSON 文件不存在时抛出。</exception>
    public static async Task<bool> LoadFromJsonAsync<TConfig>(string? configPath = null)
        where TConfig : IStaticConfigBase {
        if (!File.Exists(TConfig.FileName)) {
            Log.Error($"Config file {configPath ?? ConfigDefaultPath}/{TConfig.FileName}.QwQConf not found");
            return false;
        }

        try {
            await using var fileStream = new FileStream(
                Path.Join(configPath ?? ConfigDefaultPath, $"{TConfig.FileName}.QwQConf"),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            return TConfig.Parse(await JsonNode.ParseAsync(fileStream).ConfigureAwait(false));
        } catch (Exception ex) {
            Log.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 同步从 JSON 文件加载数据。
    /// </summary>
    /// <returns>加载的数据对象。</returns>
    /// <exception cref="FileNotFoundException">当 JSON 文件不存在时抛出。</exception>
    public static bool LoadFromJson<TConfig>(string? configPath = null) where TConfig : IStaticConfigBase {
        if (!File.Exists(Path.Join(configPath ?? ConfigDefaultPath, $"{TConfig.FileName}.QwQConf"))) {
            Log.Error(
                $"Config file {Path.Join(configPath ?? ConfigDefaultPath, $"{TConfig.FileName}.QwQConf")} not found");
            return false;
        }

        try {
            using var fileStream = new FileStream(
                Path.Join(configPath ?? ConfigDefaultPath, $"{TConfig.FileName}.QwQConf"),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            return TConfig.Parse(JsonNode.Parse(fileStream));
        } catch (Exception ex) {
            Log.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 异步保存数据到 JSON 文件。
    /// </summary>
    public static async Task<bool> SaveToJsonAsync<TConfig>(string? configPath = null)
        where TConfig : IStaticConfigBase {
        try {
            await new StreamWriter(
                    new FileStream(
                        Path.Join(configPath ?? ConfigDefaultPath, $"{TConfig.FileName}.QwQConf"),
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.Read)).WriteAsync(TConfig.Dump().ToJsonString(JsonSerializerOptions))
                .ConfigureAwait(false);
            return true;
        } catch (Exception ex) {
            Log.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 同步保存数据到 JSON 文件。
    /// </summary>
    public static bool SaveToJson<TConfig>(string? configPath = null) where TConfig : IStaticConfigBase {
        try {
            new StreamWriter(
                new FileStream(
                    Path.Join(configPath ?? ConfigDefaultPath, $"{TConfig.FileName}.QwQConf"),
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read)).Write(TConfig.Dump().ToJsonString(JsonSerializerOptions));
            return true;
        } catch (Exception ex) {
            Log.Error(ex.Message);
            return false;
        }
    }

    #endregion StaticConfig JsonReadWrite

    #region ObjectConfig DataBaseReadWrite

    // ReSharper disable once InconsistentNaming
    private static readonly SqliteConnection Database = new(
        "data source=" + MainConfig.DatabaseSavePath + ";Version=3;");

    private static SqliteCommand Command {
        get {
            if (Database.State is ConnectionState.Connecting or ConnectionState.Closed) Database.Open();
            return Database.CreateCommand();
        }
    }

    public struct Sort {
        public static string Asc = "Order By ASC";
        public static string Desc = "Order By DESC";
    }

    private static async Task EnsureTableExistsAsync() {
        if (!File.Exists(MainConfig.DatabaseSavePath))
            _ = new FileStream(MainConfig.DatabaseSavePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var command = Command;
        command.CommandText = $"""
                               CREATE TABLE IF NOT EXISTS MUSICS(
                               {nameof(ConfigInfoModel.Version)} TEXT NOT NULL,
                               {nameof(MusicItemModel.Title)} TEXT PRIMARY KEY,
                               {nameof(MusicItemModel.Artists)} BLOB,
                               {nameof(MusicItemModel.Album)} TEXT,
                               {nameof(MusicItemModel.FilePath)} TEXT NOT NULL,
                               {nameof(MusicItemModel.FileSize)} TEXT NOT NULL,
                               {nameof(MusicItemModel.CoverPath)} TEXT,
                               {nameof(MusicItemModel.Current)} BLOB,
                               {nameof(MusicItemModel.Duration)} BLOB NOT NULL,
                               {nameof(MusicItemModel.Gain)} REAL NOT NULL,
                               {nameof(MusicItemModel.EncodingFormat)} TEXT NOT NULL,
                               {nameof(MusicItemModel.Comment)} TEXT,
                               {nameof(MusicItemModel.Remarks)} TEXT)
                               """;
    }

    private static void EnsureTableExists() { EnsureTableExistsAsync().Wait(); }

    /// <summary>
    /// 异步从 database 文件加载数据。
    /// </summary>
    /// <returns>加载的数据对象。</returns>
    public static async IAsyncEnumerable<TConfig> LoadFromDatabaseAsync<TConfig>(
        string column = "Title",
        Range? limit = null,
        Sort? sort = null) where TConfig : IConfigBase<TConfig> {
        var rst = EnsureTableExistsAsync().ConfigureAwait(false);
        limit ??= ..100;
        await using var command = Command;
        SqliteDataReader? reader;
        try {
            await rst;
            command.CommandText = $"SELECT * FROM MUSICS LIMIT {limit.Value.Start},{limit.Value.End} ";
            if (sort is not null) command.CommandText += $"ORDER BY {column} {sort.Value}";
            reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        } catch (Exception ex) {
            Log.Error(ex.Message);
            yield break;
        }

        if (!reader.HasRows) yield break;
        do { yield return TConfig.Parse(reader); } while (await reader.ReadAsync().ConfigureAwait(false));
    }

    /// <summary>
    /// 同步从 JSON 文件加载数据。
    /// </summary>
    /// <returns>加载的数据对象。</returns>
    /// <exception cref="FileNotFoundException">当 JSON 文件不存在时抛出。</exception>
    public static IEnumerable<TConfig> LoadFromDatabase<TConfig>(
        string column = "Title",
        Range? limit = null,
        Sort? sort = null) where TConfig : IConfigBase<TConfig> {
        limit ??= ..100;
        EnsureTableExists();
        using var command = Command;
        SqliteDataReader? reader;
        try {
            command.CommandText = $"SELECT * FROM MUSICS LIMIT {limit.Value.Start},{limit.Value.End} ";
            if (sort is not null) command.CommandText += $"ORDER BY {column} {sort.Value}";
            reader = command.ExecuteReader();
        } catch (Exception ex) {
            Log.Error(ex.Message);
            yield break;
        }

        if (!reader.HasRows) yield break;
        do { yield return TConfig.Parse(reader); } while (reader.Read());
    }

    /// <summary>
    /// 异步保存数据到 JSON 文件。
    /// </summary>
    public static async Task<bool> SaveToDatabaseAsync<TConfig>(IConfigBase<TConfig> config, string? configPath = null)
        where TConfig : IConfigBase<TConfig> {
        try {
            var command = Command;
            command.CommandText = $"INSERT INTO MUSICS {config.Dump()}";
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            return true;
        } catch (Exception ex) {
            Log.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 同步保存数据到 JSON 文件。
    /// </summary>
    public static bool SaveToDatabase<TConfig>(IConfigBase<TConfig> config, string? configPath = null)
        where TConfig : IConfigBase<TConfig> {
        try {
            var command = Command;
            command.CommandText = $"INSERT INTO MUSICS {config.Dump()}";
            command.ExecuteNonQuery();
            return true;
        } catch (Exception ex) {
            Log.Error(ex.Message);
            return false;
        }
    }

    #endregion ObjectConfig DataBaseReadWrite
}