using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Utilities;
using Log = QwQ_Music.Services.LoggerService;

namespace QwQ_Music.Services.ConfigIO;

/// <summary>
/// 数据库服务，提供数据库操作的方法
/// </summary>
public static class DataBaseService
{
    #region 数据库连接管理

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

                _database = new SqliteConnection($"data source={MainConfig.DatabaseSavePath};Foreign Keys=True;");
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

    /// <summary>
    /// 关闭数据库连接
    /// </summary>
    public static async Task CloseConnectionAsync()
    {
        if (_database != null && _database.State != ConnectionState.Closed)
        {
            await _database.CloseAsync();
            _database.Dispose();
            _database = null;
        }
    }

    #endregion

    #region 枚举定义

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

    #endregion

    #region 表结构管理

    private static async Task EnsureTableExistsAsync()
    {
        // 确保数据库文件存在
        if (!File.Exists(MainConfig.DatabaseSavePath))
        {
            // 确保目录存在
            PathEnsurer.EnsureFileAndDirectoryExist(MainConfig.DatabaseSavePath);

            await CloseConnectionAsync();
        }

        // 创建表
        await using var music = Command;
        await using var playlist = Command;
        await using var names = Command;

        music.CommandText = $"""
            CREATE TABLE IF NOT EXISTS MUSICS(
            {nameof(MusicItemModel.Title)} TEXT NOT NULL PRIMARY KEY,
            {nameof(MusicItemModel.Artists)} TEXT,
            {nameof(MusicItemModel.Composer)} TEXT,
            {nameof(MusicItemModel.Album)} TEXT,
            {nameof(MusicItemModel.CoverPath)} TEXT,
            {nameof(MusicItemModel.FilePath)} TEXT NOT NULL UNIQUE,
            {nameof(MusicItemModel.FileSize)} TEXT NOT NULL,
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
            {nameof(PlaylistModel.Name)} TEXT NOT NULL,
            {nameof(MusicItemModel.FilePath)} TEXT NOT NULL,
            FOREIGN KEY({nameof(MusicItemModel.FilePath)}) REFERENCES MUSICS({nameof(MusicItemModel.FilePath)}))
            """;

        names.CommandText = $"""
            CREATE TABLE IF NOT EXISTS LISTNAMES(
            {nameof(PlaylistModel.Name)} TEXT NOT NULL UNIQUE PRIMARY KEY,
            {nameof(PlaylistModel.Count)} INTEGER NOT NULL,
            {nameof(PlaylistModel.LatestPlayedMusic)} TEXT NOT NULL)
            """;

        await music.ExecuteNonQueryAsync();
        await playlist.ExecuteNonQueryAsync();
        await names.ExecuteNonQueryAsync();
    }

    private static void EnsureTableExists()
    {
        EnsureTableExistsAsync().Wait();
    }

    #endregion

    #region 数据查询方法

    /// <summary>
    /// 异步从数据库加载数据，返回字典集合。
    /// </summary>
    public static async IAsyncEnumerable<Dictionary<string, object>> LoadDataAsync(
        Table table = Table.MUSICS,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC,
        Table? table2 = null
    )
    {
        if (limit.Equals(default))
            limit = ..50;

        var check = EnsureTableExistsAsync();
        await using var command = Command;
        SqliteDataReader? reader;

        try
        {
            await check;

            command.CommandText = $"SELECT * FROM {table:G}";

            if (table2 is not null)
                command.CommandText +=
                    $" JOIN {table2:G} ON {table:G}.{nameof(MusicItemModel.FilePath)} = {table2:G}.{nameof(MusicItemModel.FilePath)} ";

            if (search is not null)
            {
                // 检查搜索条件是否已包含 WHERE 关键字
                if (!search.Trim().StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText += " WHERE ";
                }
                command.CommandText += search + " ";
            }

            if (sortBy is not null)
                command.CommandText += $" ORDER BY {sortBy} {sort:G} LIMIT {limit.Start},{limit.End} ";

            reader = await command.ExecuteReaderAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"加载数据失败: {ex.Message}");
            yield break;
        }

        if (!reader.HasRows)
            yield break;

