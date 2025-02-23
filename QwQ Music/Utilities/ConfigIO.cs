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
/// 提供读取和写入配置的方法。
/// </summary>
public static class ConfigIO {
    #region StaticConfig JsonReadWrite

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
    private static readonly string ConfigDefaultPath = Path.Join(Directory.GetCurrentDirectory(), "config");


    /// <summary>
    /// 异步从 JSON 文件加载数据。
    /// </summary>
    public static async Task<bool> LoadFromJsonAsync<TConfig>(string? configPath = null) where TConfig : IConfigBase {
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
    public static bool LoadFromJson<TConfig>(string? configPath = null) where TConfig : IConfigBase {
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
    public static async Task<bool> SaveToJsonAsync<TConfig>(string? configPath = null) where TConfig : IConfigBase {
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
    public static bool SaveToJson<TConfig>(string? configPath = null) where TConfig : IConfigBase {
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

    public static bool TryParse<TType>(in JsonNode node, string name, ref TType value) {
        try {
            value = node[name]!.GetValue<TType>();
            return true;
        } catch (Exception) {
            Log.Error(
                $"Cannot Load {typeof(TType)}. Config file broken or version inconsistent? (file version {
                    node[nameof(ConfigInfoModel.Version)]?.GetValue<string>()}, app version {ConfigInfoModel.Version
                    })");
            return false;
        }
    }

    public static bool TryParse<TBasic, TType>(
        in JsonNode node,
        string name,
        ref TType value,
        Func<TBasic, TType> converter) {
        try {
            value = converter(node[name]!.GetValue<TBasic>());
            return true;
        } catch (Exception) {
            Log.Error(
                $"Cannot Load {typeof(TType)}. Config file broken or version inconsistent? (file version {
                    node[nameof(ConfigInfoModel.Version)]?.GetValue<string>()}, app version {ConfigInfoModel.Version
                    })");
            return false;
        }
    }

    #endregion StaticConfig JsonReadWrite

    #region ObjectConfig DataBaseReadWrite

    // ReSharper disable once InconsistentNaming
    private static readonly SqliteConnection Database = new(
        "data source=" + MainConfig.DatabaseSavePath + ";Version=3;Foreign Keys=True;");

    private static SqliteCommand Command {
        get {
            if (Database.State is ConnectionState.Connecting or ConnectionState.Closed) Database.Open();
            return Database.CreateCommand();
        }
    }

    // ReSharper disable InconsistentNaming
    public enum Sort { ASC, DESC }

    public enum Table { MUSICS, PLAYLISTS, LISTNAMES }
    // ReSharper restore InconsistentNaming

    private static async Task EnsureTableExistsAsync() {
        if (!File.Exists(MainConfig.DatabaseSavePath))
            _ = new FileStream(MainConfig.DatabaseSavePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var music = Command;
        await using var playlist = Command;
        await using var names = Command;
        music.CommandText = $"""
                             CREATE TABLE IF NOT EXISTS MUSICS(
                             {nameof(ConfigInfoModel.Version)} TEXT NOT NULL,
                             {nameof(MusicItemModel.Title)} TEXT NOT NULL PRIMARY KEY,
                             {nameof(MusicItemModel.Artists)} TEXT,
                             {nameof(MusicItemModel.Album)} TEXT,
                             BASICINFO TEXT NOT NULL UNIQUE,
                             {nameof(MusicItemModel.FilePath)} TEXT NOT NULL UNIQUE,
                             {nameof(MusicItemModel.FileSize)} TEXT NOT NULL,
                             {nameof(MusicItemModel.CoverPath)} TEXT,
                             {nameof(MusicItemModel.Current)} BLOB,
                             {nameof(MusicItemModel.Duration)} BLOB NOT NULL,
                             {nameof(MusicItemModel.Gain)} REAL NOT NULL,
                             {nameof(MusicItemModel.EncodingFormat)} TEXT NOT NULL,
                             {nameof(MusicItemModel.Comment)} TEXT,
                             {nameof(MusicItemModel.Remarks)} TEXT)
                             """;
        playlist.CommandText = $"""
                                CREATE TABLE IF NOT EXISTS PLAYLISTS(
                                {nameof(PlaylistModel.Name)} TEXT NOT NULL PRIMARY KEY,
                                BASICINFO TEXT NOT NULL,
                                FOREIGN KEY(BASICINFO) REFERENCES MUSICS(BASICINFO))
                                """;
        names.CommandText = $"""
                             CREATE TABLE IF NOT EXISTS LISTNAMES(
                             {nameof(PlaylistModel.Name)} TEXT NOT NULL UNIQUE PRIMARY KEY,
                             {nameof(PlaylistModel.Count)} INTEGER NOT NULL,
                             {nameof(PlaylistModel.LatestPlayedMusic)} TEXT NOT NULL)
                             """;
        var wait = names.ExecuteNonQueryAsync().ConfigureAwait(false);
        await music.ExecuteNonQueryAsync().ConfigureAwait(false);
        await playlist.ExecuteNonQueryAsync().ConfigureAwait(false);
        await wait;
    }

    private static void EnsureTableExists() { EnsureTableExistsAsync().Wait(); }

    /// <summary>
    /// 异步从数据库加载数据。
    /// </summary>
    public static async IAsyncEnumerable<TConfig> LoadFromDatabaseAsync<TConfig>(
        Table table = Table.MUSICS,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC,
        Table? table2 = null) where TConfig : IModelBase<TConfig> {
        if (limit.Equals(default)) limit = ..50;
        var check = EnsureTableExistsAsync().ConfigureAwait(false);
        await using var command = Command;
        SqliteDataReader? reader;
        try {
            await check;
            command.CommandText = $"SELECT * FROM {table:G}";
            if (table2 is not null)
                command.CommandText += $" JOIN {table2:G} ON {table:G}.BASICINFO = {table2:G}.BASICINFO ";
            if (search is not null) command.CommandText += " WHERE " + search;
            if (sortBy is not null)
                command.CommandText += $" ORDER BY {sortBy} {sort:G} LIMIT {limit.Start},{limit.End} ";
            reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        } catch (Exception ex) {
            Log.Error(ex.Message);
            yield break;
        }

        if (!reader.HasRows) yield break;
        do { yield return TConfig.Parse(reader); } while (await reader.ReadAsync().ConfigureAwait(false));
    }

    /// <summary>
    /// 同步从数据库加载数据。
    /// </summary>
    public static IEnumerable<TConfig> LoadFromDatabase<TConfig>(
        Table table = Table.MUSICS,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC,
        Table? table2 = null) where TConfig : IModelBase<TConfig> {
        if (limit.Equals(default)) limit = ..50;
        EnsureTableExists();
        using var command = Command;
        SqliteDataReader? reader;
        try {
            command.CommandText = $"SELECT * FROM {table:G}";
            if (table2 is not null)
                command.CommandText += $" JOIN {table2:G} ON {table:G}.BASICINFO = {table2:G}.BASICINFO ";
            if (search is not null) command.CommandText += search;
            if (sortBy is not null)
                command.CommandText += $" ORDER BY {sortBy} {sort:G} LIMIT {limit.Start},{limit.End} ";
            reader = command.ExecuteReader();
        } catch (Exception ex) {
            Log.Error(ex.Message);
            yield break;
        }

        if (!reader.HasRows) yield break;
        do { yield return TConfig.Parse(reader); } while (reader.Read());
    }

    /// <summary>
    /// 异步保存数据到数据库。
    /// </summary>
    public static async Task<bool> UpdateDataAsync<TConfig>(IModelBase<TConfig> model, Table table, string condition)
        where TConfig : IModelBase<TConfig> {
        try {
            var command = Command;
            var data = model.Dump();
            string formatted = "";
            foreach (var (key, val) in data) formatted += $"{key} = {val},";

            command.CommandText = $"UPDATE {table:G} {formatted.TrimEnd(',')} WHERE {condition}";
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            return true;
        } catch (Exception ex) {
            Log.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 同步保存数据到数据库。
    /// </summary>
    public static bool UpdateData<TConfig>(in IModelBase<TConfig> model, in Table table, in string condition)
        where TConfig : IModelBase<TConfig> {
        try {
            var command = Command;
            var data = model.Dump();
            string formatted = "";
            foreach (var (key, val) in data) formatted += $"{key} = {val},";

            command.CommandText = $"UPDATE {table:G} {formatted.TrimEnd(',')} WHERE {condition}";
            command.ExecuteNonQuery();
            return true;
        } catch (Exception ex) {
            Log.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 异步保存数据到数据库。
    /// </summary>
    public static async Task<bool> SaveToDatabaseAsync<TConfig>(IModelBase<TConfig> model, Table table)
        where TConfig : IModelBase<TConfig> {
        try {
            var command = Command;
            command.CommandText = $"INSERT INTO {table:G} {model.Dump()}";
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            return true;
        } catch (Exception ex) {
            Log.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 同步保存数据到数据库。
    /// </summary>
    public static bool SaveToDatabase<TConfig>(in IModelBase<TConfig> model, Table table)
        where TConfig : IModelBase<TConfig> {
        try {
            var command = Command;
            command.CommandText = $"INSERT INTO {table:G} {model.Dump()}";
            command.ExecuteNonQuery();
            return true;
        } catch (Exception ex) {
            Log.Error(ex.Message);
            return false;
        }
    }

    public static bool TryParse<TType>(in SqliteDataReader config, string ordinal, ref TType value) {
        try {
            value = config.GetFieldValue<TType>(config.GetOrdinal(ordinal));
            return true;
        } catch (Exception) {
            Log.Error(
                $"Cannot Load {typeof(TType)}. Database broken or config version inconsistent? (file version {
                    config.GetString(config.GetOrdinal(nameof(ConfigInfoModel.Version)))}, app version {
                        ConfigInfoModel.Version})");
            return false;
        }
    }

    public static bool TryParse<TBasic, TType>(
        in SqliteDataReader config,
        string ordinal,
        ref TType value,
        Func<TBasic, TType> converter) {
        try {
            value = converter(config.GetFieldValue<TBasic>(config.GetOrdinal(ordinal)));
            return true;
        } catch (Exception) {
            Log.Error(
                $"Cannot Load {typeof(TType)}. Database broken or config version inconsistent? (file version {
                    config.GetString(config.GetOrdinal(nameof(ConfigInfoModel.Version)))}, app version {
                        ConfigInfoModel.Version})");
            return false;
        }
    }


    public static async Task<List<TResult>> LoadFromDataBaseAsync<TResult>(
        Table table,
        string[] ordinals,
        Func<SqliteDataReader, TResult> converter,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC) {
        if (limit.Equals(default)) limit = ..50;
        var check = EnsureTableExistsAsync().ConfigureAwait(false);
        await using var command = Command;
        command.CommandText = $"SELECT {string.Join(',', ordinals)} FROM {table:G} ";
        if (search is not null) command.CommandText += $" WHERE {search}";
        if (sortBy is not null) command.CommandText += $" ORDER BY {sortBy} {sort:G} ";
        command.CommandText += $"LIMIT {limit.Start},{limit.End}";
        await check;
        var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        var result = new List<TResult>();
        while (await reader.ReadAsync().ConfigureAwait(false)) result.Add(converter(reader));
        return result;
    }


    public static List<TResult> LoadFromDataBase<TResult>(
        Table table,
        string[] ordinals,
        Func<SqliteDataReader, TResult> converter,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC) {
        if (limit.Equals(default)) limit = ..50;
        EnsureTableExists();
        using var command = Command;
        command.CommandText = $"SELECT {string.Join(',', ordinals)} FROM {table:G} ";
        if (search is not null) command.CommandText += $" WHERE {search}";
        if (sortBy is not null) command.CommandText += $" ORDER BY {sortBy} {sort:G} ";
        command.CommandText += $"LIMIT {limit.Start},{limit.End}";
        var reader = command.ExecuteReader();
        var result = new List<TResult>();
        while (reader.Read()) result.Add(converter(reader));
        return result;
    }

    #endregion ObjectConfig DataBaseReadWrite
}