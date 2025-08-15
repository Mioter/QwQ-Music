using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace QwQ_Music.Common.Services.Databases;

/// <summary>
///     提供基于 Sqlite 的数据库操作服务，包括建表、删表、增删改查等常用功能。
/// </summary>
public class DatabaseService : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    /// <summary>
    ///     初始化数据库服务。
    /// </summary>
    /// <param name="dbPath">数据库文件路径</param>
    public DatabaseService(string dbPath)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
            throw new ArgumentException("数据库路径不能为空。", nameof(dbPath));

        string? directory = Path.GetDirectoryName(dbPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string connectionString = $"Data Source={dbPath}";
        _connection = new SqliteConnection(connectionString);
    }

    /// <summary>
    ///     异步释放数据库连接资源。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     异步打开数据库连接。
    /// </summary>
    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
    }

    #region 查询操作

    /// <summary>
    ///     执行查询，返回结果集。
    /// </summary>
    /// <param name="sql">SQL 查询语句</param>
    /// <param name="parameters">参数字典</param>
    /// <returns>结果集，每行是一个字典</returns>
    public async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL 语句不能为空。", nameof(sql));

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        if (parameters != null)
        {
            foreach ((string key, object value) in parameters)
            {
                cmd.Parameters.AddWithValue($"@{key}", value);
            }
        }

        var result = new List<Dictionary<string, object?>>();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            result.Add(row);
        }

        return result;
    }

    #endregion

    #region 迭代读取查询

    /// <summary>
    ///     异步执行查询，返回可迭代的结果集。
    /// </summary>
    /// <param name="sql">SQL 查询语句</param>
    /// <param name="parameters">参数字典</param>
    /// <returns>可迭代的结果集，每行是一个字典</returns>
    public async IAsyncEnumerable<Dictionary<string, object?>> QueryAsyncEnumerable(string sql, Dictionary<string, object>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL 语句不能为空。", nameof(sql));

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        if (parameters != null)
        {
            foreach ((string key, object value) in parameters)
            {
                cmd.Parameters.AddWithValue($"@{key}", value);
            }
        }

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            yield return row;
        }
    }

    #endregion

    #region 工具方法

    /// <summary>
    ///     转义标识符以防止关键字冲突。
    /// </summary>
    /// <param name="identifier">标识符名称</param>
    /// <returns>转义后的标识符</returns>
    private static string EscapeIdentifier(string identifier)
    {
        // SQLite 使用双引号转义标识符
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    #endregion

    #region 表结构操作

    /// <summary>
    ///     创建表（如果不存在）。
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="columnsDefinition">字段定义，如 "id INTEGER PRIMARY KEY, name TEXT"</param>
    public async Task CreateTableAsync(string tableName, string columnsDefinition)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        if (string.IsNullOrWhiteSpace(columnsDefinition))
            throw new ArgumentException("列定义不能为空。", nameof(columnsDefinition));

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {EscapeIdentifier(tableName)} ({columnsDefinition});";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     删除表（如果存在）。
    /// </summary>
    /// <param name="tableName">表名</param>
    public async Task DropTableAsync(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS {EscapeIdentifier(tableName)};";
        await cmd.ExecuteNonQueryAsync();
    }

    #endregion

    #region 数据操作

    /// <summary>
    ///     插入一条数据。
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="data">字段名与值的字典</param>
    public async Task InsertAsync(string tableName, Dictionary<string, object?> data)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        if (data == null || data.Count == 0)
            throw new ArgumentException("插入数据不能为空。", nameof(data));

        string columns = string.Join(", ", data.Keys.Select(EscapeIdentifier));
        string paramNames = string.Join(", ", data.Keys.Select(k => "@" + k));
        using var cmd = _connection.CreateCommand();

        foreach ((string key, object? value) in data)
        {
            cmd.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);
        }

        cmd.CommandText = $"INSERT INTO {EscapeIdentifier(tableName)} ({columns}) VALUES ({paramNames});";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     更新数据。
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="data">要更新的字段名与值</param>
    /// <param name="whereClause">WHERE 子句（不含 WHERE 关键字）</param>
    /// <param name="whereParams">WHERE 子句参数</param>
    public async Task UpdateAsync(
        string tableName,
        Dictionary<string, object?> data,
        string whereClause,
        Dictionary<string, object?>? whereParams = null
        )
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        if (data == null || data.Count == 0)
            throw new ArgumentException("更新数据不能为空。", nameof(data));

        if (string.IsNullOrWhiteSpace(whereClause))
            throw new ArgumentException("WHERE 条件不能为空。", nameof(whereClause));

        string setClause = string.Join(", ", data.Keys.Select(k => $"{EscapeIdentifier(k)} = @{k}"));
        using var cmd = _connection.CreateCommand();

        foreach ((string key, object? value) in data)
        {
            cmd.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);
        }

        if (whereParams != null)
        {
            foreach ((string key, object? value) in whereParams)
            {
                cmd.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);
            }
        }

        cmd.CommandText = $"UPDATE {EscapeIdentifier(tableName)} SET {setClause} WHERE {whereClause};";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    ///     删除数据。
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="whereClause">WHERE 子句（不含 WHERE 关键字）</param>
    /// <param name="whereParams">WHERE 子句参数</param>
    public async Task DeleteAsync(string tableName, string whereClause, Dictionary<string, object>? whereParams = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        if (string.IsNullOrWhiteSpace(whereClause))
            throw new ArgumentException("WHERE 条件不能为空。", nameof(whereClause));

        using var cmd = _connection.CreateCommand();

        if (whereParams != null)
        {
            foreach ((string key, object value) in whereParams)
            {
                cmd.Parameters.AddWithValue($"@{key}", value);
            }
        }

        cmd.CommandText = $"DELETE FROM {EscapeIdentifier(tableName)} WHERE {whereClause};";
        await cmd.ExecuteNonQueryAsync();
    }

    #endregion

    #region 事务支持

    private SqliteTransaction? _transaction;

    /// <summary>
    ///     开始事务
    /// </summary>
    public void BeginTransaction()
    {
        if (_transaction != null)
            throw new InvalidOperationException("事务已在进行中");

        _transaction = _connection.BeginTransaction();
    }

    /// <summary>
    ///     提交事务
    /// </summary>
    public void Commit()
    {
        if (_transaction == null)
            throw new InvalidOperationException("没有活动的事务");

        _transaction.Commit();
        _transaction.Dispose();
        _transaction = null;
    }

    /// <summary>
    ///     回滚事务
    /// </summary>
    public void Rollback()
    {
        if (_transaction == null)
            throw new InvalidOperationException("没有活动的事务");

        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = null;
    }

    /// <summary>
    ///     执行 SQL 命令（用于 UPDATE/DELETE 等操作）
    /// </summary>
    public async Task ExecuteAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL 语句不能为空。", nameof(sql));

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        if (parameters != null)
        {
            foreach ((string key, object value) in parameters)
            {
                cmd.Parameters.AddWithValue($"@{key}", value);
            }
        }

        cmd.Transaction = _transaction;
        await cmd.ExecuteNonQueryAsync();
    }

    #endregion
}
