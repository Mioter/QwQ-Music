using System;
using System.Collections.Generic;
using System.Linq;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Models;

namespace QwQ_Music.Common.Services.Databases;

public class MusicItemRepository : IDatabaseRepository<MusicItemModel>
{
    public const string TABLE_NAME = "MUSICS_ITEM";
    private readonly DatabaseService _db;

    public MusicItemRepository(string dbPath)
    {
        _db = new DatabaseService(dbPath);
        Initialize();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    public MusicItemModel? Get(string primaryKey)
    {
        var result = _db.Query(
            $"SELECT * FROM {TABLE_NAME} WHERE {nameof(MusicItemModel.FilePath)} = @primaryKey",
            new Dictionary<string, object>
            {
                ["primaryKey"] = primaryKey,
            }
        );

        return result.Count > 0 ? MapToModel(result[0]) : null;
    }

    public IEnumerable<MusicItemModel> GetAll()
    {
        var rows = _db.Query($"SELECT * FROM {TABLE_NAME}");

        return rows.Select(MapToModel);
    }

    public int Count()
    {
        var result = _db.Query($"SELECT COUNT(*) AS cnt FROM {TABLE_NAME}");

        return Convert.ToInt32(result[0]["cnt"]);
    }

    public void Insert(MusicItemModel item)
    {
        var data = ModelToDictionary(item);
        _db.Insert(TABLE_NAME, data);
    }

    public void Update(MusicItemModel item)
    {
        var data = ModelToDictionary(item);
        data.Remove(nameof(MusicItemModel.FilePath));

        const string whereClause = $"{nameof(MusicItemModel.FilePath)} = @{nameof(MusicItemModel.FilePath)}";

        _db.Update(TABLE_NAME, data, whereClause, new Dictionary<string, object?>
        {
            [nameof(MusicItemModel.FilePath)] = item.FilePath,
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

        const string whereClause = $"{nameof(MusicItemModel.FilePath)} = @primaryKey";

        _db.Update(TABLE_NAME, data, whereClause, new Dictionary<string, object?>
        {
            ["primaryKey"] = primaryKey,
        });
    }

    public void Update(string primaryKey, Dictionary<string, object?> fieldValues)
    {
        if (fieldValues.Count == 0)
            return;

        const string whereClause = $"{nameof(MusicItemModel.FilePath)} = @primaryKey";

        _db.Update(TABLE_NAME, fieldValues, whereClause, new Dictionary<string, object?>
        {
            ["primaryKey"] = primaryKey,
        });
    }

    public void Delete(string primaryKey)
    {
        const string whereClause = $"{nameof(MusicItemModel.FilePath)} = @primaryKey";

        _db.Delete(TABLE_NAME, whereClause, new Dictionary<string, object>
        {
            ["primaryKey"] = primaryKey,
        });
    }

    public bool Exists(string primaryKey)
    {
        var result = _db.Query(
            $"SELECT 1 FROM {TABLE_NAME} WHERE {nameof(MusicItemModel.FilePath)} = @primaryKey LIMIT 1",
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
