using System;
using System.Collections.Generic;
using System.Linq;
using QwQ_Music.Models;

namespace QwQ_Music.Common.Services.Databases;

public class MusicListItemRepository : IDisposable
{
    public const string TABLE_NAME = "MUSIC_LIST_ITEM";
    private readonly DatabaseService _db;

    public MusicListItemRepository(string listIdStr, string dbPath)
    {
        ListIdStr = listIdStr;
        _db = new DatabaseService(dbPath);
        Initialize();
    }

    // --- 属性 ---

    public int Position { get; set; } // 保持为整数

    public string ListIdStr { get; }

    // --- Dispose ---

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Initialize()
    {
        // 创建表（如果不存在）
        _db.CreateTable(TABLE_NAME,
            $"""
             {nameof(MusicListModel.IdStr)} TEXT,
             {nameof(MusicItemModel.FilePath)} TEXT,
             {nameof(Position)} INTEGER,
             PRIMARY KEY ({nameof(MusicListModel.IdStr)}, {nameof(MusicItemModel.FilePath)}),
             FOREIGN KEY ({nameof(MusicListModel.IdStr)}) REFERENCES {MusicListMapRepository.TABLE_NAME}({nameof(MusicListModel.IdStr)}) ON DELETE CASCADE,
             FOREIGN KEY ({nameof(MusicItemModel.FilePath)}) REFERENCES {MusicItemRepository.TABLE_NAME}({nameof(MusicItemModel.FilePath)}) ON DELETE CASCADE
             """);
    }

    /// <summary>
    ///     添加歌曲到歌单末尾
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    public void Add(string filePath)
    {
        _db.BeginTransaction();

        try
        {
            int newPosition = GetMaxPosition() + 1;

            var data = new Dictionary<string, object?>
            {
                [nameof(MusicListModel.IdStr)] = ListIdStr,
                [nameof(MusicItemModel.FilePath)] = filePath,
                [nameof(Position)] = newPosition,
            };

            _db.Insert(TABLE_NAME, data);
            _db.Commit();
        }
        catch
        {
            _db.Rollback();

            throw;
        }
    }

    /// <summary>
    ///     在指定位置插入歌曲（后续项后移）
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    /// <param name="position">插入位置</param>
    public void Insert(string filePath, int position)
    {
        _db.BeginTransaction();

        try
        {
            // 将指定位置及后续所有项的 Position + 1
            ShiftPositionsForward(position, int.MaxValue);

            // 插入新项
            var data = new Dictionary<string, object?>
            {
                [nameof(MusicListModel.IdStr)] = ListIdStr,
                [nameof(MusicItemModel.FilePath)] = filePath,
                [nameof(Position)] = position,
            };

            _db.Insert(TABLE_NAME, data);
            _db.Commit();
        }
        catch
        {
            _db.Rollback();

            throw;
        }
    }

    /// <summary>
    ///     从歌单中删除指定歌曲（不调整其他项位置）
    /// </summary>
    /// <param name="filePath">音频文件路径</param>
    public void Remove(string filePath)
    {
        var whereParams = new Dictionary<string, object>
        {
            [nameof(MusicListModel.IdStr)] = ListIdStr,
            [nameof(MusicItemModel.FilePath)] = filePath,
        };

        _db.Delete(TABLE_NAME,
            $"{nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)} AND {nameof(MusicItemModel.FilePath)} = @{nameof(MusicItemModel.FilePath)}",
            whereParams);
    }

    /// <summary>
    ///     清空整个歌单
    /// </summary>
    public void Clear()
    {
        var whereParams = new Dictionary<string, object>
        {
            [nameof(MusicListModel.IdStr)] = ListIdStr,
        };

        _db.Delete(TABLE_NAME,
            $"{nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)}",
            whereParams);
    }

    /// <summary>
    ///     获取歌单中所有歌曲（按位置排序）
    /// </summary>
    /// <returns>文件路径列表</returns>
    public List<string> GetAll()
    {
        const string sql = $"SELECT {nameof(MusicItemModel.FilePath)} FROM {TABLE_NAME} " +
            $"WHERE {nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)} " +
            $"ORDER BY {nameof(Position)}";

        var parameters = new Dictionary<string, object>
        {
            [nameof(MusicListModel.IdStr)] = ListIdStr,
        };

        var result = _db.Query(sql, parameters);

        return result.Select(row => row[nameof(MusicItemModel.FilePath)]?.ToString() ?? "").ToList();
    }

