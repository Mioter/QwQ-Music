using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Models.ModelBase;
using QwQ_Music.Utilities;
using Log = QwQ_Music.Services.LoggerService;

namespace QwQ_Music.Services.ConfigIO;

public static class DataBaseService
{
    // ReSharper disable once InconsistentNaming
    private static SqliteConnection? _database;

    private static SqliteConnection Database
    {
        get
        {
            if (_database == null)
            {
                // 确保数据库文件所在目录存在
                PathEnsurer.EnsureFileAndDirectoryExist(MainConfig.DatabaseSavePath);

                _database = new SqliteConnection("data source=" + MainConfig.DatabaseSavePath + ";Foreign Keys=True;");
            }
            return _database;
        }
    }

    private static SqliteCommand Command
    {
        get
        {
            if (Database.State is ConnectionState.Connecting or ConnectionState.Closed)
                Database.Open();
            return Database.CreateCommand();
        }
    }

    // ReSharper disable InconsistentNaming
    public enum Sort
    {
        ASC,
        DESC,
    }

    public enum Table
    {
        MUSICS,
        PLAYLISTS,
        LISTNAMES,
    }

    // ReSharper restore InconsistentNaming

    private static async Task EnsureTableExistsAsync()
    {
        // 确保数据库文件存在
        if (!File.Exists(MainConfig.DatabaseSavePath))
        {
            // 确保目录存在
            PathEnsurer.EnsureFileAndDirectoryExist(MainConfig.DatabaseSavePath);

            // 如果连接已经创建，需要关闭并重新创建
            if (_database != null)
            {
                if (_database.State != ConnectionState.Closed)
                    await _database.CloseAsync();
                _database = null; // 强制重新创建连接
            }
        }

        // 创建表
        await using var music = Command;
        await using var playlist = Command;
        await using var names = Command;
        music.CommandText = $"""
            CREATE TABLE IF NOT EXISTS MUSICS(
            {nameof(ConfigInfoModel.Version)} TEXT NOT NULL,
            {nameof(MusicItemModel.Title)} TEXT NOT NULL PRIMARY KEY,
            {nameof(MusicItemModel.Artists)} TEXT ,
            {nameof(MusicItemModel.Composer)} TEXT ,
            {nameof(MusicItemModel.Album)} TEXT,
            BASICINFO TEXT NOT NULL UNIQUE,
            {nameof(MusicItemModel.FilePath)} TEXT NOT NULL UNIQUE,
            {nameof(MusicItemModel.FileSize)} TEXT NOT NULL,
            {nameof(MusicItemModel.CoverPath)} TEXT,
            {nameof(MusicItemModel.Current)} BLOB,
            {nameof(MusicItemModel.Duration)} BLOB NOT NULL,
            {nameof(MusicItemModel.CoverColors)} TEXT,
            {nameof(MusicItemModel.Gain)} REAL,
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

    private static void EnsureTableExists()
    {
        EnsureTableExistsAsync().Wait();
    }

    /// <summary>
    /// 异步从数据库加载数据。
    /// </summary>
    public static async IAsyncEnumerable<TConfig> LoadFromDatabaseAsync<TConfig>(
        Table table = Table.MUSICS,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC,
        Table? table2 = null
    )
        where TConfig : IModelBase<TConfig>
    {
        if (limit.Equals(default))
            limit = ..50;
        var check = EnsureTableExistsAsync().ConfigureAwait(false);
        await using var command = Command;
        SqliteDataReader? reader;
        try
        {
            await check;
            command.CommandText = $"SELECT * FROM {table:G}";
            if (table2 is not null)
                command.CommandText += $" JOIN {table2:G} ON {table:G}.BASICINFO = {table2:G}.BASICINFO ";
            if (search is not null)
                command.CommandText += " WHERE " + search;
            if (sortBy is not null)
                command.CommandText += $" ORDER BY {sortBy} {sort:G} LIMIT {limit.Start},{limit.End} ";
            reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            yield break;
        }

        if (!reader.HasRows)
            yield break;

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            yield return TConfig.Parse(reader);
        }
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
        Table? table2 = null
    )
        where TConfig : IModelBase<TConfig>
    {
        if (limit.Equals(default))
            limit = ..50;
        EnsureTableExists();
        using var command = Command;
        SqliteDataReader? reader;
        try
        {
            command.CommandText = $"SELECT * FROM {table:G}";
            if (table2 is not null)
                command.CommandText += $" JOIN {table2:G} ON {table:G}.BASICINFO = {table2:G}.BASICINFO ";
            if (search is not null)
                command.CommandText += search;
            if (sortBy is not null)
                command.CommandText += $" ORDER BY {sortBy} {sort:G} LIMIT {limit.Start},{limit.End} ";
            reader = command.ExecuteReader();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            yield break;
        }

        if (!reader.HasRows)
            yield break;

        while (reader.Read())
        {
            yield return TConfig.Parse(reader);
        }
    }

    /// <summary>
    /// 异步更新数据到数据库。
    /// </summary>
    public static async Task<bool> UpdateDataAsync<TConfig>(IModelBase<TConfig> model, Table table, string condition)
        where TConfig : IModelBase<TConfig>
    {
        try
        {
            var command = Command;
            var data = model.Dump();
            string formatted = "";
            foreach ((string? key, string? val) in data)
                formatted += $"{key} = {val},";

            command.CommandText = $"UPDATE {table:G} {formatted.TrimEnd(',')} WHERE {condition}";
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 同步更新数据到数据库。
    /// </summary>
    public static bool UpdateData<TConfig>(in IModelBase<TConfig> model, in Table table, in string condition)
        where TConfig : IModelBase<TConfig>
    {
        try
        {
            var command = Command;
            var data = model.Dump();
            string formatted = "";
            foreach ((string? key, string? val) in data)
                formatted += $"{key} = {val},";

            command.CommandText = $"UPDATE {table:G} {formatted.TrimEnd(',')} WHERE {condition}";
            command.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 异步保存数据到数据库。
    /// </summary>
    public static async Task<bool> SaveToDatabaseAsync<TConfig>(IModelBase<TConfig> model, Table table)
        where TConfig : IModelBase<TConfig>
    {
        try
        {
            var command = Command;
            var dict = model.Dump();

            // 检查记录是否已存在
            if (dict.TryGetValue(nameof(MusicItemModel.FilePath), out string? filePath))
            {
                var checkCommand = Command;
                checkCommand.CommandText =
                    $"SELECT COUNT(*) FROM {table:G} WHERE {nameof(MusicItemModel.FilePath)} = @FilePath";
                checkCommand.Parameters.AddWithValue("@FilePath", filePath);
                int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync().ConfigureAwait(false));

                // 如果记录已存在，执行更新而不是插入
                if (count > 0)
                {
                    return await UpdateDataAsync(model, table, $"{nameof(MusicItemModel.FilePath)} = {filePath}")
                        .ConfigureAwait(false);
                }
            }

            // 执行原来的插入逻辑
            var parameters = dict.Select(kv => new SqliteParameter($"@{kv.Key}", kv.Value));
            command.CommandText =
                $"INSERT INTO {table:G} ({string.Join(',', dict.Keys)}) VALUES ({string.Join(",", dict.Keys.Select(k => $"@{k}"))})";
            command.Parameters.AddRange(parameters.ToArray());
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 同步保存数据到数据库。
    /// </summary>
    public static bool SaveToDatabase<TConfig>(in IModelBase<TConfig> model, Table table)
        where TConfig : IModelBase<TConfig>
    {
        try
        {
            var command = Command;
            var dict = model.Dump();
            command.CommandText =
                $"INSERT INTO {table:G} ({string.Join(',', dict.Keys)}) VALUES ({string.Join(',', dict.Values)})";
            command.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
            return false;
        }
    }

    public static bool TryParse<TType>(in SqliteDataReader config, string ordinal, ref TType value)
    {
        try
        {
            value = config.GetFieldValue<TType>(config.GetOrdinal(ordinal));
            return true;
        }
        catch (Exception)
        {
            Log.Error(
                $"Cannot Load {typeof(TType)}. Database broken or config version inconsistent? (file version {
                    config.GetString(config.GetOrdinal(nameof(ConfigInfoModel.Version)))}, app version {
                        ConfigInfoModel.Version})"
            );
            return false;
        }
    }

    public static bool TryParse<TBasic, TType>(
        in SqliteDataReader config,
        string ordinal,
        ref TType value,
        Func<TBasic, TType> converter
    )
    {
        try
        {
            value = converter(config.GetFieldValue<TBasic>(config.GetOrdinal(ordinal)));
            return true;
        }
        catch (Exception)
        {
            Log.Error(
                $"Cannot Load {typeof(TType)}. Database broken or config version inconsistent? (file version {
                    config.GetString(config.GetOrdinal(nameof(ConfigInfoModel.Version)))}, app version {
                        ConfigInfoModel.Version})"
            );
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
        Sort sort = Sort.ASC
    )
    {
        if (limit.Equals(default))
            limit = ..50;
        var check = EnsureTableExistsAsync().ConfigureAwait(false);
        await using var command = Command;
        command.CommandText = $"SELECT {string.Join(',', ordinals)} FROM {table:G} ";
        if (search is not null)
            command.CommandText += $" WHERE {search}";
        if (sortBy is not null)
            command.CommandText += $" ORDER BY {sortBy} {sort:G} ";
        command.CommandText += $"LIMIT {limit.Start},{limit.End}";
        await check;
        var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        var result = new List<TResult>();
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            result.Add(converter(reader));
        }

        return result;
    }

    public static List<TResult> LoadFromDataBase<TResult>(
        Table table,
        string[] ordinals,
        Func<SqliteDataReader, TResult> converter,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC
    )
    {
        if (limit.Equals(default))
            limit = ..50;
        EnsureTableExists();
        using var command = Command;
        command.CommandText = $"SELECT {string.Join(',', ordinals)} FROM {table:G} ";
        if (search is not null)
            command.CommandText += $" WHERE {search}";
        if (sortBy is not null)
            command.CommandText += $" ORDER BY {sortBy} {sort:G} ";
        command.CommandText += $"LIMIT {limit.Start},{limit.End}";
        var reader = command.ExecuteReader();
        var result = new List<TResult>();
        while (reader.Read())
        {
            result.Add(converter(reader));
        }

        return result;
    }
}
