using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Utilities;
using Log = QwQ_Music.Services.LoggerService;

namespace QwQ_Music.Services.ConfigIO;

/// <summary>
/// 数据库服务，提供数据库操作的方法
/// </summary>
public static class DataBaseService
{
    #region 配置项

    private static readonly DataBaseConfig _dataBaseConfig = ConfigManager.DataBaseConfig;

    /// <summary>
    /// 是否启用详细日志记录
    /// </summary>
    public static bool EnableVerboseLogging => _dataBaseConfig.EnableVerboseLogging;

    /// <summary>
    /// 是否启用性能监控
    /// </summary>
    public static bool EnablePerformanceMonitoring => _dataBaseConfig.EnablePerformanceMonitoring;

    /// <summary>
    /// 慢查询阈值（毫秒）
    /// </summary>
    public static int SlowQueryThreshold => _dataBaseConfig.SlowQueryThreshold;

    /// <summary>
    /// 命令超时时间（秒）
    /// </summary>
    public static int CommandTimeout => _dataBaseConfig.CommandTimeout;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public static int MaxRetryCount => _dataBaseConfig.MaxRetryCount;

    /// <summary>
    /// 重试延迟（毫秒）
    /// </summary>
    public static int RetryDelay => _dataBaseConfig.RetryDelay;

    /// <summary>
    /// 连接池大小
    /// </summary>
    public static int MaxConnectionPoolSize => _dataBaseConfig.MaxConnectionPoolSize;

    #endregion

    #region 数据库连接管理

    private static SqliteConnection? database;
    private static readonly SemaphoreSlim _connectionLock = new(1, 1);
    private static readonly SemaphoreSlim _poolLock = new(MaxConnectionPoolSize, MaxConnectionPoolSize);
    private static int openConnectionCount;
    private static DateTime lastConnectionTime = DateTime.MinValue;
    private static bool isDisposed;

    /// <summary>
    /// 获取数据库连接实例
    /// </summary>
    private static SqliteConnection Database
    {
        get
        {
            ObjectDisposedException.ThrowIf(isDisposed, nameof(DataBaseService));

            if (database != null && database.State != ConnectionState.Broken)
            {
                return database;
            }

            Log.Debug($"初始化数据库连接: {MainConfig.DatabaseSavePath}");

            // 确保数据库文件所在目录存在
            PathEnsurer.EnsureFileAndDirectoryExist(MainConfig.DatabaseSavePath);

            database?.Dispose();
            database = new SqliteConnection(
                $"data source={MainConfig.DatabaseSavePath};Foreign Keys=True;Cache=Shared;"
            );
            lastConnectionTime = DateTime.Now;
            return database;
        }
    }

    /// <summary>
    /// 获取数据库命令实例
    /// </summary>
    private static SqliteCommand CreateCommand()
    {
        var connection = Database;

        if (connection.State is ConnectionState.Connecting or ConnectionState.Closed)
        {
            Log.Debug($"打开数据库连接 (当前状态: {connection.State})");
            connection.Open();
            Interlocked.Increment(ref openConnectionCount);
        }

        var cmd = connection.CreateCommand();
        cmd.CommandTimeout = CommandTimeout;
        return cmd;
    }