    /// <summary>
    ///     检查某首歌是否在歌单中
    /// </summary>
    public bool Contains(string filePath)
    {
        const string sql = $"SELECT 1 FROM {TABLE_NAME} " +
            $"WHERE {nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)} " +
            $"AND {nameof(MusicItemModel.FilePath)} = @{nameof(MusicItemModel.FilePath)} " +
            $"LIMIT 1";

        var parameters = new Dictionary<string, object>
        {
            [nameof(MusicItemModel.FilePath)] = filePath,
            [nameof(MusicListModel.IdStr)] = ListIdStr,
        };

        return _db.Query(sql, parameters).Count != 0;
    }

    /// <summary>
    ///     移动歌曲位置（交换策略）
    /// </summary>
    /// <param name="filePath">要移动的歌曲文件路径</param>
    /// <param name="newPosition">新位置</param>
    public void Move(string filePath, int newPosition)
    {
        _db.BeginTransaction();

        try
        {
            // 获取当前项
            var currentItem = GetCurrentItem(filePath);
            int oldPosition = currentItem.Position;

            if (oldPosition == newPosition)
            {
                _db.Commit();

                return;
            }

            // 查找目标位置的项
            var targetItem = GetItemByPosition(newPosition);

            if (targetItem.HasValue)
            {
                // 交换两个项的 Position
                SwapPositions(filePath, targetItem.Value.FilePath);
            }
            else
            {
                // 目标位置为空，直接更新位置
                UpdateItemPosition(filePath, newPosition);
            }

            _db.Commit();
        }
        catch
        {
            _db.Rollback();

            throw;
        }
    }

    /// <summary>
    ///     重新整理位置，从 0 开始连续编号（清理空洞）
    /// </summary>
    public void ReorderPositions()
    {
        _db.BeginTransaction();

        try
        {
            var items = GetAllItemsWithPositions();

            for (int i = 0; i < items.Count; i++)
            {
                var whereParams = new Dictionary<string, object?>
                {
                    [nameof(MusicListModel.IdStr)] = ListIdStr,
                    [nameof(MusicItemModel.FilePath)] = items[i].FilePath,
                };

                var data = new Dictionary<string, object?>
                {
                    [nameof(Position)] = i,
                };

                _db.Update(TABLE_NAME, data,
                    $"{nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)} AND {nameof(MusicItemModel.FilePath)} = @{nameof(MusicItemModel.FilePath)}",
                    whereParams);
            }

            _db.Commit();
        }
        catch
        {
            _db.Rollback();

            throw;
        }
    }

    /// <summary>
    ///     检查并清理位置空洞（当 Position 值过于稀疏时）
    /// </summary>
    /// <param name="autoFix">是否自动修复</param>
    /// <returns>是否存在问题</returns>
    public bool CheckAndCleanupPositions(bool autoFix = true)
    {
        const string sql = $"""
                            SELECT COUNT(*) as count,
                                   MAX({nameof(Position)}) as max_pos
                            FROM {TABLE_NAME} 
                            WHERE {nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)}
                            """;

        var parameters = new Dictionary<string, object>
        {
            [nameof(MusicListModel.IdStr)] = ListIdStr,
        };

        var result = _db.Query(sql, parameters).FirstOrDefault();

        if (result == null)
            return false;

        int count = result["count"] is long c ? (int)c : 0;
        int maxPos = result["max_pos"] is long mp ? (int)mp : -1;

        // 如果最大位置远大于项目数，说明有很多空洞
        if (count <= 100 || maxPos <= count * 2) return false;

        if (autoFix)
        {
            ReorderPositions();
        }

        return true;
    }

    #region 私有辅助方法

    /// <summary>
    ///     获取当前最大位置值
    /// </summary>
    private int GetMaxPosition()
    {
        const string sql = $"SELECT MAX({nameof(Position)}) FROM {TABLE_NAME} WHERE {nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)}";

        var parameters = new Dictionary<string, object>
        {
            [nameof(MusicListModel.IdStr)] = ListIdStr,
        };

        var result = _db.Query(sql, parameters).FirstOrDefault();

        return result?.Values.First() is long l ? (int)l : -1;
    }

