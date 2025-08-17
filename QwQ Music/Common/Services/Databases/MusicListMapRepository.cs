using System;
using System.Collections.Generic;
using System.Linq;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Models;

namespace QwQ_Music.Common.Services.Databases;

public class MusicListMapRepository : IDatabaseRepository<MusicListModel>
{
    public const string TABLE_NAME = "MUSIC_LIST_MAP";
    private readonly DatabaseService _db;

    public MusicListMapRepository(string path)
    {
        _db = new DatabaseService(path);
        Initialize();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    public MusicListModel? Get(string primaryKey)
    {
        var result = _db.Query(
            $"SELECT * FROM {TABLE_NAME} WHERE {nameof(MusicListModel.IdStr)} = @primaryKey",
            new Dictionary<string, object>
            {
                ["primaryKey"] = primaryKey,
            }
        );

        return result.Count > 0 ? MapToModel(result[0]) : null;
    }

    public IEnumerable<MusicListModel> GetAll()
    {
        var rows = _db.Query($"SELECT * FROM {TABLE_NAME}");

        return rows.Select(MapToModel);
    }

    public int Count()
    {
        var result = _db.Query($"SELECT COUNT(*) AS cnt FROM {TABLE_NAME}");

        return Convert.ToInt32(result[0]["cnt"]);
    }

    public void Insert(MusicListModel item)
    {
        var data = ModelToDictionary(item);
        _db.Insert(TABLE_NAME, data);
    }

    public void Update(MusicListModel item)
    {
        var data = ModelToDictionary(item);

        // 移除主键字段，因为主键不应该被更新
        data.Remove(nameof(MusicListModel.IdStr));

        const string whereClause = $"{nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)}";

        _db.Update(TABLE_NAME, data, whereClause,
            new Dictionary<string, object?>
            {
                [nameof(MusicListModel.IdStr)] = item.IdStr,
            });
    }

    public void Update(string primaryKey, string[] fields, string?[] values)
    {
        if (fields.Length != values.Length)
            throw new ArgumentException("字段和值的长度必须相同。");

        var data = new Dictionary<string, object?>();

        for (int i = 0; i < fields.Length; i++)
        {
            data[fields[i]] = values[i];
        }

        const string whereClause = $"{nameof(MusicListModel.IdStr)} = @primaryKey";

        _db.Update(TABLE_NAME, data, whereClause,
            new Dictionary<string, object?>
            {
                ["primaryKey"] = primaryKey,
            });
    }

    public void Update(string primaryKey, Dictionary<string, object?> fieldValues)
    {
        if (fieldValues.Count == 0)
            return;

        const string whereClause = $"{nameof(MusicListModel.IdStr)} = @primaryKey";

        _db.Update(TABLE_NAME, fieldValues, whereClause,
            new Dictionary<string, object?>
            {
                ["primaryKey"] = primaryKey,
            });
    }

    public void Delete(string primaryKey)
    {
        const string whereClause = $"{nameof(MusicListModel.IdStr)} = @primaryKey";

        _db.Delete(TABLE_NAME, whereClause,
            new Dictionary<string, object>
            {
                ["primaryKey"] = primaryKey,
            });
    }

    public bool Exists(string primaryKey)
    {
        var result = _db.Query(
            $"SELECT 1 FROM {TABLE_NAME} WHERE {nameof(MusicListModel.IdStr)} = @primaryKey LIMIT 1",
            new Dictionary<string, object>
            {
                ["primaryKey"] = primaryKey,
            }
        );

        return result.Count > 0;
    }

    private void Initialize()
    {
        _db.CreateTable(TABLE_NAME,
            $"""
             {nameof(MusicListModel.IdStr)} TEXT NOT NULL PRIMARY KEY,
             {nameof(MusicListModel.Name)} TEXT,
             {nameof(MusicListModel.Description)} TEXT,
             {nameof(MusicListModel.CoverId)} TEXT
             """);
    }

    public IEnumerable<MusicListModel> GetEnumerableAll()
    {
        var rows = _db.Query($"SELECT * FROM {TABLE_NAME}");

        return rows.Select(MapToModel);
    }

    #region Helper Methods

    private static MusicListModel MapToModel(Dictionary<string, object?> dict)
    {
        var model = new MusicListModel
        {
            IdStr = dict[nameof(MusicListModel.IdStr)]?.ToString() ?? throw new InvalidOperationException("歌单Id为空!"),
        };

        if (dict.TryGetValue(nameof(MusicListModel.Name), out object? name) && name?.ToString() is { } nameStr)
            model.Name = nameStr;

        if (dict.TryGetValue(nameof(MusicListModel.Description), out object? description) && description?.ToString() is { } descriptionStr)
            model.Description = descriptionStr;

        if (dict.TryGetValue(nameof(MusicListModel.CoverId), out object? coverFileName))
            model.CoverId = coverFileName?.ToString();

        return model;
    }

    private static Dictionary<string, object?> ModelToDictionary(MusicListModel model)
    {
        var dict = new Dictionary<string, object?>
        {
            [nameof(MusicListModel.IdStr)] = model.IdStr,
            [nameof(MusicListModel.Name)] = model.Name,
            [nameof(MusicListModel.Description)] = model.Description,
            [nameof(MusicListModel.CoverId)] = model.CoverId,
        };

        return dict;
    }

    #endregion
}
