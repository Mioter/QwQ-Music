using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QwQ_Music.Models;

namespace QwQ_Music.Common.Services.Databases;

public sealed class PlaylistRepository(string dbPath) : IAsyncDisposable
{
    public const string TABLE_NAME = "PLAYLIST_ITEM";

    // 为插入留空的步长（建议 1024）
    private const int SORTKEY_STEP = 1024;

    private readonly DatabaseService _db = new(dbPath);
    private bool _initialized;

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    // 初始化并创建表和索引（仅首次）
    private async Task InitializeAsync()
    {
        if (_initialized) return;

        await _db.InitializeAsync();

        // 基础表结构：自增 Id、FilePath 唯一 + 外键、SortKey 排序键
        await _db.CreateTableAsync(
            TABLE_NAME,
            $"""
             Id INTEGER PRIMARY KEY AUTOINCREMENT,
             {nameof(MusicItemModel.FilePath)} TEXT NOT NULL UNIQUE,
             SortKey INTEGER NOT NULL,
             FOREIGN KEY ({nameof(MusicItemModel.FilePath)})
               REFERENCES {MusicItemRepository.TABLE_NAME}({nameof(MusicItemModel.FilePath)})
               ON DELETE CASCADE
             """);

        // 索引：SortKey 排序/定位
        await _db.ExecuteAsync($"""
            CREATE INDEX IF NOT EXISTS IX_{TABLE_NAME}_SortKey
            ON {TABLE_NAME}(SortKey)
            """);

        _initialized = true;
    }

    // =========================
    // 公共 API
    // =========================

    // 尾部追加（等价于 AddAsync）
    public async Task AddAsync(string filePath)
    {
        await InitializeAsync();

        await _db.ExecuteAsync("BEGIN TRANSACTION");
        try
        {
            long lastKey = await GetLastSortKeyAsync();
            long newKey = lastKey > 0 ? lastKey + SORTKEY_STEP : SORTKEY_STEP;

            await InsertRowAsync(filePath, newKey);
            await _db.ExecuteAsync("COMMIT");
        }
        catch
        {
            await _db.ExecuteAsync("ROLLBACK");
            throw;
        }
    }

    // 批量尾部追加
    public async Task AddRangeAsync(IEnumerable<string> filePaths)
    {
        await InitializeAsync();

        var list = filePaths?.ToList() ?? [];
        if (list.Count == 0) return;

        await _db.ExecuteAsync("BEGIN TRANSACTION");
        try
        {
            long lastKey = await GetLastSortKeyAsync();
            long key = lastKey > 0 ? lastKey + SORTKEY_STEP : SORTKEY_STEP;

            foreach (string path in list)
            {
                await InsertRowAsync(path, key);
                key += SORTKEY_STEP;
            }

            await _db.ExecuteAsync("COMMIT");
        }
        catch
        {
            await _db.ExecuteAsync("ROLLBACK");
            throw;
        }
    }

    // 替换整个播放列表（保持顺序）
    public async Task SetAllAsync(IEnumerable<string> filePaths)
    {
        await InitializeAsync();

        var list = filePaths?.ToList() ?? [];

        await _db.ExecuteAsync("BEGIN TRANSACTION");
        try
        {
            await _db.ExecuteAsync($"DELETE FROM {TABLE_NAME}");

            long key = SORTKEY_STEP;
            foreach (string path in list)
            {
                await InsertRowAsync(path, key);
                key += SORTKEY_STEP;
            }

            await _db.ExecuteAsync("COMMIT");
        }
        catch
        {
            await _db.ExecuteAsync("ROLLBACK");
            throw;
        }
    }

    // 获取按顺序的所有文件路径
    public async Task<List<string>> GetAllAsync()
    {
        await InitializeAsync();

        var rows = await _db.QueryAsync($"""
            SELECT {nameof(MusicItemModel.FilePath)}
            FROM {TABLE_NAME}
            ORDER BY SortKey ASC
            """);

        return rows.Select(r => r[nameof(MusicItemModel.FilePath)]?.ToString() ?? "").ToList();
    }

    // 清空播放列表
    public async Task ClearAsync()
    {
        await InitializeAsync();
        await _db.ExecuteAsync($"DELETE FROM {TABLE_NAME}");
    }

