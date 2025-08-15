using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Models;

namespace QwQ_Music.Common.Services.Databases;

public class MusicItemRepository(string dbPath) : IDatabaseRepository<MusicItemModel>
{
    public const string TABLE_NAME = "MUSICS_ITEM";
    private readonly DatabaseService _db = new(dbPath);
    private bool _initialized;

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public async Task<MusicItemModel?> GetAsync(string primaryKey)
    {
        await InitializeAsync();

        var result = await _db.QueryAsync(
            $"SELECT * FROM {TABLE_NAME} WHERE {nameof(MusicItemModel.FilePath)} = @primaryKey",
            new Dictionary<string, object>
            {
                ["primaryKey"] = primaryKey,
            }
        );

        return result.Count > 0 ? MapToModel(result[0]) : null;
    }

    public async Task<IEnumerable<MusicItemModel>> GetAllAsync()
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

    public async Task InsertAsync(MusicItemModel item)
    {
        await InitializeAsync();

        var data = ModelToDictionary(item);
        await _db.InsertAsync(TABLE_NAME, data);
    }

    public async Task UpdateAsync(MusicItemModel item)
    {
        await InitializeAsync();

        var data = ModelToDictionary(item);
        data.Remove(nameof(MusicItemModel.FilePath));

        const string whereClause = $"{nameof(MusicItemModel.FilePath)} = @{nameof(MusicItemModel.FilePath)}";

        await _db.UpdateAsync(TABLE_NAME, data, whereClause, new Dictionary<string, object?>
        {
            [nameof(MusicItemModel.FilePath)] = item.FilePath,
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

        const string whereClause = $"{nameof(MusicItemModel.FilePath)} = @primaryKey";

        await _db.UpdateAsync(TABLE_NAME, data, whereClause, new Dictionary<string, object?>
        {
            ["primaryKey"] = primaryKey,
        });
    }

    public async Task UpdateAsync(string primaryKey, Dictionary<string, object?> fieldValues)
    {
        await InitializeAsync();

        if (fieldValues.Count == 0)
            return;

        const string whereClause = $"{nameof(MusicItemModel.FilePath)} = @primaryKey";

        await _db.UpdateAsync(TABLE_NAME, fieldValues, whereClause, new Dictionary<string, object?>
        {
            ["primaryKey"] = primaryKey,
        });
    }

    public async Task DeleteAsync(string primaryKey)
    {
        await InitializeAsync();

        const string whereClause = $"{nameof(MusicItemModel.FilePath)} = @primaryKey";

        await _db.DeleteAsync(TABLE_NAME, whereClause, new Dictionary<string, object>
        {
            ["primaryKey"] = primaryKey,
        });
    }

    public async Task<bool> ExistsAsync(string primaryKey)
    {
        await InitializeAsync();

        var result = await _db.QueryAsync(
            $"SELECT 1 FROM {TABLE_NAME} WHERE {nameof(MusicItemModel.FilePath)} = @primaryKey LIMIT 1",
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
             {nameof(MusicItemModel.Title)} TEXT NOT NULL,
             {nameof(MusicItemModel.Artists)} TEXT,
             {nameof(MusicItemModel.Composer)} TEXT,
             {nameof(MusicItemModel.Album)} TEXT,
             {nameof(MusicItemModel.CoverId)} TEXT,
             {nameof(MusicItemModel.FilePath)} TEXT NOT NULL UNIQUE PRIMARY KEY,
             {nameof(MusicItemModel.FileSize)} TEXT NOT NULL,
             {nameof(MusicItemModel.Current)} BLOB,
             {nameof(MusicItemModel.Duration)} BLOB NOT NULL,
             {nameof(MusicItemModel.CoverColors)} TEXT,
             {nameof(MusicItemModel.Gain)} INTEGER,
             {nameof(MusicItemModel.EncodingFormat)} TEXT NOT NULL,
             {nameof(MusicItemModel.Comment)} TEXT,
             {nameof(MusicItemModel.Remarks)} TEXT,
             {nameof(MusicItemModel.LyricOffset)} INTEGER
             """);

        _initialized = true;
    }

    public async IAsyncEnumerable<MusicItemModel> GetEnumerableAllAsync()
    {
        await InitializeAsync();

        await foreach (var row in _db.QueryAsyncEnumerable($"SELECT * FROM {TABLE_NAME}"))
        {
            yield return MapToModel(row);
        }
    }

    #region Helper Methods

    private static MusicItemModel MapToModel(Dictionary<string, object?> dict)
    {
        var model = new MusicItemModel
        {
            FilePath = dict[nameof(MusicItemModel.FilePath)]?.ToString() ?? throw new InvalidOperationException("音乐项路径为空！"),
        };

        if (dict.TryGetValue(nameof(MusicItemModel.Title), out object? title) && title?.ToString() is { } titleStr)
            model.Title = titleStr;

        if (dict.TryGetValue(nameof(MusicItemModel.Artists), out object? artists) && artists?.ToString() is { } artistsStr)
            model.Artists = artistsStr;

        if (dict.TryGetValue(nameof(MusicItemModel.Album), out object? album) && album?.ToString() is { } albumStr)
            model.Album = albumStr;

        if (dict.TryGetValue(nameof(MusicItemModel.Composer), out object? composer))
            model.Composer = composer?.ToString();

        if (dict.TryGetValue(nameof(MusicItemModel.CoverId), out object? coverFileName))
            model.CoverId = coverFileName?.ToString();

        if (dict.TryGetValue(nameof(MusicItemModel.FileSize), out object? fileSize))
            model.FileSize = fileSize?.ToString();

        if (dict.TryGetValue(nameof(MusicItemModel.Current), out object? current) && current?.ToString() is { } currentStr)
            model.Current = TimeSpan.Parse(currentStr);

        if (dict.TryGetValue(nameof(MusicItemModel.Duration), out object? duration) && duration?.ToString() is { } durationStr)
            model.Duration = TimeSpan.Parse(durationStr);

        if (dict.TryGetValue(nameof(MusicItemModel.CoverColors), out object? coverColors))
            model.CoverColors = coverColors?.ToString()?.Split("、");

        if (dict.TryGetValue(nameof(MusicItemModel.Gain), out object? gain))
            model.Gain = Convert.ToInt32(gain);

        if (dict.TryGetValue(nameof(MusicItemModel.EncodingFormat), out object? encodingFormat))
            model.EncodingFormat = encodingFormat?.ToString();

        if (dict.TryGetValue(nameof(MusicItemModel.Comment), out object? comment))
            model.Comment = comment?.ToString();

        if (dict.TryGetValue(nameof(MusicItemModel.Remarks), out object? remarks))
            model.Remarks = remarks?.ToString();

        if (dict.TryGetValue(nameof(MusicItemModel.LyricOffset), out object? lyricOffset))
            model.LyricOffset = Convert.ToInt32(lyricOffset);

        return model;
    }

    private static Dictionary<string, object?> ModelToDictionary(MusicItemModel model)
    {
        var dict = new Dictionary<string, object?>
        {
            [nameof(MusicItemModel.Title)] = model.Title,
            [nameof(MusicItemModel.Artists)] = model.Artists,
            [nameof(MusicItemModel.Composer)] = model.Composer,
            [nameof(MusicItemModel.Album)] = model.Album,
            [nameof(MusicItemModel.CoverId)] = model.CoverId,
            [nameof(MusicItemModel.FilePath)] = model.FilePath,
            [nameof(MusicItemModel.FileSize)] = model.FileSize,
            [nameof(MusicItemModel.Current)] = model.Current.ToString(),
            [nameof(MusicItemModel.Duration)] = model.Duration.ToString(),
            [nameof(MusicItemModel.CoverColors)] = model.CoverColors != null ? string.Join("、", model.CoverColors) : null,
            [nameof(MusicItemModel.Gain)] = model.Gain,
            [nameof(MusicItemModel.EncodingFormat)] = model.EncodingFormat,
            [nameof(MusicItemModel.Comment)] = model.Comment,
            [nameof(MusicItemModel.Remarks)] = model.Remarks,
            [nameof(MusicItemModel.LyricOffset)] = model.LyricOffset,
        };

        return dict;
    }

    #endregion
}
