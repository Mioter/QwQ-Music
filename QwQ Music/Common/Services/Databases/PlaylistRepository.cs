using System;
using System.Collections.Generic;
using System.Linq;
using QwQ_Music.Models;

namespace QwQ_Music.Common.Services.Databases;

public sealed class PlaylistRepository : IDisposable
{
    public const string TABLE_NAME = "PLAYLIST_ITEM";

    // 为插入留空的步长（建议 1024）
    private const int SORTKEY_STEP = 1024;

    private readonly DatabaseService _db;
    private bool _initialized;

    public PlaylistRepository(string dbPath)
    {
        _db = new DatabaseService(dbPath);
        Initialize();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // 初始化并创建表和索引
    private void Initialize()
    {
        if (_initialized) return;

        // 基础表结构：自增 Id、FilePath 唯一 + 外键、SortKey 排序键
        _db.CreateTable(TABLE_NAME,
            $"""
             Id INTEGER PRIMARY KEY AUTOINCREMENT,
             {nameof(MusicItemModel.FilePath)} TEXT NOT NULL UNIQUE,
             SortKey INTEGER NOT NULL,
             FOREIGN KEY ({nameof(MusicItemModel.FilePath)})
               REFERENCES {MusicItemRepository.TABLE_NAME}({nameof(MusicItemModel.FilePath)})
               ON DELETE CASCADE
             """);

        // 索引：SortKey 排序/定位
        _db.Execute($"""
                     CREATE INDEX IF NOT EXISTS IX_{TABLE_NAME}_SortKey
                     ON {TABLE_NAME}(SortKey)
                     """);

        _initialized = true;
    }

    // =========================
    // 公共 API
    // =========================

    // 尾部追加
    public void Add(string filePath)
    {
        _db.BeginTransaction();

        try
        {
            long lastKey = GetLastSortKey();
            long newKey = lastKey > 0 ? lastKey + SORTKEY_STEP : SORTKEY_STEP;

            InsertRow(filePath, newKey);
            _db.Commit();
        }
        catch
        {
            _db.Rollback();

            throw;
        }
    }

    // 批量尾部追加
    public void AddRange(IEnumerable<string> filePaths)
    {
        var list = filePaths?.ToList() ?? [];

        if (list.Count == 0) return;

        _db.BeginTransaction();

        try
        {
            long lastKey = GetLastSortKey();
            long key = lastKey > 0 ? lastKey + SORTKEY_STEP : SORTKEY_STEP;

            foreach (string path in list)
            {
                InsertRow(path, key);
                key += SORTKEY_STEP;
            }

            _db.Commit();
        }
        catch
        {
            _db.Rollback();

            throw;
        }
    }

    // 替换整个播放列表（保持顺序）
    public void SetAll(IEnumerable<string> filePaths)
    {
        var list = filePaths?.ToList() ?? [];

        _db.BeginTransaction();

        try
        {
            _db.Execute($"DELETE FROM {TABLE_NAME}");

            long key = SORTKEY_STEP;

            foreach (string path in list)
            {
                InsertRow(path, key);
                key += SORTKEY_STEP;
            }

            _db.Commit();
        }
        catch
        {
            _db.Rollback();

            throw;
        }
    }

    // 获取按顺序的所有文件路径
    public List<string> GetAll()
    {
        var rows = _db.Query($"""
                              SELECT {nameof(MusicItemModel.FilePath)}
                              FROM {TABLE_NAME}
                              ORDER BY SortKey ASC
                              """);

        return rows.Select(r => r[nameof(MusicItemModel.FilePath)]?.ToString() ?? "").ToList();
    }

    // 清空播放列表
    public void Clear()
    {
        _db.Execute($"DELETE FROM {TABLE_NAME}");
    }

    // 移除指定索引的歌曲
    public void RemoveAt(int index)
    {
        string? filePath = GetAt(index);

        if (filePath is null) return;

        Remove(filePath);
    }

    // 移除指定歌曲
    public void Remove(string filePath)
    {
        _db.Execute(
            $"DELETE FROM {TABLE_NAME} WHERE {nameof(MusicItemModel.FilePath)} = @f",
            new Dictionary<string, object>
            {
                ["f"] = filePath,
            });
    }

    // 批量移除指定歌曲
    public void Remove(IEnumerable<string> filePaths)
    {
        var list = filePaths?.Distinct().ToList() ?? [];

        if (list.Count == 0) return;

        string placeholders = string.Join(",", list.Select((_, i) => $"@f{i}"));
        var p = new Dictionary<string, object>();

        for (int i = 0; i < list.Count; i++)
        {
            p[$"f{i}"] = list[i];
        }

        _db.Execute(
            $"""
             DELETE FROM {TABLE_NAME}
             WHERE {nameof(MusicItemModel.FilePath)} IN ({placeholders})
             """,
            p);
    }

    // 在指定位置插入（如果已存在则移动到该位置）
    public void Insert(string filePath, int position)
    {
        if (Contains(filePath))
        {
            MoveToIndex(filePath, position);

            return;
        }

        _db.BeginTransaction();

        try
        {
            (long? prev, long? next) = GetNeighborKeys(position);

            long newKey = ComputeInsertKeyOrNormalize(prev, next, position);

            InsertRow(filePath, newKey);
            _db.Commit();
        }
        catch
        {
            _db.Rollback();

            throw;
        }
    }

    // 按位置移动项（from -> to）
    public void Move(int fromPosition, int toPosition)
    {
        if (fromPosition == toPosition) return;

        string? filePath = GetAt(fromPosition);

        if (filePath is null) return;

        MoveToIndex(filePath, toPosition);
    }

    // 获取播放列表大小
    public int Count()
    {
        var row = _db.Query($"SELECT COUNT(*) AS c FROM {TABLE_NAME}").FirstOrDefault();

        return row?["c"] is long c ? (int)c : 0;
    }

    // 是否为空
    public bool IsEmpty()
    {
        return Count() == 0;
    }

    // 获取指定索引的歌曲路径
    public string? GetAt(int index)
    {
        var row = _db.Query($"""
                             SELECT {nameof(MusicItemModel.FilePath)}
                             FROM {TABLE_NAME}
                             ORDER BY SortKey ASC
                             LIMIT 1 OFFSET @i
                             """, new Dictionary<string, object>
            {
                ["i"] = Math.Max(index, 0),
            })
            .FirstOrDefault();

        return row?[nameof(MusicItemModel.FilePath)]?.ToString();
    }

    // 获取歌曲在列表中的索引（不存在返回 -1）
    public int GetPosition(string filePath)
    {
        var keyRow = _db.Query($"""
                                SELECT SortKey FROM {TABLE_NAME}
                                WHERE {nameof(MusicItemModel.FilePath)} = @f
                                """, new Dictionary<string, object>
        {
            ["f"] = filePath,
        }).FirstOrDefault();

        if (keyRow?["SortKey"] is not long key) return -1;

        var cntRow = _db.Query($"""
                                SELECT COUNT(*) AS c FROM {TABLE_NAME}
                                WHERE SortKey < @k
                                """, new Dictionary<string, object>
        {
            ["k"] = key,
        }).FirstOrDefault();

        return cntRow?["c"] is long c ? (int)c : -1;
    }

    // 是否包含
    public bool Contains(string filePath)
    {
        var rows = _db.Query($"""
                              SELECT 1 FROM {TABLE_NAME}
                              WHERE {nameof(MusicItemModel.FilePath)} = @f
                              LIMIT 1
                              """, new Dictionary<string, object>
        {
            ["f"] = filePath,
        });

        return rows.Count > 0;
    }

    // =========================
    // 私有方法
    // =========================

    // 插入一行
    private void InsertRow(string filePath, long sortKey)
    {
        _db.Execute($"""
                     INSERT INTO {TABLE_NAME} ({nameof(MusicItemModel.FilePath)}, SortKey)
                     VALUES (@f, @k)
                     """, new Dictionary<string, object>
        {
            ["f"] = filePath,
            ["k"] = sortKey,
        });
    }

    // 获取最后一个 SortKey（没有则 0）
    private long GetLastSortKey()
    {
        var row = _db.Query($"""
                             SELECT SortKey FROM {TABLE_NAME}
                             ORDER BY SortKey DESC LIMIT 1
                             """).FirstOrDefault();

        return row?["SortKey"] is long k ? k : 0L;
    }

    // 计算插入位置的前/后邻居键
    // 返回：(prev, next)，index 为插入到 index 的语义（0..Count）
    private (long? prev, long? next) GetNeighborKeys(int index)
    {
        int count = Count();
        int clamped = Math.Max(0, Math.Min(index, count));

        // 偏移 = clamped - 1，取两条
        int offset = Math.Max(0, clamped - 1);

        var rows = _db.Query($"""
                              SELECT SortKey FROM {TABLE_NAME}
                              ORDER BY SortKey ASC
                              LIMIT 2 OFFSET @off
                              """, new Dictionary<string, object>
        {
            ["off"] = offset,
        });

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
    private long ComputeInsertKeyOrNormalize(long? prev, long? next, int indexForRetry)
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
                    NormalizeSortKeys();
                    (long? p2, long? n2) = GetNeighborKeys(indexForRetry);
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
                            NormalizeSortKeys();
                            (long? p2, long? n2) = GetNeighborKeys(indexForRetry);
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
    private void MoveToIndex(string filePath, int newIndex)
    {
        // 获取当前索引
        int currIndex = GetPosition(filePath);

        if (currIndex < 0) return;
        if (currIndex == newIndex) return;

        int count = Count();
        int clamped = Math.Max(0, Math.Min(newIndex, count - 1));

        // 从当前列表移除后的目标索引（old < new 时，目标应左移一位）
        int targetIndexAfterRemoval = newIndex > currIndex ? Math.Max(0, clamped) - 1 : clamped;

        _db.BeginTransaction();

        try
        {
            (long? prev, long? next) = GetNeighborKeysExcluding(targetIndexAfterRemoval, filePath);
            long newKey = ComputeInsertKeyOrNormalizeExcluding(prev, next, targetIndexAfterRemoval, filePath);

            _db.Execute($"""
                         UPDATE {TABLE_NAME}
                         SET SortKey = @k
                         WHERE {nameof(MusicItemModel.FilePath)} = @f
                         """, new Dictionary<string, object>
            {
                ["k"] = newKey,
                ["f"] = filePath,
            });

            _db.Commit();
        }
        catch
        {
            _db.Rollback();

            throw;
        }
    }

    // 计算排除指定文件后的邻居键
    private (long? prev, long? next) GetNeighborKeysExcluding(int index, string excludeFile)
    {
        int count = Count();
        int clamped = Math.Max(0, Math.Min(index, Math.Max(0, count - 1)));

        int offset = Math.Max(0, clamped - 1);

        var rows = _db.Query($"""
                              SELECT SortKey FROM {TABLE_NAME}
                              WHERE {nameof(MusicItemModel.FilePath)} <> @ex
                              ORDER BY SortKey ASC
                              LIMIT 2 OFFSET @off
                              """, new Dictionary<string, object>
        {
            ["ex"] = excludeFile,
            ["off"] = offset,
        });

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

    private long ComputeInsertKeyOrNormalizeExcluding(long? prev, long? next, int indexForRetry, string excludeFile)
    {
        switch (prev)
        {
            case null when next is null:
                return SORTKEY_STEP;
            case null when next.Value > 1:
                return next.Value / 2;
            case null:
                NormalizeSortKeys();
                (long? p2, long? n2) = GetNeighborKeysExcluding(indexForRetry, excludeFile);

                return ComputeInsertKeyNoNormalize(p2, n2);
        }

        if (next is null) return prev.Value + SORTKEY_STEP;

        if (next.Value - prev.Value > 1) return (prev.Value + next.Value) / 2;

        NormalizeSortKeys();
        (long? p3, long? n3) = GetNeighborKeysExcluding(indexForRetry, excludeFile);

        return ComputeInsertKeyNoNormalize(p3, n3);
    }

    // 将 SortKey 归一化为等间隔（罕见路径）
    private void NormalizeSortKeys()
    {
        _db.BeginTransaction();

        try
        {
            var rows = _db.Query($"""
                                  SELECT Id FROM {TABLE_NAME}
                                  ORDER BY SortKey ASC
                                  """);

            long key = SORTKEY_STEP;

            foreach (var row in rows)
            {
                _db.Execute($"""
                             UPDATE {TABLE_NAME}
                             SET SortKey = @k
                             WHERE Id = @id
                             """, new Dictionary<string, object>
                {
                    ["k"] = key,
                    ["id"] = row["Id"]!,
                });

                key += SORTKEY_STEP;
            }

            _db.Commit();
        }
        catch
        {
            _db.Rollback();

            throw;
        }
    }
}