    // 移除指定索引的歌曲
    public async Task RemoveAtAsync(int index)
    {
        await InitializeAsync();

        string? filePath = await GetAtAsync(index);
        if (filePath is null) return;

        await RemoveAsync(filePath);
    }

    // 移除指定歌曲
    public async Task RemoveAsync(string filePath)
    {
        await InitializeAsync();
        await _db.ExecuteAsync(
            $"DELETE FROM {TABLE_NAME} WHERE {nameof(MusicItemModel.FilePath)} = @f",
            new Dictionary<string, object> { ["f"] = filePath });
    }

    // 批量移除指定歌曲
    public async Task RemoveAsync(IEnumerable<string> filePaths)
    {
        await InitializeAsync();

        var list = filePaths?.Distinct().ToList() ?? [];
        if (list.Count == 0) return;

        string placeholders = string.Join(",", list.Select((_, i) => $"@f{i}"));
        var p = new Dictionary<string, object>();

        for (int i = 0; i < list.Count; i++)
            p[$"f{i}"] = list[i];

        await _db.ExecuteAsync(
            $"""
             DELETE FROM {TABLE_NAME}
             WHERE {nameof(MusicItemModel.FilePath)} IN ({placeholders})
             """,
            p);
    }

    // 在指定位置插入（如果已存在则移动到该位置）
    public async Task InsertAsync(string filePath, int position)
    {
        await InitializeAsync();

        if (await ContainsAsync(filePath))
        {
            await MoveToIndexAsync(filePath, position);
            return;
        }

        await _db.ExecuteAsync("BEGIN TRANSACTION");
        try
        {
            (long? prev, long? next) = await GetNeighborKeysAsync(position);

            long newKey = await ComputeInsertKeyOrNormalizeAsync(prev, next, position);

            await InsertRowAsync(filePath, newKey);
            await _db.ExecuteAsync("COMMIT");
        }
        catch
        {
            await _db.ExecuteAsync("ROLLBACK");
            throw;
        }
    }

    // 按位置移动项（from -> to）
    public async Task MoveAsync(int fromPosition, int toPosition)
    {
        await InitializeAsync();
        if (fromPosition == toPosition) return;

        string? filePath = await GetAtAsync(fromPosition);
        if (filePath is null) return;

        await MoveToIndexAsync(filePath, toPosition);
    }

    // 获取播放列表大小
    public async Task<int> CountAsync()
    {
        await InitializeAsync();

        var row = (await _db.QueryAsync($"SELECT COUNT(*) AS c FROM {TABLE_NAME}")).FirstOrDefault();

        return row?["c"] is long c ? (int)c : 0;
    }

    // 是否为空
    public async Task<bool> IsEmptyAsync() => await CountAsync() == 0;

    // 获取指定索引的歌曲路径
    public async Task<string?> GetAtAsync(int index)
    {
        await InitializeAsync();

        var row = (await _db.QueryAsync($"""
            SELECT {nameof(MusicItemModel.FilePath)}
            FROM {TABLE_NAME}
            ORDER BY SortKey ASC
            LIMIT 1 OFFSET @i
            """, new Dictionary<string, object> { ["i"] = Math.Max(index, 0) }))
            .FirstOrDefault();

        return row?[nameof(MusicItemModel.FilePath)]?.ToString();
    }

    // 获取歌曲在列表中的索引（不存在返回 -1）
    public async Task<int> GetPositionAsync(string filePath)
    {
        await InitializeAsync();

        var keyRow = (await _db.QueryAsync($"""
            SELECT SortKey FROM {TABLE_NAME}
            WHERE {nameof(MusicItemModel.FilePath)} = @f
            """, new Dictionary<string, object> { ["f"] = filePath })).FirstOrDefault();

        if (keyRow?["SortKey"] is not long key) return -1;

        var cntRow = (await _db.QueryAsync($"""
            SELECT COUNT(*) AS c FROM {TABLE_NAME}
            WHERE SortKey < @k
            """, new Dictionary<string, object> { ["k"] = key })).FirstOrDefault();

        return cntRow?["c"] is long c ? (int)c : -1;
    }