        while (await reader.ReadAsync())
        {
            yield return ReadRowToDictionary(reader);
        }
    }

    /// <summary>
    /// 同步从数据库加载数据，返回字典集合。
    /// </summary>
    public static IEnumerable<Dictionary<string, object>> LoadData(
        Table table = Table.MUSICS,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC,
        Table? table2 = null
    )
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
                command.CommandText +=
                    $" JOIN {table2:G} ON {table:G}.{nameof(MusicItemModel.FilePath)} = {table2:G}.{nameof(MusicItemModel.FilePath)} ";

            if (search is not null)
            {
                // 检查搜索条件是否已包含 WHERE 关键字
                if (!search.Trim().StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText += " WHERE ";
                }
                command.CommandText += search + " ";
            }

            if (sortBy is not null)
                command.CommandText += $" ORDER BY {sortBy} {sort:G} LIMIT {limit.Start},{limit.End} ";

            reader = command.ExecuteReader();
        }
        catch (Exception ex)
        {
            Log.Error($"加载数据失败: {ex.Message}");
            yield break;
        }

        if (!reader.HasRows)
            yield break;

        while (reader.Read())
        {
            yield return ReadRowToDictionary(reader);
        }
    }

    /// <summary>
    /// 从数据库中获取特定字段的值
    /// </summary>
    public static async Task<List<TResult>> LoadSpecifyFieldsAsync<TResult>(
        Table table,
        string[] ordinals,
        Func<Dictionary<string, object>, TResult> converter,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC
    )
    {
        if (limit.Equals(default))
            limit = ..50;

        var check = EnsureTableExistsAsync();
        await using var command = Command;

        command.CommandText = $"SELECT {string.Join(',', ordinals)} FROM {table:G} ";

        if (search is not null)
        {
            // 检查搜索条件是否已包含 WHERE 关键字
            if (!search.Trim().StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                command.CommandText += " WHERE ";
            }
            command.CommandText += search + " ";
        }

        if (sortBy is not null)
            command.CommandText += $" ORDER BY {sortBy} {sort:G} ";

        command.CommandText += $" LIMIT {limit.Start},{limit.End}"; // 添加了前导空格

        await check;
        var reader = await command.ExecuteReaderAsync();
        var result = new List<TResult>();

        while (await reader.ReadAsync())
        {
            var rowDict = ReadRowToDictionary(reader);
            result.Add(converter(rowDict));
        }

        return result;
    }

    /// <summary>
    /// 从数据库中获取特定字段的值
    /// </summary>
    public static List<TResult> LoadSpecifyFields<TResult>(
        Table table,
        string[] ordinals,
        Func<Dictionary<string, object>, TResult> converter,
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
        {
            // 检查搜索条件是否已包含 WHERE 关键字
            if (!search.Trim().StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                command.CommandText += " WHERE ";
            }
            command.CommandText += search + " ";
        }

        if (sortBy is not null)
            command.CommandText += $" ORDER BY {sortBy} {sort:G} ";

        command.CommandText += $" LIMIT {limit.Start},{limit.End}"; // 添加了前导空格

        var reader = command.ExecuteReader();
        var result = new List<TResult>();

        while (reader.Read())
        {
            var rowDict = ReadRowToDictionary(reader);
            result.Add(converter(rowDict));
        }

        return result;
    }

    /// <summary>
    /// 检查记录是否存在
    /// </summary>
    public static async Task<bool> RecordExistsAsync(Table table, string columnName, string value)
    {
        await EnsureTableExistsAsync();
        await using var command = Command;

        command.CommandText = $"SELECT COUNT(*) FROM {table:G} WHERE {columnName} = @value";
        command.Parameters.AddWithValue("@value", value);

        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// 检查记录是否存在
    /// </summary>
    public static bool RecordExists(Table table, string columnName, string value)
    {
        EnsureTableExists();
        using var command = Command;

        command.CommandText = $"SELECT COUNT(*) FROM {table:G} WHERE {columnName} = @value";
        command.Parameters.AddWithValue("@value", value);

        object? result = command.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }

    #endregion

    #region 数据修改方法

    /// <summary>
    /// 异步更新数据到数据库。
    /// </summary>
    public static async Task<bool> UpdateDataAsync(
        Dictionary<string, string?> data,
        Table table,
        string whereColumn,
        string whereValue
    )
    {
        try
        {
            await EnsureTableExistsAsync();
            await using var command = Command;

            // 使用参数化查询来防止SQL注入并正确处理特殊字符
            var setClause = (from key in data.Keys let paramName = $"@{key}" select $"{key} = {paramName}").ToList();

            // 处理SET参数
            foreach (var kv in data)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{kv.Key}";

                if (kv.Value == null)
                    parameter.Value = DBNull.Value;
                else
                    parameter.Value = kv.Value;

                command.Parameters.Add(parameter);
            }

            // 处理WHERE条件
            var whereParam = command.CreateParameter();
            whereParam.ParameterName = "@whereValue";
            whereParam.Value = whereValue;
            command.Parameters.Add(whereParam);

            command.CommandText =
                $"UPDATE {table:G} SET {string.Join(", ", setClause)} WHERE {whereColumn} = @whereValue";
            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"更新数据失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 同步更新数据到数据库。
    /// </summary>
    public static bool UpdateData(Dictionary<string, string?> data, Table table, string whereColumn, string whereValue)
    {
        try
        {
            EnsureTableExists();
            using var command = Command;

            // 使用参数化查询来防止SQL注入并正确处理特殊字符
            var setClause = (from key in data.Keys let paramName = $"@{key}" select $"{key} = {paramName}").ToList();

            // 处理SET参数
            foreach (var kv in data)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{kv.Key}";

                if (kv.Value == null)
                    parameter.Value = DBNull.Value;
                else
                    parameter.Value = kv.Value;

                command.Parameters.Add(parameter);
            }

            // 处理WHERE条件
            var whereParam = command.CreateParameter();
            whereParam.ParameterName = "@whereValue";
            whereParam.Value = whereValue;
            command.Parameters.Add(whereParam);

            command.CommandText =
                $"UPDATE {table:G} SET {string.Join(", ", setClause)} WHERE {whereColumn} = @whereValue";
            command.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"更新数据失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 异步插入数据到数据库。
    /// </summary>
    public static async Task<bool> InsertDataAsync(Dictionary<string, string?> data, Table table)
    {
        try
        {
            await EnsureTableExistsAsync();
            await using var command = Command;

            // 执行插入逻辑
            command.CommandText =
                $"INSERT INTO {table:G} ({string.Join(',', data.Keys)}) VALUES ({string.Join(",", data.Keys.Select(k => $"@{k}"))})";

            // 避免装箱的参数添加方式
            foreach (var kv in data)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{kv.Key}";

                if (kv.Value == null)
                    parameter.Value = DBNull.Value;
                else
                    parameter.Value = kv.Value;

                command.Parameters.Add(parameter);
            }

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"插入数据失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 同步插入数据到数据库。
    /// </summary>
    public static bool InsertData(Dictionary<string, string?> data, Table table)
    {
        try
        {
            EnsureTableExists();
            using var command = Command;

            // 执行插入逻辑
            command.CommandText =
                $"INSERT INTO {table:G} ({string.Join(',', data.Keys)}) VALUES ({string.Join(",", data.Keys.Select(k => $"@{k}"))})";

            // 避免装箱的参数添加方式
            foreach (var kv in data)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{kv.Key}";

                if (kv.Value == null)
                    parameter.Value = DBNull.Value;
                else
                    parameter.Value = kv.Value;

                command.Parameters.Add(parameter);
            }

            command.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"插入数据失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 异步删除数据
    /// </summary>
    public static async Task<bool> DeleteDataAsync(Table table, string whereColumn, string whereValue)
    {
        try
        {
            await EnsureTableExistsAsync();
            await using var command = Command;

            command.CommandText = $"DELETE FROM {table:G} WHERE {whereColumn} = @whereValue";
            command.Parameters.AddWithValue("@whereValue", whereValue);

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"删除数据失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 同步删除数据
    /// </summary>
    public static bool DeleteData(Table table, string whereColumn, string whereValue)
    {
        try
        {
            EnsureTableExists();
            using var command = Command;

            command.CommandText = $"DELETE FROM {table:G} WHERE {whereColumn} = @whereValue";
            command.Parameters.AddWithValue("@whereValue", whereValue);

            command.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"删除数据失败: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 将SqliteDataReader的当前行转换为字典
    /// </summary>
    private static Dictionary<string, object> ReadRowToDictionary(SqliteDataReader reader)
    {
        var result = new Dictionary<string, object>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            string columnName = reader.GetName(i);
            if (!reader.IsDBNull(i))
            {
                // 根据列的类型获取适当的值
                object value = reader.GetValue(i);
                result[columnName] = value;
            }
            else
            {
                result[columnName] = null!;
            }
        }
        return result;
    }

    #endregion
}
