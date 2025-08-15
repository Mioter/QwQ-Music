using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Models;

namespace QwQ_Music.Common.Services.Databases;

public class MusicListMapRepository(string path) : IDatabaseRepository<MusicListModel>
{
    public const string TABLE_NAME = "MUSIC_LIST_MAP";
    private readonly DatabaseService _db = new(path);
    private bool _initialized;

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public async Task<MusicListModel?> GetAsync(string primaryKey)
    {
        await InitializeAsync();

        var result = await _db.QueryAsync(
            $"SELECT * FROM {TABLE_NAME} WHERE {nameof(MusicListModel.IdStr)} = @primaryKey",
            new Dictionary<string, object>
            {
                ["primaryKey"] = primaryKey,
            }
        );

        return result.Count > 0 ? MapToModel(result[0]) : null;
    }

    public async Task<IEnumerable<MusicListModel>> GetAllAsync()
    {
        await InitializeAsync();

        var rows = await _db.QueryAsync($"SELECT * FROM {TABLE_NAME}");

        return rows.Select(MapToModel);
    }

    public async Task<int> CountAsync()
    {
        await InitializeAsync();

        var result = await _db.QueryAsync($"SELECT COUNT(*) AS cnt FROM {TABLE_NAME}");

        return Convert.ToInt32(result[0]["cnt"]);
    }

    public async Task InsertAsync(MusicListModel item)
    {
        await InitializeAsync();

        var data = ModelToDictionary(item);
        await _db.InsertAsync(TABLE_NAME, data);
    }

    public async Task UpdateAsync(MusicListModel item)
    {
        await InitializeAsync();

        var data = ModelToDictionary(item);

        // 移除主键字段，因为主键不应该被更新
        data.Remove(nameof(MusicListModel.IdStr));

        const string whereClause = $"{nameof(MusicListModel.IdStr)} = @{nameof(MusicListModel.IdStr)}";

        await _db.UpdateAsync(TABLE_NAME, data, whereClause,
            new Dictionary<string, object?>
            {
                [nameof(MusicListModel.IdStr)] = item.IdStr,
            });
    }

    public async Task UpdateAsync(string primaryKey, string[] fields, string?[] values)
    {
        await InitializeAsync();

        if (fields.Length != values.Length)
            throw new ArgumentException("字段和值的长度必须相同。");

        var data = new Dictionary<string, object?>();

        for (int i = 0; i < fields.Length; i++)
        {
            data[fields[i]] = values[i];
        }

        const string whereClause = $"{nameof(MusicListModel.IdStr)} = @primaryKey";

        await _db.UpdateAsync(TABLE_NAME, data, whereClause,
            new Dictionary<string, object?>
            {
                ["primaryKey"] = primaryKey,
            });
    }

    public async Task UpdateAsync(string primaryKey, Dictionary<string, object?> fieldValues)
    {
        await InitializeAsync();

        if (fieldValues.Count == 0)
            return;

        const string whereClause = $"{nameof(MusicListModel.IdStr)} = @primaryKey";

        await _db.UpdateAsync(TABLE_NAME, fieldValues, whereClause,
            new Dictionary<string, object?>
            {
                ["primaryKey"] = primaryKey,
            });
    }

    public async Task DeleteAsync(string primaryKey)
    {
        await InitializeAsync();

        const string whereClause = $"{nameof(MusicListModel.IdStr)} = @primaryKey";

        await _db.DeleteAsync(TABLE_NAME, whereClause,
            new Dictionary<string, object>
            {
                ["primaryKey"] = primaryKey,
            });
    }

    public async Task<bool> ExistsAsync(string primaryKey)
    {
        await InitializeAsync();

        var result = await _db.QueryAsync(
            $"SELECT 1 FROM {TABLE_NAME} WHERE {nameof(MusicListModel.IdStr)} = @primaryKey LIMIT 1",
            new Dictionary<string, object>
            {
                ["primaryKey"] = primaryKey,
            }
        );

        return result.Count > 0;
    }

    private async Task InitializeAsync()
    {
        if (_initialized) return;

        await _db.InitializeAsync();

        await _db.CreateTableAsync(TABLE_NAME,
            $"""
             {nameof(MusicListModel.IdStr)} TEXT NOT NULL PRIMARY KEY,
             {nameof(MusicListModel.Name)} TEXT,
             {nameof(MusicListModel.Description)} TEXT,
             {nameof(MusicListModel.CoverId)} TEXT
             """);

        _initialized = true;
    }

    public async IAsyncEnumerable<MusicListModel> GetEnumerableAllAsync()
    {
        await InitializeAsync();

        await foreach (var row in _db.QueryAsyncEnumerable($"SELECT * FROM {TABLE_NAME}"))
        {
            yield return MapToModel(row);
        }
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