    // 是否包含
    public async Task<bool> ContainsAsync(string filePath)
    {
        await InitializeAsync();

        var rows = await _db.QueryAsync($"""
            SELECT 1 FROM {TABLE_NAME}
            WHERE {nameof(MusicItemModel.FilePath)} = @f
            LIMIT 1
            """, new Dictionary<string, object> { ["f"] = filePath });

        return rows.Count > 0;
    }

    // =========================
    // 私有方法
    // =========================

    // 插入一行
    private Task InsertRowAsync(string filePath, long sortKey)
        => _db.ExecuteAsync($"""
            INSERT INTO {TABLE_NAME} ({nameof(MusicItemModel.FilePath)}, SortKey)
            VALUES (@f, @k)
            """, new Dictionary<string, object> { ["f"] = filePath, ["k"] = sortKey });

    // 获取最后一个 SortKey（没有则 0）
    private async Task<long> GetLastSortKeyAsync()
    {
        var row = (await _db.QueryAsync($"""
            SELECT SortKey FROM {TABLE_NAME}
            ORDER BY SortKey DESC LIMIT 1
            """)).FirstOrDefault();

        return row?["SortKey"] is long k ? k : 0L;
    }

    // 计算插入位置的前/后邻居键
    // 返回：(prev, next)，index 为插入到 index 的语义（0..Count）
    private async Task<(long? prev, long? next)> GetNeighborKeysAsync(int index)
    {
        int count = await CountAsync();
        int clamped = Math.Max(0, Math.Min(index, count));

        // 偏移 = clamped - 1，取两条
        int offset = Math.Max(0, clamped - 1);

        var rows = await _db.QueryAsync($"""
            SELECT SortKey FROM {TABLE_NAME}
            ORDER BY SortKey ASC
            LIMIT 2 OFFSET @off
            """, new Dictionary<string, object> { ["off"] = offset });

        long? prev = null;
        long? next = null;

        if (clamped == 0)
        {
            // rows[0] 是 next
            if (rows.ElementAtOrDefault(0)?["SortKey"] is long n0) next = n0;
        }
        else
        {
            // rows[0] 是 prev，rows[1] 可能是 next
            if (rows.ElementAtOrDefault(0)?["SortKey"] is long p0) prev = p0;
            if (rows.ElementAtOrDefault(1)?["SortKey"] is long n1) next = n1;
        }

        return (prev, next);
    }

    // 计算插入新键（如无间隙则归一化后重试）
    private async Task<long> ComputeInsertKeyOrNormalizeAsync(long? prev, long? next, int indexForRetry)
    {
        long newKey;

        switch (prev)
        {
            case null when next is null:
                newKey = SORTKEY_STEP; // 空表

                break;

            // 插到最前
            case null when next.Value > 1:
                newKey = next.Value / 2;

                break;
            case null:
                {
                    await NormalizeSortKeysAsync();
                    (long? p2, long? n2) = await GetNeighborKeysAsync(indexForRetry);
                    newKey = ComputeInsertKeyNoNormalize(p2, n2);

                    break;
                }
            default:
                {
                    if (next is null) // 插到末尾
                    {
                        newKey = prev.Value + SORTKEY_STEP;
                    }
                    else
                    {
                        // 有间隙则取中位数
                        if (next.Value - prev.Value > 1)
                            newKey = (prev.Value + next.Value) / 2;
                        else
                        {
                            await NormalizeSortKeysAsync();
                            (long? p2, long? n2) = await GetNeighborKeysAsync(indexForRetry);
                            newKey = ComputeInsertKeyNoNormalize(p2, n2);
                        }
                    }

                    break;
                }
        }

        return newKey;
    }

    private static long ComputeInsertKeyNoNormalize(long? prev, long? next)
    {
        switch (prev)
        {
            case null when next is null:
                return SORTKEY_STEP;
            case null:
                return next.Value / 2;
        }

        if (next is null) return prev.Value + SORTKEY_STEP;
        return (prev.Value + next.Value) / 2;
    }

