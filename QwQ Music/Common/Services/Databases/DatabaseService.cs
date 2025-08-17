using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace QwQ_Music.Common.Services.Databases;

/// <summary>
///     提供基于 Sqlite 的数据库操作服务，包括建表、删表、增删改查等常用功能。
/// </summary>
public class DatabaseService : IDisposable
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
        _connection.Open(); // 构造时自动打开连接
    }

    /// <summary>
    ///     释放数据库连接资源。
    /// </summary>
    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    #region 查询操作

    /// <summary>
    ///     执行查询，返回结果集。
    /// </summary>
    /// <param name="sql">SQL 查询语句</param>
    /// <param name="parameters">参数字典</param>
    /// <returns>结果集，每行是一个字典</returns>
    public List<Dictionary<string, object?>> Query(string sql, Dictionary<string, object>? parameters = null)
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
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
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

    #region 工具方法

    /// <summary>
    ///     转义标识符以防止关键字冲突。
    /// </summary>
    /// <param name="identifier">标识符名称</param>
    /// <returns>转义后的标识符</returns>
    private static string EscapeIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    #endregion

    #region 表结构操作

    /// <summary>
    ///     创建表（如果不存在）。
    /// </summary>
    public void CreateTable(string tableName, string columnsDefinition)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        if (string.IsNullOrWhiteSpace(columnsDefinition))
            throw new ArgumentException("列定义不能为空。", nameof(columnsDefinition));

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {EscapeIdentifier(tableName)} ({columnsDefinition});";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    ///     删除表（如果存在）。
    /// </summary>
    public void DropTable(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS {EscapeIdentifier(tableName)};";
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region 数据操作

    /// <summary>
    ///     插入一条数据。
    /// </summary>
    public void Insert(string tableName, Dictionary<string, object?> data)
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
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    ///     更新数据。
    /// </summary>
    public void Update(
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
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    ///     删除数据。
    /// </summary>
    public void Delete(string tableName, string whereClause, Dictionary<string, object>? whereParams = null)
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
        cmd.ExecuteNonQuery();
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
    public void Execute(string sql, Dictionary<string, object>? parameters = null)
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
        cmd.ExecuteNonQuery();
    }

    #endregion
}