    /// <summary>
    /// 关闭数据库连接
    /// </summary>
    public static async Task CloseConnectionAsync()
    {
        if (isDisposed)
            return;

        await _connectionLock.WaitAsync();
        try
        {
            if (database != null && database.State != ConnectionState.Closed)
            {
                await Log.DebugAsync($"关闭数据库连接 (打开次数: {openConnectionCount})");
                await database.CloseAsync();
                database.Dispose();
                database = null;
                openConnectionCount = 0;
            }
        }
        catch (Exception ex)
        {
            await Log.ErrorAsync($"关闭数据库连接时发生错误: {ex.Message}");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 关闭数据库连接（同步版本）
    /// </summary>
    public static void CloseConnection()
    {
        if (isDisposed)
            return;

        _connectionLock.Wait();
        try
        {
            if (database == null || database.State == ConnectionState.Closed)
                return;

            Log.Debug($"关闭数据库连接 (打开次数: {openConnectionCount})");
            database.Close();
            database.Dispose();
            database = null;
            openConnectionCount = 0;
        }
        catch (Exception ex)
        {
            Log.Error($"关闭数据库连接时发生错误: {ex.Message}");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// 获取数据库连接状态信息
    /// </summary>
    public static string GetConnectionStatus()
    {
        if (isDisposed)
        {
            return "已释放";
        }

        return database == null
            ? "未初始化"
            : $"状态: {database.State}, 打开次数: {openConnectionCount}, 最后连接时间: {lastConnectionTime:yyyy-MM-dd HH:mm:ss}";
    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public static async Task DisposeAsync()
    {
        if (isDisposed)
            return;

        isDisposed = true;
        await CloseConnectionAsync();
        _connectionLock.Dispose();
        _poolLock.Dispose();

        await Log.InfoAsync("数据库已经退出~");
    }

    /// <summary>
    /// 释放所有资源（同步版本）
    /// </summary>
    public static void Dispose()
    {
        if (isDisposed)
            return;

        isDisposed = true;
        CloseConnection();
        _connectionLock.Dispose();
        _poolLock.Dispose();

        Log.Info("数据库已经退出~");
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
        MUSICLISTS,
        LISTINFO,
    }

    /// <summary>
    /// 数据库操作结果
    /// </summary>
    public enum OperationResult
    {
        /// <summary>
        /// 操作成功
        /// </summary>
        Success,

        /// <summary>
        /// 操作失败
        /// </summary>
        Failure,

        /// <summary>
        /// 操作项不存在
        /// </summary>
        NotFound,
    }

    #endregion

    #region 表结构管理

    private static readonly SemaphoreSlim _tableCreationLock = new(1, 1);
    private static volatile bool _tablesInitialized;

    /// <summary>
    /// 确保表结构存在（异步版本）
    /// </summary>
    private static async Task EnsureTableExistsAsync()
    {
        if (_tablesInitialized)
            return;

        await _tableCreationLock.WaitAsync();
        try
        {
            if (_tablesInitialized)
                return;

            // 确保数据库文件存在
            if (!File.Exists(MainConfig.DatabaseSavePath))
            {
                await Log.InfoAsync($"数据库文件不存在，将创建新数据库: {MainConfig.DatabaseSavePath}");
                PathEnsurer.EnsureFileAndDirectoryExist(MainConfig.DatabaseSavePath);
                await CloseConnectionAsync();
            }

            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

            try
            {
                await CreateTablesAsync();
                _tablesInitialized = true;

                if (EnableVerboseLogging)
                {
                    stopwatch?.Stop();
                    await Log.InfoAsync(
                        $"数据库表结构创建完成 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                }
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"创建数据库表结构失败: {ex.Message}");
                await Log.DebugAsync($"异常详情: {ex}");
                throw;
            }
        }
        finally
        {
            _tableCreationLock.Release();
        }
    }

    /// <summary>
    /// 确保表结构存在（同步版本）
    /// </summary>
    private static void EnsureTableExists()
    {
        if (_tablesInitialized)
            return;

        _tableCreationLock.Wait();
        try
        {
            if (_tablesInitialized)
                return;

            // 确保数据库文件存在
            if (!File.Exists(MainConfig.DatabaseSavePath))
            {
                Log.Info($"数据库文件不存在，将创建新数据库: {MainConfig.DatabaseSavePath}");
                PathEnsurer.EnsureFileAndDirectoryExist(MainConfig.DatabaseSavePath);
                CloseConnection();
            }

            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

            try
            {
                CreateTables();
                _tablesInitialized = true;

                if (EnableVerboseLogging)
                {
                    stopwatch?.Stop();
                    Log.Info(
                        $"数据库表结构创建完成 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                }
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                Log.Error($"创建数据库表结构失败: {ex.Message}");
                Log.Debug($"异常详情: {ex}");
                throw;
            }
        }
        finally
        {
            _tableCreationLock.Release();
        }
    }

    /// <summary>
    /// 创建数据库表（异步版本）
    /// </summary>
    private static async Task CreateTablesAsync()
    {
        await using var music = CreateCommand();
        await using var musicLists = CreateCommand();
        await using var musicListInfo = CreateCommand();

        if (EnableVerboseLogging)
        {
            await Log.DebugAsync("开始创建数据库表结构");
        }

        music.CommandText = $"""
            CREATE TABLE IF NOT EXISTS MUSICS(
            {nameof(MusicItemModel.Title)} TEXT NOT NULL,
            {nameof(MusicItemModel.Artists)} TEXT,
            {nameof(MusicItemModel.Composer)} TEXT,
            {nameof(MusicItemModel.Album)} TEXT,
            {nameof(MusicItemModel.CoverFileName)} TEXT,
            {nameof(MusicItemModel.FilePath)} TEXT NOT NULL UNIQUE PRIMARY KEY,
            {nameof(MusicItemModel.FileSize)} TEXT NOT NULL,
            {nameof(MusicItemModel.Current)} BLOB,
            {nameof(MusicItemModel.Duration)} BLOB NOT NULL,
            {nameof(MusicItemModel.CoverColors)} TEXT,
            {nameof(MusicItemModel.Gain)} INTEGER,
            {nameof(MusicItemModel.EncodingFormat)} TEXT NOT NULL,
            {nameof(MusicItemModel.Comment)} TEXT,
            {nameof(MusicItemModel.Remarks)} TEXT,
            {nameof(MusicItemModel.LyricOffset)} INTEGER)
            """;

        musicLists.CommandText = $"""
            CREATE TABLE IF NOT EXISTS MUSICLISTS(
            {nameof(MusicListModel.Name)} TEXT NOT NULL,
            {nameof(MusicItemModel.FilePath)} TEXT NOT NULL,
            FOREIGN KEY({nameof(MusicItemModel.FilePath)}) REFERENCES MUSICS({nameof(MusicItemModel.FilePath)}))
            """;

        musicListInfo.CommandText = $"""
            CREATE TABLE IF NOT EXISTS LISTINFO(
            {nameof(MusicListModel.Name)} TEXT NOT NULL UNIQUE PRIMARY KEY,
            {nameof(MusicListModel.Description)} TEXT,
            {nameof(MusicListModel.CoverPath)} TEXT,
            {nameof(MusicListModel.Count)} INTEGER NOT NULL DEFAULT 0,
            {nameof(MusicListModel.LatestPlayedMusic)} TEXT)
            """;

        await music.ExecuteNonQueryAsync();
        await musicLists.ExecuteNonQueryAsync();
        await musicListInfo.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 创建数据库表（同步版本）
    /// </summary>
    private static void CreateTables()
    {
        using var music = CreateCommand();
        using var musicLists = CreateCommand();
        using var musicListInfo = CreateCommand();

        if (EnableVerboseLogging)
        {
            Log.Debug("开始创建数据库表结构");
        }

        music.CommandText = $"""
            CREATE TABLE IF NOT EXISTS MUSICS(
            {nameof(MusicItemModel.Title)} TEXT NOT NULL PRIMARY KEY,
            {nameof(MusicItemModel.Artists)} TEXT,
            {nameof(MusicItemModel.Composer)} TEXT,
            {nameof(MusicItemModel.Album)} TEXT,
            {nameof(MusicItemModel.CoverFileName)} TEXT,
            {nameof(MusicItemModel.FilePath)} TEXT NOT NULL UNIQUE,
            {nameof(MusicItemModel.FileSize)} TEXT NOT NULL,
            {nameof(MusicItemModel.Current)} BLOB,
            {nameof(MusicItemModel.Duration)} BLOB NOT NULL,
            {nameof(MusicItemModel.CoverColors)} TEXT,
            {nameof(MusicItemModel.Gain)} INTEGER,
            {nameof(MusicItemModel.EncodingFormat)} TEXT NOT NULL,
            {nameof(MusicItemModel.Comment)} TEXT,
            {nameof(MusicItemModel.Remarks)} TEXT,
            {nameof(MusicItemModel.LyricOffset)} INTEGER)
            """;

        musicLists.CommandText = $"""
            CREATE TABLE IF NOT EXISTS MUSICLISTS(
            {nameof(MusicListModel.Name)} TEXT NOT NULL,
            {nameof(MusicItemModel.FilePath)} TEXT NOT NULL,
            FOREIGN KEY({nameof(MusicItemModel.FilePath)}) REFERENCES MUSICS({nameof(MusicItemModel.FilePath)}))
            """;

        musicListInfo.CommandText = $"""
            CREATE TABLE IF NOT EXISTS LISTINFO(
            {nameof(MusicListModel.Name)} TEXT NOT NULL UNIQUE PRIMARY KEY,
            {nameof(MusicListModel.Description)} TEXT,
            {nameof(MusicListModel.CoverPath)} TEXT,
            {nameof(MusicListModel.Count)} INTEGER NOT NULL DEFAULT 0,
            {nameof(MusicListModel.LatestPlayedMusic)} TEXT)
            """;

        music.ExecuteNonQuery();
        musicLists.ExecuteNonQuery();
        musicListInfo.ExecuteNonQuery();
    }

    #endregion

    #region 事务支持

    /// <summary>
    /// 在事务中执行多个操作
    /// </summary>
    public static async Task<OperationResult> ExecuteInTransactionAsync(Func<Task> operations)
    {
        await EnsureTableExistsAsync();

        await _poolLock.WaitAsync();
        try
        {
            var connection = Database;
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            await using var transaction = await connection.BeginTransactionAsync();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

            try
            {
                await Log.DebugAsync("开始数据库事务");
                await operations();
                await transaction.CommitAsync();

                stopwatch?.Stop();
                await Log.InfoAsync(
                    $"数据库事务提交成功 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                );
                return OperationResult.Success;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"数据库事务执行失败，将回滚: {ex.Message}");
                await Log.DebugAsync($"事务异常详情: {ex}");

                try
                {
                    await transaction.RollbackAsync();
                    await Log.InfoAsync("数据库事务已回滚");
                }
                catch (Exception rollbackEx)
                {
                    await Log.ErrorAsync($"事务回滚失败: {rollbackEx.Message}");
                }

                return OperationResult.Failure;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 在事务中执行多个操作（同步版本）
    /// </summary>
    public static OperationResult ExecuteInTransaction(Action operations)
    {
        EnsureTableExists();

        _poolLock.Wait();
        try
        {
            var connection = Database;
            if (connection.State != ConnectionState.Open)
                connection.Open();

            using var transaction = connection.BeginTransaction();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

            try
            {
                Log.Debug("开始数据库事务（同步）");
                operations();
                transaction.Commit();

                stopwatch?.Stop();
                Log.Info(
                    $"数据库事务提交成功（同步）{(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                );
                return OperationResult.Success;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                Log.Error($"数据库事务执行失败，将回滚（同步）: {ex.Message}");
                Log.Debug($"事务异常详情: {ex}");

                try
                {
                    transaction.Rollback();
                    Log.Info("数据库事务已回滚（同步）");
                }
                catch (Exception rollbackEx)
                {
                    Log.Error($"事务回滚失败（同步）: {rollbackEx.Message}");
                }

                return OperationResult.Failure;
            }
        }
        finally
        {
            _poolLock.Release();
        }
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
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行查询");
            yield break;
        }

        await EnsureTableExistsAsync();

        await _poolLock.WaitAsync();
        try
        {
            await using var command = CreateCommand();
            SqliteDataReader? reader;
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                sqlCommand = BuildQueryCommand(table, limit, search, sortBy, sort, table2);
                command.CommandText = sqlCommand;

                if (EnableVerboseLogging)
                {
                    await Log.DebugAsync($"执行查询: {sqlCommand}");
                }

                reader = await command.ExecuteReaderAsync();

                if (!reader.HasRows && EnableVerboseLogging)
                {
                    await Log.DebugAsync("查询结果为空");
                }
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"加载数据失败: {ex.Message}");
                await Log.DebugAsync($"SQL: {sqlCommand}");
                await Log.DebugAsync($"异常详情: {ex}");
                yield break;
            }

            if (!reader.HasRows)
            {
                await reader.DisposeAsync();
                yield break;
            }

            int rowCount = 0;
            try
            {
                while (await reader.ReadAsync())
                {
                    rowCount++;
                    yield return ReadRowToDictionary(reader);
                }
            }
            finally
            {
                await reader.DisposeAsync();
            }

            stopwatch?.Stop();
            if (EnableVerboseLogging || stopwatch != null && stopwatch.ElapsedMilliseconds > SlowQueryThreshold)
            {
                await Log.InfoAsync(
                    $"查询完成: 返回 {rowCount} 行数据 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                );

                if (stopwatch != null && stopwatch.ElapsedMilliseconds > SlowQueryThreshold)
                {
                    await Log.WarningAsync($"检测到慢查询: {sqlCommand}");
                }
            }
        }
        finally
        {
            _poolLock.Release();
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
        if (isDisposed)
        {
            Log.Error("数据库服务已释放，无法执行查询");
            yield break;
        }

        EnsureTableExists();

        _poolLock.Wait();
        try
        {
            using var command = CreateCommand();
            SqliteDataReader? reader;
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                sqlCommand = BuildQueryCommand(table, limit, search, sortBy, sort, table2);
                command.CommandText = sqlCommand;

                if (EnableVerboseLogging)
                {
                    Log.Debug($"执行查询（同步）: {sqlCommand}");
                }

                reader = command.ExecuteReader();

                if (!reader.HasRows && EnableVerboseLogging)
                {
                    Log.Debug("查询结果为空（同步）");
                }
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                Log.Error($"加载数据失败（同步）: {ex.Message}");
                Log.Debug($"SQL: {sqlCommand}");
                Log.Debug($"异常详情: {ex}");
                yield break;
            }

            if (!reader.HasRows)
            {
                reader.Dispose();
                yield break;
            }

            int rowCount = 0;
            try
            {
                while (reader.Read())
                {
                    rowCount++;
                    yield return ReadRowToDictionary(reader);
                }
            }
            finally
            {
                reader.Dispose();
            }

            stopwatch?.Stop();
            if (EnableVerboseLogging || stopwatch != null && stopwatch.ElapsedMilliseconds > SlowQueryThreshold)
            {
                Log.Info(
                    $"查询完成（同步）: 返回 {rowCount} 行数据 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                );

                if (stopwatch != null && stopwatch.ElapsedMilliseconds > SlowQueryThreshold)
                {
                    Log.Warning($"检测到慢查询（同步）: {sqlCommand}");
                }
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 构建查询命令
    /// </summary>
    private static string BuildQueryCommand(
        Table table,
        Range limit,
        string? search,
        string? sortBy,
        Sort sort,
        Table? table2
    )
    {
        string sqlCommand = $"SELECT * FROM {table:G}";

        if (table2 is not null)
        {
            sqlCommand +=
                $" JOIN {table2:G} ON {table:G}.{nameof(MusicItemModel.FilePath)} = {table2:G}.{nameof(MusicItemModel.FilePath)} ";
        }

        if (!string.IsNullOrEmpty(search))
        {
            // 检查搜索条件是否已包含 WHERE 关键字
            if (!search.Trim().StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                sqlCommand += " WHERE ";
            }
            sqlCommand += search + " ";
        }

        if (!string.IsNullOrEmpty(sortBy))
        {
            sqlCommand += $" ORDER BY {sortBy} {sort:G} ";
        }

        // 优化limit处理：如果limit为Range.All（..），则不拼接LIMIT语句

        if (!limit.Equals(default))
        {
            sqlCommand += $" LIMIT {limit.Start},{limit.End} ";
        }

        return sqlCommand;
    }

    /// <summary>
    /// 从数据库中获取特定字段的值
    /// </summary>
    public static async Task<List<TResult>?> LoadSpecifyFieldsAsync<TResult>(
        Table table,
        string[] ordinals,
        Func<Dictionary<string, object>, TResult> converter,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC
    )
    {
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行查询");
            return null;
        }

        if (ordinals.Length == 0)
        {
            await Log.ErrorAsync("字段列表为空，无法执行查询");
            return null;
        }

        await EnsureTableExistsAsync();

        await _poolLock.WaitAsync();
        try
        {
            await using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                sqlCommand = BuildFieldQueryCommand(table, ordinals, limit, search, sortBy, sort);
                command.CommandText = sqlCommand;

                if (EnableVerboseLogging)
                {
                    await Log.DebugAsync($"执行字段查询: {sqlCommand}");
                }

                await using var reader = await command.ExecuteReaderAsync();
                var result = new List<TResult>();

                while (await reader.ReadAsync())
                {
                    var rowDict = ReadRowToDictionary(reader);
                    result.Add(converter(rowDict));
                }

                stopwatch?.Stop();
                if (!EnableVerboseLogging && (stopwatch == null || stopwatch.ElapsedMilliseconds <= SlowQueryThreshold))
                    return result;

                await Log.InfoAsync(
                    $"字段查询完成: 返回 {result.Count} 行数据 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                );

                if (stopwatch != null && stopwatch.ElapsedMilliseconds > SlowQueryThreshold)
                {
                    await Log.WarningAsync($"检测到慢查询: {sqlCommand}");
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"加载特定字段失败: {ex.Message}");
                await Log.DebugAsync($"SQL: {sqlCommand}");
                await Log.DebugAsync($"异常详情: {ex}");
                return null;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 从数据库中获取特定字段的值（同步版本）
    /// </summary>
    public static List<TResult>? LoadSpecifyFields<TResult>(
        Table table,
        string[] ordinals,
        Func<Dictionary<string, object>, TResult> converter,
        Range limit = default,
        string? search = null,
        string? sortBy = null,
        Sort sort = Sort.ASC
    )
    {
        if (isDisposed)
        {
            Log.Error("数据库服务已释放，无法执行查询");
            return null;
        }

        if (ordinals.Length == 0)
        {
            Log.Error("字段列表为空，无法执行查询");
            return null;
        }

        EnsureTableExists();

        _poolLock.Wait();
        try
        {
            using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                sqlCommand = BuildFieldQueryCommand(table, ordinals, limit, search, sortBy, sort);
                command.CommandText = sqlCommand;

                if (EnableVerboseLogging)
                {
                    Log.Debug($"执行字段查询（同步）: {sqlCommand}");
                }

                using var reader = command.ExecuteReader();
                var result = new List<TResult>();

                while (reader.Read())
                {
                    var rowDict = ReadRowToDictionary(reader);
                    result.Add(converter(rowDict));
                }

                stopwatch?.Stop();
                if (!EnableVerboseLogging && (stopwatch == null || stopwatch.ElapsedMilliseconds <= SlowQueryThreshold))
                    return result;

                Log.Info(
                    $"字段查询完成（同步）: 返回 {result.Count} 行数据 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                );

                if (stopwatch != null && stopwatch.ElapsedMilliseconds > SlowQueryThreshold)
                {
                    Log.Warning($"检测到慢查询（同步）: {sqlCommand}");
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                Log.Error($"加载特定字段失败（同步）: {ex.Message}");
                Log.Debug($"SQL: {sqlCommand}");
                Log.Debug($"异常详情: {ex}");
                return null;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 构建字段查询命令
    /// </summary>
    private static string BuildFieldQueryCommand(
        Table table,
        string[] ordinals,
        Range limit,
        string? search,
        string? sortBy,
        Sort sort
    )
    {
        string sqlCommand = $"SELECT {string.Join(',', ordinals)} FROM {table:G} ";

        if (!string.IsNullOrEmpty(search))
        {
            // 检查搜索条件是否已包含 WHERE 关键字
            if (!search.Trim().StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                sqlCommand += " WHERE ";
            }
            sqlCommand += search + " ";
        }

        if (!string.IsNullOrEmpty(sortBy))
        {
            sqlCommand += $" ORDER BY {sortBy} {sort:G} ";
        }

        // 优化limit处理：如果limit为Range.All（..），则不拼接LIMIT语句
        if (!limit.Equals(default))
        {
            sqlCommand += $" LIMIT {limit.Start},{limit.End} ";
        }

        return sqlCommand;
    }

    /// <summary>
    /// 检查记录是否存在
    /// </summary>
    public static async Task<bool> RecordExistsAsync(Table table, string columnName, string? value)
    {
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行查询");
            return false;
        }

        if (string.IsNullOrEmpty(columnName))
        {
            await Log.ErrorAsync("列名为空，无法执行查询");
            return false;
        }

        await EnsureTableExistsAsync();

        await _poolLock.WaitAsync();
        try
        {
            await using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                sqlCommand = $"SELECT COUNT(*) FROM {table:G} WHERE {columnName} = @value";
                command.CommandText = sqlCommand;
                command.Parameters.AddWithValue("@value", value as object ?? DBNull.Value);

                if (EnableVerboseLogging)
                {
                    await Log.DebugAsync($"检查记录是否存在: {sqlCommand} [参数: {value}]");
                }

                object? result = await command.ExecuteScalarAsync();
                bool exists = Convert.ToInt32(result) > 0;

                stopwatch?.Stop();
                if (EnableVerboseLogging)
                {
                    await Log.DebugAsync(
                        $"记录{(exists ? "存在" : "不存在")} {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                }

                return exists;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"检查记录是否存在失败: {ex.Message}");
                await Log.DebugAsync($"SQL: {sqlCommand} [参数: {value}]");
                await Log.DebugAsync($"异常详情: {ex}");
                return false;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 检查记录是否存在（同步版本）
    /// </summary>
    public static bool RecordExists(Table table, string columnName, string value)
    {
        if (isDisposed)
        {
            Log.Error("数据库服务已释放，无法执行查询");
            return false;
        }

        if (string.IsNullOrEmpty(columnName))
        {
            Log.Error("列名为空，无法执行查询");
            return false;
        }

        EnsureTableExists();

        _poolLock.Wait();
        try
        {
            using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                sqlCommand = $"SELECT COUNT(*) FROM {table:G} WHERE {columnName} = @value";
                command.CommandText = sqlCommand;
                command.Parameters.AddWithValue("@value", value);

                if (EnableVerboseLogging)
                {
                    Log.Debug($"检查记录是否存在（同步）: {sqlCommand} [参数: {value}]");
                }

                object? result = command.ExecuteScalar();
                bool exists = Convert.ToInt32(result) > 0;

                stopwatch?.Stop();
                if (EnableVerboseLogging)
                {
                    Log.Debug(
                        $"记录{(exists ? "存在" : "不存在")}（同步）{(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                }

                return exists;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                Log.Error($"检查记录是否存在失败（同步）: {ex.Message}");
                Log.Debug($"SQL: {sqlCommand} [参数: {value}]");
                Log.Debug($"异常详情: {ex}");
                return false;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 获取记录数量
    /// </summary>
    public static async Task<int> GetRecordCountAsync(Table table, string? whereClause = null)
    {
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行查询");
            return 0;
        }

        await EnsureTableExistsAsync();

        await _poolLock.WaitAsync();
        try
        {
            await using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                sqlCommand = $"SELECT COUNT(*) FROM {table:G}";

                if (!string.IsNullOrEmpty(whereClause))
                {
                    if (!whereClause.Trim().StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
                    {
                        sqlCommand += " WHERE ";
                    }
                    sqlCommand += whereClause;
                }

                command.CommandText = sqlCommand;

                if (EnableVerboseLogging)
                {
                    await Log.DebugAsync($"获取记录数量: {sqlCommand}");
                }

                object? result = await command.ExecuteScalarAsync();
                int count = Convert.ToInt32(result);

                stopwatch?.Stop();
                if (EnableVerboseLogging)
                {
                    await Log.DebugAsync(
                        $"记录数量: {count} {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                }

                return count;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"获取记录数量失败: {ex.Message}");
                await Log.DebugAsync($"SQL: {sqlCommand}");
                await Log.DebugAsync($"异常详情: {ex}");
                return 0;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 获取记录数量（同步版本）
    /// </summary>
    public static int GetRecordCount(Table table, string? whereClause = null)
    {
        if (isDisposed)
        {
            Log.Error("数据库服务已释放，无法执行查询");
            return 0;
        }

        EnsureTableExists();

        _poolLock.Wait();
        try
        {
            using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                sqlCommand = $"SELECT COUNT(*) FROM {table:G}";

                if (!string.IsNullOrEmpty(whereClause))
                {
                    if (!whereClause.Trim().StartsWith("WHERE", StringComparison.OrdinalIgnoreCase))
                    {
                        sqlCommand += " WHERE ";
                    }
                    sqlCommand += whereClause;
                }

                command.CommandText = sqlCommand;

                if (EnableVerboseLogging)
                {
                    Log.Debug($"获取记录数量（同步）: {sqlCommand}");
                }

                object? result = command.ExecuteScalar();
                int count = Convert.ToInt32(result);

                stopwatch?.Stop();
                if (EnableVerboseLogging)
                {
                    Log.Debug(
                        $"记录数量（同步）: {count} {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                }

                return count;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                Log.Error($"获取记录数量失败（同步）: {ex.Message}");
                Log.Debug($"SQL: {sqlCommand}");
                Log.Debug($"异常详情: {ex}");
                return 0;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    #endregion

    #region 数据修改方法

    /// <summary>
    /// 异步更新数据到数据库。
    /// </summary>
    public static async Task<OperationResult> UpdateDataAsync(
        Dictionary<string, string?> data,
        Table table,
        string whereColumn,
        string? whereValue
    )
    {
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行更新");
            return OperationResult.Failure;
        }

        if (data.Count == 0)
        {
            await Log.WarningAsync("更新数据失败: 没有提供要更新的数据");
            return OperationResult.Failure;
        }

        if (string.IsNullOrEmpty(whereColumn))
        {
            await Log.ErrorAsync("更新条件列名为空");
            return OperationResult.Failure;
        }

        await EnsureTableExistsAsync();

        await _poolLock.WaitAsync();
        try
        {
            await using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                string setClause = string.Join(", ", data.Select(kv => $"{kv.Key} = @{kv.Key}"));
                sqlCommand = $"UPDATE {table:G} SET {setClause} WHERE {whereColumn} = @whereValue";
                command.CommandText = sqlCommand;

                // 添加参数
                foreach ((string key, string? value) in data)
                {
                    command.Parameters.AddWithValue($"@{key}", value as object ?? DBNull.Value);
                }
                command.Parameters.AddWithValue("@whereValue", whereValue as object ?? DBNull.Value);

                if (EnableVerboseLogging)
                {
                    await Log.DebugAsync($"执行更新: {sqlCommand}");
                    await Log.DebugAsync($"更新条件: {whereColumn} = {whereValue}");
                    await Log.DebugAsync($"更新数据: {string.Join(", ", data.Select(kv => $"{kv.Key} = {kv.Value}"))}");
                }

                int rowsAffected = await command.ExecuteNonQueryAsync();

                stopwatch?.Stop();
                if (rowsAffected > 0)
                {
                    await Log.InfoAsync(
                        $"更新成功: 影响了 {rowsAffected} 行数据 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                    return OperationResult.Success;
                }

                await Log.WarningAsync($"更新失败: 没有找到匹配的记录 (条件: {whereColumn} = {whereValue})");
                return OperationResult.NotFound;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"更新数据失败: {ex.Message}");
                await Log.DebugAsync($"SQL: {sqlCommand}");
                await Log.DebugAsync($"异常详情: {ex}");
                return OperationResult.Failure;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 同步更新数据到数据库。
    /// </summary>
    public static OperationResult UpdateData(
        Dictionary<string, string?> data,
        Table table,
        string whereColumn,
        string? whereValue
    )
    {
        if (isDisposed)
        {
            Log.Error("数据库服务已释放，无法执行更新");
            return OperationResult.Failure;
        }

        if (data.Count == 0)
        {
            Log.Warning("更新数据失败（同步）: 没有提供要更新的数据");
            return OperationResult.Failure;
        }

        if (string.IsNullOrEmpty(whereColumn))
        {
            Log.Error("更新条件列名为空");
            return OperationResult.Failure;
        }

        EnsureTableExists();

        _poolLock.Wait();
        try
        {
            using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                string setClause = string.Join(", ", data.Select(kv => $"{kv.Key} = @{kv.Key}"));
                sqlCommand = $"UPDATE {table:G} SET {setClause} WHERE {whereColumn} = @whereValue";
                command.CommandText = sqlCommand;

                // 添加参数
                foreach ((string key, string? value) in data)
                {
                    command.Parameters.AddWithValue($"@{key}", value as object ?? DBNull.Value);
                }
                command.Parameters.AddWithValue("@whereValue", whereValue as object ?? DBNull.Value);

                if (EnableVerboseLogging)
                {
                    Log.Debug($"执行更新（同步）: {sqlCommand}");
                    Log.Debug($"更新条件: {whereColumn} = {whereValue}");
                    Log.Debug($"更新数据: {string.Join(", ", data.Select(kv => $"{kv.Key} = {kv.Value}"))}");
                }

                int rowsAffected = command.ExecuteNonQuery();

                stopwatch?.Stop();
                if (rowsAffected > 0)
                {
                    Log.Info(
                        $"更新成功（同步）: 影响了 {rowsAffected} 行数据 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                    return OperationResult.Success;
                }

                Log.Warning($"更新失败（同步）: 没有找到匹配的记录 (条件: {whereColumn} = {whereValue})");
                return OperationResult.NotFound;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                Log.Error($"更新数据失败（同步）: {ex.Message}");
                Log.Debug($"SQL: {sqlCommand}");
                Log.Debug($"异常详情: {ex}");
                return OperationResult.Failure;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 异步插入数据到数据库。
    /// </summary>
    public static async Task<OperationResult> InsertDataAsync(Dictionary<string, string?> data, Table table)
    {
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行插入");
            return OperationResult.Failure;
        }

        if (data.Count == 0)
        {
            await Log.WarningAsync("插入数据失败: 没有提供要插入的数据");
            return OperationResult.Failure;
        }

        await EnsureTableExistsAsync();

        await _poolLock.WaitAsync();
        try
        {
            await using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                string columns = string.Join(", ", data.Keys);
                string parameters = string.Join(", ", data.Keys.Select(k => $"@{k}"));

                sqlCommand = $"INSERT INTO {table:G} ({columns}) VALUES ({parameters})";
                command.CommandText = sqlCommand;

                // 添加参数
                foreach ((string key, string? value) in data)
                {
                    command.Parameters.AddWithValue($"@{key}", value as object ?? DBNull.Value);
                }

                if (EnableVerboseLogging)
                {
                    await Log.DebugAsync($"执行插入: {sqlCommand}");
                    await Log.DebugAsync($"插入数据: {string.Join(", ", data.Select(kv => $"{kv.Key} = {kv.Value}"))}");
                }

                int rowsAffected = await command.ExecuteNonQueryAsync();

                stopwatch?.Stop();
                if (rowsAffected > 0)
                {
                    await Log.InfoAsync(
                        $"插入成功: 影响了 {rowsAffected} 行数据 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                    return OperationResult.Success;
                }

                await Log.WarningAsync("插入失败: 没有行受影响");
                return OperationResult.NotFound;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
            {
                stopwatch?.Stop();
                await Log.ErrorAsync("插入数据失败: 违反唯一性约束或外键约束");
                await Log.DebugAsync($"SQL: {sqlCommand}");
                await Log.DebugAsync($"异常详情: {ex}");
                return OperationResult.Failure;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"插入数据失败: {ex.Message}");
                await Log.DebugAsync($"SQL: {sqlCommand}");
                await Log.DebugAsync($"异常详情: {ex}");
                return OperationResult.Failure;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 同步插入数据到数据库。
    /// </summary>
    public static OperationResult InsertData(Dictionary<string, string?> data, Table table)
    {
        if (isDisposed)
        {
            Log.Error("数据库服务已释放，无法执行插入");
            return OperationResult.Failure;
        }

        if (data.Count == 0)
        {
            Log.Warning("插入数据失败（同步）: 没有提供要插入的数据");
            return OperationResult.Failure;
        }

        EnsureTableExists();

        _poolLock.Wait();
        try
        {
            using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                string columns = string.Join(", ", data.Keys);
                string parameters = string.Join(", ", data.Keys.Select(k => $"@{k}"));

                sqlCommand = $"INSERT INTO {table:G} ({columns}) VALUES ({parameters})";
                command.CommandText = sqlCommand;

                // 添加参数
                foreach ((string key, string? value) in data)
                {
                    command.Parameters.AddWithValue($"@{key}", value as object ?? DBNull.Value);
                }

                if (EnableVerboseLogging)
                {
                    Log.Debug($"执行插入（同步）: {sqlCommand}");
                    Log.Debug($"插入数据: {string.Join(", ", data.Select(kv => $"{kv.Key} = {kv.Value}"))}");
                }

                int rowsAffected = command.ExecuteNonQuery();

                stopwatch?.Stop();
                if (rowsAffected > 0)
                {
                    Log.Info(
                        $"插入成功（同步）: 影响了 {rowsAffected} 行数据 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                    return OperationResult.Success;
                }

                Log.Warning("插入失败（同步）: 没有行受影响");
                return OperationResult.NotFound;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // SQLITE_CONSTRAINT
            {
                stopwatch?.Stop();
                Log.Error("插入数据失败（同步）: 违反唯一性约束或外键约束");
                Log.Debug($"SQL: {sqlCommand}");
                Log.Debug($"异常详情: {ex}");
                return OperationResult.Failure;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                Log.Error($"插入数据失败（同步）: {ex.Message}");
                Log.Debug($"SQL: {sqlCommand}");
                Log.Debug($"异常详情: {ex}");
                return OperationResult.Failure;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 异步删除数据库中的数据。
    /// </summary>
    public static async Task<OperationResult> DeleteDataAsync(
        Table table,
        string whereColumn,
        string? whereValue = null
    )
    {
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行删除");
            return OperationResult.Failure;
        }

        if (string.IsNullOrEmpty(whereColumn))
        {
            await Log.ErrorAsync("删除条件列名为空");
            return OperationResult.Failure;
        }

        await EnsureTableExistsAsync();

        await _poolLock.WaitAsync();
        try
        {
            await using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                sqlCommand = $"DELETE FROM {table:G} WHERE {whereColumn} = @whereValue";
                command.CommandText = sqlCommand;
                command.Parameters.AddWithValue("@whereValue", whereValue as object ?? DBNull.Value);

                if (EnableVerboseLogging)
                {
                    await Log.DebugAsync($"执行删除: {sqlCommand} [参数: {whereValue}]");
                }

                int rowsAffected = await command.ExecuteNonQueryAsync();

                stopwatch?.Stop();
                if (rowsAffected > 0)
                {
                    await Log.InfoAsync(
                        $"删除成功: 影响了 {rowsAffected} 行数据 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                    return OperationResult.Success;
                }

                await Log.WarningAsync($"删除失败: 没有找到匹配的记录 (条件: {whereColumn} = {whereValue})");
                return OperationResult.NotFound;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"删除数据失败: {ex.Message}");
                await Log.DebugAsync($"SQL: {sqlCommand} [参数: {whereValue}]");
                await Log.DebugAsync($"异常详情: {ex}");
                return OperationResult.Failure;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 同步删除数据库中的数据。
    /// </summary>
    public static OperationResult DeleteData(Table table, string whereColumn, string? whereValue)
    {
        if (isDisposed)
        {
            Log.Error("数据库服务已释放，无法执行删除");
            return OperationResult.Failure;
        }

        if (string.IsNullOrEmpty(whereColumn))
        {
            Log.Error("删除条件列名为空");
            return OperationResult.Failure;
        }

        EnsureTableExists();

        _poolLock.Wait();
        try
        {
            using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
            string sqlCommand = string.Empty;

            try
            {
                sqlCommand = $"DELETE FROM {table:G} WHERE {whereColumn} = @whereValue";
                command.CommandText = sqlCommand;
                command.Parameters.AddWithValue("@whereValue", whereValue as object ?? DBNull.Value);

                if (EnableVerboseLogging)
                {
                    Log.Debug($"执行删除（同步）: {sqlCommand} [参数: {whereValue}]");
                }

                int rowsAffected = command.ExecuteNonQuery();

                stopwatch?.Stop();
                if (rowsAffected > 0)
                {
                    Log.Info(
                        $"删除成功（同步）: 影响了 {rowsAffected} 行数据 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                    return OperationResult.Success;
                }

                Log.Warning($"删除失败（同步）: 没有找到匹配的记录 (条件: {whereColumn} = {whereValue})");
                return OperationResult.NotFound;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                Log.Error($"删除数据失败（同步）: {ex.Message}");
                Log.Debug($"SQL: {sqlCommand} [参数: {whereValue}]");
                Log.Debug($"异常详情: {ex}");
                return OperationResult.Failure;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 异步批量删除数据库中的数据。
    /// </summary>
    public static async Task<OperationResult> BatchDeleteDataAsync(
        Table table,
        string whereColumn,
        IEnumerable<string> whereValues
    )
    {
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行批量删除");
            return OperationResult.Failure;
        }

        if (string.IsNullOrEmpty(whereColumn))
        {
            await Log.ErrorAsync("批量删除条件列名为空");
            return OperationResult.Failure;
        }

        var valuesList = whereValues.ToList();
        if (valuesList.Count == 0)
        {
            await Log.WarningAsync("批量删除值列表为空，跳过操作");
            return OperationResult.Success;
        }

        await EnsureTableExistsAsync();
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
        int totalRowsAffected = 0;
        int successCount = 0;
        int failCount = 0;

        try
        {
            await Log.DebugAsync($"开始批量删除操作: 表 {table:G}, 条件列 {whereColumn}, 共 {valuesList.Count} 项");

            // 使用事务进行批量操作
            return await ExecuteInTransactionAsync(async () =>
            {
                int index = 0;
                foreach (string value in valuesList)
                {
                    await using var command = CreateCommand();
                    string sqlCommand = $"DELETE FROM {table:G} WHERE {whereColumn} = @whereValue";
                    command.CommandText = sqlCommand;
                    command.Parameters.AddWithValue("@whereValue", value);

                    if (EnableVerboseLogging)
                    {
                        await Log.DebugAsync($"执行批量删除 #{++index}: {sqlCommand} [参数: {value}]");
                    }

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    totalRowsAffected += rowsAffected;

                    if (rowsAffected > 0)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        await Log.WarningAsync(
                            $"批量删除项 #{index} 失败: 没有找到匹配的记录 (条件: {whereColumn} = {value})"
                        );
                    }
                }
            });
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            await Log.ErrorAsync($"批量删除数据失败: {ex.Message}");
            await Log.DebugAsync($"异常详情: {ex}");
            return OperationResult.Failure;
        }
        finally
        {
            stopwatch?.Stop();
            await Log.InfoAsync(
                $"批量删除操作完成: 成功 {successCount} 项, 失败 {failCount} 项, 共影响 {totalRowsAffected} 行数据 "
                    + $"{(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
            );
        }
    }

    /// <summary>
    /// 同步批量删除数据库中的数据。
    /// </summary>
    public static OperationResult BatchDeleteData(Table table, string whereColumn, IEnumerable<string> whereValues)
    {
        if (isDisposed)
        {
            Log.Error("数据库服务已释放，无法执行批量删除");
            return OperationResult.Failure;
        }

        if (string.IsNullOrEmpty(whereColumn))
        {
            Log.Error("批量删除条件列名为空");
            return OperationResult.Failure;
        }

        var valuesList = whereValues.ToList();
        if (valuesList.Count == 0)
        {
            Log.Warning("批量删除值列表为空，跳过操作");
            return OperationResult.Success;
        }

        EnsureTableExists();
        var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;
        int totalRowsAffected = 0;
        int successCount = 0;
        int failCount = 0;

        try
        {
            Log.Debug($"开始批量删除操作（同步）: 表 {table:G}, 条件列 {whereColumn}, 共 {valuesList.Count} 项");

            // 使用事务进行批量操作
            return ExecuteInTransaction(() =>
            {
                int index = 0;
                foreach (string value in valuesList)
                {
                    using var command = CreateCommand();
                    string sqlCommand = $"DELETE FROM {table:G} WHERE {whereColumn} = @whereValue";
                    command.CommandText = sqlCommand;
                    command.Parameters.AddWithValue("@whereValue", value);

                    if (EnableVerboseLogging)
                    {
                        Log.Debug($"执行批量删除（同步）#{++index}: {sqlCommand} [参数: {value}]");
                    }

                    int rowsAffected = command.ExecuteNonQuery();
                    totalRowsAffected += rowsAffected;

                    if (rowsAffected > 0)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        Log.Warning(
                            $"批量删除项（同步）#{index} 失败: 没有找到匹配的记录 (条件: {whereColumn} = {value})"
                        );
                    }
                }
            });
        }
        catch (Exception ex)
        {
            stopwatch?.Stop();
            Log.Error($"批量删除数据失败（同步）: {ex.Message}");
            Log.Debug($"异常详情: {ex}");
            return OperationResult.Failure;
        }
        finally
        {
            stopwatch?.Stop();
            Log.Info(
                $"批量删除操作完成（同步）: 成功 {successCount} 项, 失败 {failCount} 项, 共影响 {totalRowsAffected} 行数据 "
                    + $"{(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
            );
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 将数据库行转换为字典
    /// </summary>
    private static Dictionary<string, object> ReadRowToDictionary(SqliteDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var result = new Dictionary<string, object>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            string name = reader.GetName(i);
            object value = reader.IsDBNull(i) ? null! : reader.GetValue(i);
            result[name] = value;
        }
        return result;
    }

    /// <summary>
    /// 执行带有重试机制的操作
    /// </summary>
    private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (string.IsNullOrEmpty(operationName))
        {
            throw new ArgumentException("操作名称不能为空", nameof(operationName));
        }

        int retryCount = 0;
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (SqliteException ex)
                when ((
                        ex.SqliteErrorCode == 5
                        || // SQLITE_BUSY
                        ex.SqliteErrorCode == 6
                    ) // SQLITE_LOCKED
                    && retryCount < MaxRetryCount
                )
            {
                retryCount++;
                await Log.WarningAsync(
                    $"{operationName} 操作遇到数据库锁定，将进行第 {retryCount} 次重试 (错误码: {ex.SqliteErrorCode})"
                );
                await Task.Delay(RetryDelay * retryCount);
            }
        }
    }

    /// <summary>
    /// 执行带有重试机制的操作（同步版本）
    /// </summary>
    private static T ExecuteWithRetry<T>(Func<T> operation, string operationName)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (string.IsNullOrEmpty(operationName))
        {
            throw new ArgumentException("操作名称不能为空", nameof(operationName));
        }

        int retryCount = 0;
        while (true)
        {
            try
            {
                return operation();
            }
            catch (SqliteException ex)
                when ((
                        ex.SqliteErrorCode == 5
                        || // SQLITE_BUSY
                        ex.SqliteErrorCode == 6
                    ) // SQLITE_LOCKED
                    && retryCount < MaxRetryCount
                )
            {
                retryCount++;
                Log.Warning(
                    $"{operationName} 操作遇到数据库锁定，将进行第 {retryCount} 次重试 (错误码: {ex.SqliteErrorCode})"
                );
                Thread.Sleep(RetryDelay * retryCount);
            }
        }
    }

    /// <summary>
    /// 备份数据库文件
    /// </summary>
    public static OperationResult BackupDatabase(string backupPath = "")
    {
        if (isDisposed)
        {
            Log.Error("数据库服务已释放，无法执行备份");
            return OperationResult.Failure;
        }

        if (string.IsNullOrEmpty(backupPath))
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            backupPath = $"{MainConfig.DatabaseSavePath}.backup_{timestamp}";
        }

        try
        {
            // 确保数据库连接已关闭
            CloseConnectionAsync().Wait();

            // 复制数据库文件
            if (File.Exists(MainConfig.DatabaseSavePath))
            {
                File.Copy(MainConfig.DatabaseSavePath, backupPath, true);
                Log.Info($"数据库已成功备份到: {backupPath}");
                return OperationResult.Success;
            }

            Log.Warning($"数据库备份失败: 源文件不存在 {MainConfig.DatabaseSavePath}");
            return OperationResult.NotFound;
        }
        catch (Exception ex)
        {
            Log.Error($"数据库备份失败: {ex.Message}");
            Log.Debug($"异常详情: {ex}");
            return OperationResult.Failure;
        }
    }

    /// <summary>
    /// 异步备份数据库文件
    /// </summary>
    public static async Task<OperationResult> BackupDatabaseAsync(string backupPath = "")
    {
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行备份");
            return OperationResult.Failure;
        }

        if (string.IsNullOrEmpty(backupPath))
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            backupPath = $"{MainConfig.DatabaseSavePath}.backup_{timestamp}";
        }

        try
        {
            // 确保数据库连接已关闭
            await CloseConnectionAsync();

            // 复制数据库文件
            if (File.Exists(MainConfig.DatabaseSavePath))
            {
                File.Copy(MainConfig.DatabaseSavePath, backupPath, true);
                await Log.InfoAsync($"数据库已成功备份到: {backupPath}");
                return OperationResult.Success;
            }

            await Log.WarningAsync($"数据库备份失败: 源文件不存在 {MainConfig.DatabaseSavePath}");
            return OperationResult.NotFound;
        }
        catch (Exception ex)
        {
            await Log.ErrorAsync($"数据库备份失败: {ex.Message}");
            await Log.DebugAsync($"异常详情: {ex}");
            return OperationResult.Failure;
        }
    }

    /// <summary>
    /// 执行数据库完整性检查
    /// </summary>
    public static async Task<OperationResult> CheckDatabaseIntegrityAsync()
    {
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行完整性检查");
            return OperationResult.Failure;
        }

        await EnsureTableExistsAsync();

        await _poolLock.WaitAsync();
        try
        {
            await using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

            try
            {
                await Log.InfoAsync("开始执行数据库完整性检查...");
                command.CommandText = "PRAGMA integrity_check";

                string result = (await command.ExecuteScalarAsync())?.ToString() ?? string.Empty;

                stopwatch?.Stop();
                if (result.Equals("ok", StringComparison.OrdinalIgnoreCase))
                {
                    await Log.InfoAsync(
                        $"数据库完整性检查通过 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                    );
                    return OperationResult.Success;
                }

                await Log.ErrorAsync($"数据库完整性检查失败: {result}");
                return OperationResult.Failure;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"执行数据库完整性检查时出错: {ex.Message}");
                await Log.DebugAsync($"异常详情: {ex}");
                return OperationResult.Failure;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    /// <summary>
    /// 执行数据库优化
    /// </summary>
    public static async Task<OperationResult> OptimizeDatabaseAsync()
    {
        if (isDisposed)
        {
            await Log.ErrorAsync("数据库服务已释放，无法执行优化");
            return OperationResult.Failure;
        }

        await EnsureTableExistsAsync();

        await _poolLock.WaitAsync();
        try
        {
            await using var command = CreateCommand();
            var stopwatch = EnablePerformanceMonitoring ? Stopwatch.StartNew() : null;

            try
            {
                await Log.InfoAsync("开始执行数据库优化...");

                // 执行VACUUM操作，重建数据库以回收空间
                command.CommandText = "VACUUM";
                await command.ExecuteNonQueryAsync();

                // 分析数据库以优化查询计划
                command.CommandText = "ANALYZE";
                await command.ExecuteNonQueryAsync();

                stopwatch?.Stop();
                await Log.InfoAsync(
                    $"数据库优化完成 {(stopwatch != null ? $"(耗时: {stopwatch.ElapsedMilliseconds} ms)" : "")}"
                );
                return OperationResult.Success;
            }
            catch (Exception ex)
            {
                stopwatch?.Stop();
                await Log.ErrorAsync($"执行数据库优化时出错: {ex.Message}");
                await Log.DebugAsync($"异常详情: {ex}");
                return OperationResult.Failure;
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    #endregion
}