    /// <summary>
    ///     将 [start, end] 范围内的位置 +1（为插入腾空间）
    /// </summary>
    private void ShiftPositionsForward(int start, int end)
    {
        const string sql = $"""
                            UPDATE {TABLE_NAME} 
                            SET {nameof(Position)} = {nameof(Position)} + 1 
                            WHERE {nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)} 
                              AND {nameof(Position)} >= @start AND {nameof(Position)} <= @end
                            """;

        var parameters = new Dictionary<string, object>
        {
            [nameof(MusicListModel.IdStr)] = ListIdStr,
            ["start"] = start,
            ["end"] = end,
        };

        _db.Execute(sql, parameters);
    }

    /// <summary>
    ///     获取指定位置的项
    /// </summary>
    private (string FilePath, int Position)? GetItemByPosition(int position)
    {
        const string sql = $"""
                            SELECT {nameof(MusicItemModel.FilePath)}, {nameof(Position)} 
                            FROM {TABLE_NAME} 
                            WHERE {nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)} 
                              AND {nameof(Position)} = @position
                            """;

        var parameters = new Dictionary<string, object>
        {
            [nameof(MusicListModel.IdStr)] = ListIdStr,
            ["position"] = position,
        };

        var result = _db.Query(sql, parameters).FirstOrDefault();

        if (result == null) return null;

        return (result[nameof(MusicItemModel.FilePath)]?.ToString() ?? "",
            Convert.ToInt32(result[nameof(Position)]));
    }

    /// <summary>
    ///     获取当前项信息
    /// </summary>
    private (string FilePath, int Position) GetCurrentItem(string filePath)
    {
        const string sql = $"""
                            SELECT {nameof(MusicItemModel.FilePath)}, {nameof(Position)} 
                            FROM {TABLE_NAME} 
                            WHERE {nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)} 
                            AND {nameof(MusicItemModel.FilePath)} = @{nameof(MusicItemModel.FilePath)}
                            """;

        var parameters = new Dictionary<string, object>
        {
            [nameof(MusicListModel.IdStr)] = ListIdStr,
            [nameof(MusicItemModel.FilePath)] = filePath,
        };

        var result = _db.Query(sql, parameters).FirstOrDefault();

        return result == null ? throw new ArgumentException("Item not found") : (filePath, Convert.ToInt32(result[nameof(Position)]));
    }

    /// <summary>
    ///     交换两个项的位置
    /// </summary>
    private void SwapPositions(string filePath1, string filePath2)
    {
        var item1 = GetCurrentItem(filePath1);
        var item2 = GetCurrentItem(filePath2);

        // 使用临时负数避免冲突
        UpdateItemPosition(filePath1, -1);
        UpdateItemPosition(filePath2, item1.Position);
        UpdateItemPosition(filePath1, item2.Position);
    }

    /// <summary>
    ///     更新项的位置
    /// </summary>
    private void UpdateItemPosition(string filePath, int newPosition)
    {
        var data = new Dictionary<string, object?>
        {
            [nameof(Position)] = newPosition,
        };

        var whereParams = new Dictionary<string, object?>
        {
            [nameof(MusicListModel.IdStr)] = ListIdStr,
            [nameof(MusicItemModel.FilePath)] = filePath,
        };

        _db.Update(TABLE_NAME, data,
            $"{nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)} AND {nameof(MusicItemModel.FilePath)} = @{nameof(MusicItemModel.FilePath)}",
            whereParams);
    }

    /// <summary>
    ///     获取所有项及其位置
    /// </summary>
    private List<(string FilePath, int Position)> GetAllItemsWithPositions()
    {
        const string sql = $"SELECT {nameof(MusicItemModel.FilePath)}, {nameof(Position)} FROM {TABLE_NAME} " +
            $"WHERE {nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)} " +
            $"ORDER BY {nameof(Position)}";

        var parameters = new Dictionary<string, object>
        {
            [nameof(MusicListModel.IdStr)] = ListIdStr,
        };

        var result = _db.Query(sql, parameters);

        return result.Select(row => (
            row[nameof(MusicItemModel.FilePath)]?.ToString() ?? "",
            Convert.ToInt32(row[nameof(Position)])
        )).ToList();
    }

    #endregion
}