    // 将已有项移动到新索引（仅更新 SortKey）
    private async Task MoveToIndexAsync(string filePath, int newIndex)
    {
        // 获取当前索引
        int currIndex = await GetPositionAsync(filePath);
        if (currIndex < 0) return;
        if (currIndex == newIndex) return;

        int count = await CountAsync();
        int clamped = Math.Max(0, Math.Min(newIndex, count - 1));

        // 从当前列表移除后的目标索引（old < new 时，目标应左移一位）
        int targetIndexAfterRemoval = newIndex > currIndex ? Math.Max(0, clamped) - 1 : clamped;

        await _db.ExecuteAsync("BEGIN TRANSACTION");
        try
        {
            (long? prev, long? next) = await GetNeighborKeysExcludingAsync(targetIndexAfterRemoval, filePath);
            long newKey = await ComputeInsertKeyOrNormalizeExcludingAsync(prev, next, targetIndexAfterRemoval, filePath);

            await _db.ExecuteAsync($"""
                UPDATE {TABLE_NAME}
                SET SortKey = @k
                WHERE {nameof(MusicItemModel.FilePath)} = @f
                """, new Dictionary<string, object> { ["k"] = newKey, ["f"] = filePath });

            await _db.ExecuteAsync("COMMIT");
        }
        catch
        {
            await _db.ExecuteAsync("ROLLBACK");
            throw;
        }
    }

    // 计算排除指定文件后的邻居键
    private async Task<(long? prev, long? next)> GetNeighborKeysExcludingAsync(int index, string excludeFile)
    {
        int count = await CountAsync();
        int clamped = Math.Max(0, Math.Min(index, Math.Max(0, count - 1)));

        int offset = Math.Max(0, clamped - 1);
        var rows = await _db.QueryAsync($"""
            SELECT SortKey FROM {TABLE_NAME}
            WHERE {nameof(MusicItemModel.FilePath)} <> @ex
            ORDER BY SortKey ASC
            LIMIT 2 OFFSET @off
            """, new Dictionary<string, object> { ["ex"] = excludeFile, ["off"] = offset });

        long? prev = null, next = null;

        if (clamped == 0)
        {
            if (rows.ElementAtOrDefault(0)?["SortKey"] is long n0) next = n0;
        }
        else
        {
            if (rows.ElementAtOrDefault(0)?["SortKey"] is long p0) prev = p0;
            if (rows.ElementAtOrDefault(1)?["SortKey"] is long n1) next = n1;
        }

        return (prev, next);
    }

    private async Task<long> ComputeInsertKeyOrNormalizeExcludingAsync(long? prev, long? next, int indexForRetry, string excludeFile)
    {
        switch (prev)
        {
            case null when next is null:
                return SORTKEY_STEP;
            case null when next.Value > 1:
                return next.Value / 2;
            case null:
                await NormalizeSortKeysAsync();
                (long? p2, long? n2) = await GetNeighborKeysExcludingAsync(indexForRetry, excludeFile);
                return ComputeInsertKeyNoNormalize(p2, n2);
        }

        if (next is null) return prev.Value + SORTKEY_STEP;

        if (next.Value - prev.Value > 1) return (prev.Value + next.Value) / 2;

        await NormalizeSortKeysAsync();
        (long? p3, long? n3) = await GetNeighborKeysExcludingAsync(indexForRetry, excludeFile);
        return ComputeInsertKeyNoNormalize(p3, n3);
    }

    // 将 SortKey 归一化为等间隔（罕见路径）
    private async Task NormalizeSortKeysAsync()
    {
        await _db.ExecuteAsync("BEGIN TRANSACTION");
        try
        {
            var rows = await _db.QueryAsync($"""
                SELECT Id FROM {TABLE_NAME}
                ORDER BY SortKey ASC
                """);

            long key = SORTKEY_STEP;
            foreach (var row in rows)
            {
                await _db.ExecuteAsync($"""
                    UPDATE {TABLE_NAME}
                    SET SortKey = @k
                    WHERE Id = @id
                    """, new Dictionary<string, object> { ["k"] = key, ["id"] = row["Id"]! });
                key += SORTKEY_STEP;
            }

            await _db.ExecuteAsync("COMMIT");
        }
        catch
        {
            await _db.ExecuteAsync("ROLLBACK");
            throw;
        }
    }
}
