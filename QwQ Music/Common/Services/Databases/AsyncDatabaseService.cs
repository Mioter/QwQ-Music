using Microsoft.Data.Sqlite;

namespace QwQ_Music.Common.Services.Databases;

[AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
public class NotNullIf(string parameterName, bool condition) : Attribute {
    public string ParameterName { get; } = parameterName;
    public bool Condition { get; } = condition;
}

public enum InsertExist {
    FAIL, IGNORE, REPLACE
}

/// <summary>
///     提供基于 Sqlite 的数据库操作服务，包括建表、删表、增删改查等常用功能。
/// </summary>
public class AsyncDatabaseService : IAsyncDisposable {
    private readonly SqliteConnection _connection;

    /// <summary>
    ///     初始化数据库服务。
    /// </summary>
    /// <param name="dbPath">数据库文件路径</param>
    public AsyncDatabaseService(string dbPath) {
        if (string.IsNullOrWhiteSpace(dbPath))
            throw new ArgumentException("数据库路径不能为空。", nameof(dbPath));

        string? directory = Path.GetDirectoryName(dbPath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string connectionString = $"Data Source={dbPath};Foreign Keys=True;";
        _connection = new SqliteConnection(connectionString);
        _connection.Open(); // 构造时自动打开连接
    }

    /// <summary>
    ///     释放数据库连接资源。
    /// </summary>
    public async ValueTask DisposeAsync() {
        await _connection.CloseAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    #region 工具方法

    /// <summary>
    ///     转义标识符以防止关键字冲突。
    /// </summary>
    /// <param name="identifier">标识符名称</param>
    /// <returns>转义后的标识符</returns>
    private static string EscapeIdentifier(string identifier) { return $"\"{identifier.Replace("\"", "\"\"")}\""; }

    #endregion


    public SqliteCommand NewCommand() { return _connection.CreateCommand(); }

    #region 查询操作

    /// <summary>
    ///     执行查询，返回结果集。
    /// </summary>
    /// <param name="sql">SQL 查询语句</param>
    /// <param name="parameters">参数字典</param>
    /// <param name="skip">跳过项目数</param>
    /// <param name="limit">最多提取量</param>
    /// <returns>结果集，每行是一个字典</returns>
    public async Task<List<Dictionary<string, object?>>> QueryAsync(
        string sql,
        Dictionary<string, object>? parameters = null,
        int skip = 0,
        int limit = -1) {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL 语句不能为空。", nameof(sql));

        if (limit - skip > 500)
            await LoggerService.DebugAsync("正在获取大量信息。警告：可能发生长时磁盘IO").ConfigureAwait(false);

        // Combine Skip&Limit
        if (skip != 0) {
            if (limit != -1)
                sql += $" LIMIT {skip},{limit} ";
            else
                sql += $" OFFSET {skip} ";
        } else if (limit != -1) {
            sql += $" LIMIT {limit} ";
        }

        await using SqliteCommand cmd = NewCommand();
        cmd.CommandText = sql;

        if (parameters != null)
            foreach ((string key, object value) in parameters)
                cmd.Parameters.AddWithValue($"@{key}", value);

        var result = new List<Dictionary<string, object?>>();
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

        while (await reader.ReadAsync().ConfigureAwait(false)) {
            var row = new Dictionary<string, object?>();

            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

            result.Add(row);
        }

        return result;
    }

    public async Task<Dictionary<string, object?>?> SingleAsync(
        string sql,
        Dictionary<string, object>? parameters = null) {
        List<Dictionary<string, object?>> data = await QueryAsync(sql, parameters, limit: 1).ConfigureAwait(false);
        return data.FirstOrDefault();
    }

    public async Task<int> CountAsync(string tableName) {
        List<Dictionary<string, object?>> result =
            await QueryAsync($"SELECT COUNT(*) AS cnt FROM {tableName}").ConfigureAwait(false);
        return Convert.ToInt32(result[0]["cnt"]);
    }

    #endregion

    #region 表结构操作

    public async Task CreateTriggerAsync(string triggerName, string action) {
        if (string.IsNullOrWhiteSpace(triggerName))
            throw new ArgumentException("触发器名不能为空。", nameof(triggerName));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("操作不能为空。", nameof(action));
        await using SqliteCommand cmd = NewCommand();
        cmd.CommandText = $"CREATE TRIGGER IF NOT EXISTS {EscapeIdentifier(triggerName)} {action};";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     创建表（如果不存在）。
    /// </summary>
    public async Task CreateTableAsync(string tableName, string columnsDefinition) {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        if (string.IsNullOrWhiteSpace(columnsDefinition))
            throw new ArgumentException("列定义不能为空。", nameof(columnsDefinition));

        await using SqliteCommand cmd = NewCommand();
        cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {EscapeIdentifier(tableName)} ({columnsDefinition});";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     删除表（如果存在）。
    /// </summary>
    public async Task DropTableAsync(string tableName) {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        await using SqliteCommand cmd = NewCommand();
        cmd.CommandText = $"DROP TABLE IF EXISTS {EscapeIdentifier(tableName)};";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    #endregion


    #region 数据操作拓展-Insert

    /// <summary>
    ///     插入多条数据。不写入。
    /// </summary>
    public void InsertManyNonExecute(
        SqliteTransaction? transaction,
        List<SqliteCommand> commands,
        string tableName,
        in IEnumerable<Dictionary<string, object?>> dataArray,
        InsertExist onInsertExist) {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        commands.AddRange(dataArray.Select(data => InsertNonExecute(transaction, tableName, data, onInsertExist)));

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (commands.Count == 0)
            throw new ArgumentException("插入数据不能为空。", nameof(dataArray));
    }

    /// <summary>
    ///     插入多条数据。不写入。
    /// </summary>
    public List<SqliteCommand> InsertManyNonExecute(
        SqliteTransaction? transaction,
        string tableName,
        in IEnumerable<Dictionary<string, object?>> dataArray,
        InsertExist onInsertExist) {
        List<SqliteCommand> commands = [];
        InsertManyNonExecute(transaction, commands, tableName, dataArray, onInsertExist);
        return commands;
    }

    /// <summary>
    ///     插入一条数据。不写入。
    /// </summary>
    public SqliteCommand InsertNonExecute(
        SqliteTransaction? transaction,
        string tableName,
        Dictionary<string, object?> data,
        InsertExist onInsertExist) {
        string action = onInsertExist switch {
            InsertExist.FAIL    => "INSERT",
            InsertExist.IGNORE  => "INSERT OR IGNORE",
            InsertExist.REPLACE => "INSERT OR REPLACE",
            _                   => throw new ArgumentOutOfRangeException(nameof(onInsertExist), onInsertExist, null)
        };
        SqliteCommand cmd = NewCommand();
        cmd.Transaction = transaction;
        string columns = string.Join(", ", data.Keys.Select(EscapeIdentifier));
        string paramNames = string.Join(", ", data.Keys.Select(key => $"@{key}"));
        foreach ((string key, object? value) in data)
            cmd.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);

        cmd.CommandText = $"{action} INTO {EscapeIdentifier(tableName)} ({columns}) VALUES ({paramNames});";
        return cmd;
    }

    /// <summary>
    ///     插入一条数据并立即写入。
    /// </summary>
    public async Task InsertAsync(string tableName, Dictionary<string, object?> data, InsertExist onInsertExist) {
        await ExecuteAsync(InsertNonExecute(null, tableName, data, onInsertExist)).ConfigureAwait(false);
    }

    /// <summary>
    ///     插入多条数据并立即写入。
    /// </summary>
    public async Task InsertManyAsync(
        SqliteTransaction? transaction,
        string tableName,
        IEnumerable<Dictionary<string, object?>> dataArray,
        InsertExist onInsertExist) {
        await ExecuteAsync(InsertManyNonExecute(transaction, tableName, dataArray, onInsertExist))
            .ConfigureAwait(false);
    }

    #endregion

    #region 数据操作拓展-Update

    /// <summary>
    ///     更新多条数据。不写入。
    /// </summary>
    public void UpdateManyNonExecute(
        SqliteTransaction? transaction,
        List<SqliteCommand> commands,
        string tableName,
        in IEnumerable<Dictionary<string, object?>> dataArray,
        string whereClause,
        in IEnumerable<Dictionary<string, object?>> whereParamsArray) {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));
        commands.AddRange(
            dataArray.Zip(whereParamsArray)
                     .Select(item => UpdateNonExecute(transaction, tableName, item.First, whereClause, item.Second)));
    }

    /// <summary>
    ///     更新多条数据。不写入。
    /// </summary>
    public List<SqliteCommand> UpdateManyNonExecute(
        SqliteTransaction? transaction,
        string tableName,
        in IEnumerable<Dictionary<string, object?>> dataArray,
        string whereClause,
        in IEnumerable<Dictionary<string, object?>> whereParamsArray) {
        List<SqliteCommand> commands = [];
        UpdateManyNonExecute(transaction, commands, tableName, dataArray, whereClause, whereParamsArray);
        return commands;
    }

    /// <summary>
    ///     更新一条数据。不写入。
    /// </summary>
    public SqliteCommand UpdateNonExecute(
        SqliteTransaction? transaction,
        string tableName,
        Dictionary<string, object?> data,
        string whereClause,
        Dictionary<string, object?> whereParams) {
        if (string.IsNullOrWhiteSpace(whereClause))
            throw new ArgumentException("WHERE 条件不能为空。", nameof(whereClause));
        SqliteCommand cmd = NewCommand();
        cmd.Transaction = transaction;
        string setClause = string.Join(", ", data.Keys.Select(key => $"{EscapeIdentifier(key)} = @{key}"));

        foreach ((string key, object? value) in data)
            cmd.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);

        foreach ((string key, object? value) in whereParams)
            cmd.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);

        cmd.CommandText = $"UPDATE {EscapeIdentifier(tableName)} SET {setClause} WHERE {whereClause};";
        return cmd;
    }

    /// <summary>
    ///     更新一条数据并立即写入。
    /// </summary>
    public async Task UpdateAsync(
        SqliteTransaction? transaction,
        string tableName,
        Dictionary<string, object?> data,
        string whereClause,
        Dictionary<string, object?> whereParams) {
        await ExecuteAsync(UpdateNonExecute(transaction, tableName, data, whereClause, whereParams))
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     更新多条数据并立即写入。
    /// </summary>
    public async Task UpdateManyAsync(
        SqliteTransaction? transaction,
        string tableName,
        IEnumerable<Dictionary<string, object?>> dataArray,
        string whereClause,
        IEnumerable<Dictionary<string, object?>> whereParamsArray) {
        await ExecuteAsync(UpdateManyNonExecute(transaction, tableName, dataArray, whereClause, whereParamsArray))
            .ConfigureAwait(false);
    }

    #endregion

    #region 数据操作拓展-Delete

    /// <summary>
    ///     删除多条数据。不写入。
    /// </summary>
    public void DeleteManyNonExecute(
        SqliteTransaction? transaction,
        List<SqliteCommand> commands,
        string tableName,
        string whereClause,
        IEnumerable<Dictionary<string, object>> whereParamsArray) {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空。", nameof(tableName));

        if (string.IsNullOrWhiteSpace(whereClause))
            throw new ArgumentException("WHERE 条件不能为空。", nameof(whereClause));

        commands.AddRange(
            whereParamsArray.Select(whereParams => DeleteNonExecute(transaction, tableName, whereClause, whereParams)));
    }

    /// <summary>
    ///     删除多条数据。不写入。
    /// </summary>
    public List<SqliteCommand> DeleteManyNonExecute(
        SqliteTransaction? transaction,
        string tableName,
        string whereClause,
        IEnumerable<Dictionary<string, object>> whereParamsArray) {
        List<SqliteCommand> commands = [];
        DeleteManyNonExecute(transaction, commands, tableName, whereClause, whereParamsArray);
        return commands;
    }

    /// <summary>
    ///     删除一条数据。不写入。
    /// </summary>
    public SqliteCommand DeleteNonExecute(
        SqliteTransaction? transaction,
        string tableName,
        string whereClause,
        Dictionary<string, object> whereParams) {
        SqliteCommand cmd = NewCommand();
        cmd.Transaction = transaction;
        foreach ((string key, object value) in whereParams)
            cmd.Parameters.AddWithValue($"@{key}", value);

        cmd.CommandText = $"DELETE FROM {EscapeIdentifier(tableName)} WHERE {whereClause};";
        return cmd;
    }

    /// <summary>
    ///     删除一条数据并立即写入。
    /// </summary>
    public async Task DeleteAsync(
        SqliteTransaction? transaction,
        string tableName,
        string whereClause,
        Dictionary<string, object> whereParams) {
        await ExecuteAsync(DeleteNonExecute(transaction, tableName, whereClause, whereParams)).ConfigureAwait(false);
    }

    /// <summary>
    ///     删除多条数据并立即写入。
    /// </summary>
    public async Task DeleteManyAsync(
        SqliteTransaction? transaction,
        string tableName,
        string whereClause,
        IEnumerable<Dictionary<string, object>> whereParamsArray) {
        await ExecuteAsync(DeleteManyNonExecute(transaction, tableName, whereClause, whereParamsArray))
            .ConfigureAwait(false);
    }

    #endregion

    #region 事务支持

    /// <summary>
    ///     开始事务
    /// </summary>
    public SqliteTransaction BeginTransaction() { return _connection.BeginTransaction(); }


    /// <summary>
    ///     执行 SQL 命令
    /// </summary>
    public static async Task ExecuteAsync(params List<SqliteCommand> commands) {
        await using SqliteTransaction? transaction = commands[0].Transaction;
        try {
            foreach (SqliteCommand command in commands)
                await using (command) {
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
        } catch (SqliteException ex) {
            await LoggerService.ErrorAsync("执行数据库命令时发生错误", ex).ConfigureAwait(false);
            if (transaction is not null) {
                await LoggerService.InfoAsync("正在进行事务回滚...").ConfigureAwait(false);
                await transaction.RollbackAsync().ConfigureAwait(false);
                await LoggerService.InfoAsync("事务回滚已完成").ConfigureAwait(false);
            }

            return;
        }

        if (transaction is not null)
            await transaction.CommitAsync().ConfigureAwait(false);
        // Transaction的 Dispose函数没有实际作用，可忽略。
    }

    #endregion
}