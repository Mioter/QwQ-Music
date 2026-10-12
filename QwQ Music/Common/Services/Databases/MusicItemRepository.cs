using System.Runtime.CompilerServices;
using QwQ_Music.Common.Helpers;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Models.Enums;

namespace QwQ_Music.Common.Services.Databases;

public class MusicItemRepository : IAsyncDatabaseRepository<MusicItemModel> {
    public const string TABLE_NAME = "music";
    public static readonly MusicItemRepository Instance = new(StaticConfig.DatabasePath);
    private readonly AsyncDatabaseService _db;

    private MusicItemRepository(string dbPath) {
        _db = new AsyncDatabaseService(dbPath);
        InitializeAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() {
        await _db.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    public async Task<MusicItemModel?> SingleAsync(string id) {
        await LoggerService.DebugAsync($"正在获取音频《{id}》的标签").ConfigureAwait(false);

        Dictionary<string, object?>? result = await _db.SingleAsync(
                                                           $"SELECT * FROM {TABLE_NAME} WHERE {
                                                               nameof(MusicItemModel.FilePath)} = @primaryKey",
                                                           new Dictionary<string, object> { ["primaryKey"] = id })
                                                       .ConfigureAwait(false);

        return result is null ? null : Parse(result);
    }


    public async Task<IEnumerable<MusicItemModel>> GetAsync(
        string? whereClause = null,
        Dictionary<string, object>? whereParams = null,
        int skip = 0,
        int limit = -1) {
        string sql = $"SELECT * FROM {TABLE_NAME} ";
        if (whereClause is not null)
            sql += $" WHERE {whereClause}";

        return (await _db.QueryAsync(sql, whereParams, skip, limit).ConfigureAwait(false)).AsParallel()
            .Select(Parse)
            .Where(item => item is not null)!;
    }

    public async Task<int> CountAsync() {
        await LoggerService.DebugAsync("正在获取音频数量").ConfigureAwait(false);
        return await _db.CountAsync(TABLE_NAME).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task InsertAsync(MusicItemModel item, InsertExist onInsertExist = InsertExist.FAIL) {
        await _db.InsertAsync(TABLE_NAME, ToDictionary(item), onInsertExist).ConfigureAwait(false);
        await AlbumRepository.Instance.AddOrUpdateAlbumItemAsync(item).ConfigureAwait(false);
    }

    public async Task UpdateAsync(MusicItemModel item) {
        Dictionary<string, object?> data = ToDictionary(item);
        data.Remove(nameof(MusicItemModel.FilePath));
        await UpdateAsync(item.FilePath, data).ConfigureAwait(false);
    }

    public async Task UpdateAsync(string name, Dictionary<string, object?> fieldValues) {
        if (fieldValues.Count == 0)
            return;
        await LoggerService.DebugAsync($"正在更新音频《{name}》的如下字段：{string.Join(',', fieldValues.Keys)}")
                           .ConfigureAwait(false);

        const string whereClause = $"{nameof(MusicItemModel.FilePath)} = @primaryKey";

        await _db.UpdateAsync(
                     null,
                     TABLE_NAME,
                     fieldValues,
                     whereClause,
                     new Dictionary<string, object?> { ["primaryKey"] = name })
                 .ConfigureAwait(false);
    }

    public async Task DeleteAsync(string key) {
        const string whereClause = $"{nameof(MusicItemModel.FilePath)} = @{nameof(MusicItemModel.FilePath)}";

        await _db.DeleteAsync(
                     null,
                     TABLE_NAME,
                     whereClause,
                     new Dictionary<string, object> { [nameof(MusicItemModel.FilePath)] = key })
                 .ConfigureAwait(false);
        await AlbumRepository.Instance.RemoveAlbumIfClearAsync(await SingleAsync(key).ConfigureAwait(false))
                             .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string id) {
        await LoggerService.DebugAsync($"正在检测音频《{id}》是否存在").ConfigureAwait(false);

        Dictionary<string, object?>? result = await _db.SingleAsync(
                                                           $"SELECT 1 FROM {TABLE_NAME} WHERE {
                                                               nameof(MusicItemModel.FilePath)} = @{
                                                                   nameof(MusicItemModel.FilePath)}",
                                                           new Dictionary<string, object> {
                                                               [nameof(MusicItemModel.FilePath)] = id
                                                           })
                                                       .ConfigureAwait(false);

        return result is not null;
    }

    private async Task InitializeAsync() {
        await _db.CreateTableAsync(
                     TABLE_NAME,
                     $"""
                      {nameof(MusicItemModel.Title)} TEXT NOT NULL,
                      {nameof(MusicItemModel.Artists)} TEXT NOT NULL,
                      {nameof(MusicItemModel.Composer)} TEXT,
                      {nameof(MusicItemModel.Album)} TEXT,
                      {nameof(MusicItemModel.AlbumArtists)} TEXT,
                      {nameof(MusicItemModel.AlbumId)} TEXT,
                      {nameof(MusicItemModel.FilePath)} TEXT NOT NULL PRIMARY KEY,
                      {nameof(MusicItemModel.FileSize)} TEXT NOT NULL,
                      {nameof(MusicItemModel.Record)} INTEGER NOT NULL,
                      {nameof(MusicItemModel.Duration)} INTEGER NOT NULL,
                      {nameof(MusicItemModel.CoverColors)} TEXT,
                      {nameof(MusicItemModel.Gain)} TEXT NOT NULL,
                      {nameof(MusicItemModel.SampleRate)} TEXT NOT NULL,
                      {nameof(MusicItemModel.Channels)} INTEGER NOT NULL,
                      {nameof(MusicItemModel.EncodingFormat)} TEXT NOT NULL,
                      {nameof(MusicItemModel.Comment)} TEXT,
                      {nameof(MusicItemModel.AudioQualityLevel)} INTEGER,
                      {nameof(MusicItemModel.Remarks)} TEXT,
                      {nameof(MusicItemModel.LyricOffset)} TEXT,
                      {nameof(MusicItemModel.InsertTime)} INTEGER,
                      {nameof(MusicItemModel.ModificationTime)} INTEGER
                      """)
                 .ConfigureAwait(false);
    }


    public async Task RebuildAsync() {
        await LoggerService.DebugAsync("正在重建音频数据库。").ConfigureAwait(false);
        await _db.DropTableAsync(TABLE_NAME).ConfigureAwait(false);
        await InitializeAsync().ConfigureAwait(false);
    }

    #region Helper Methods

    private static MusicItemModel? Parse(Dictionary<string, object?> dict) {
        int errors = 0;
        bool critical = false;
        try {
            var model = new MusicItemModel {
                FilePath = ParseHelper.TryParse(dict, nameof(MusicItemModel.FilePath)) ?? CriticalError(""),
                Title = ParseHelper.TryParse(dict, nameof(MusicItemModel.Title)) ?? Error(""),
                Artists = ParseHelper.TryParse(dict, nameof(MusicItemModel.Artists)) ?? Error(""),
                Album = ParseHelper.TryParse(dict, nameof(MusicItemModel.Album)) ?? Error(""),
                AlbumArtists = ParseHelper.TryParse(dict, nameof(MusicItemModel.AlbumArtists)) ?? Error(""),
                Composer = ParseHelper.TryParse(dict, nameof(MusicItemModel.Composer)),
                AlbumId = ParseHelper.TryParseTuple(dict, nameof(MusicItemModel.AlbumId), '\u001F') ?? Error(("", "")),
                FileSize = ParseHelper.TryParse(dict, nameof(MusicItemModel.FileSize)) ?? Error("未知"),
                Record =
                    TimeSpan.FromTicks(ParseHelper.TryParse<long>(dict, nameof(MusicItemModel.Record)) ?? Error(0)),
                Duration =
                    TimeSpan.FromTicks(ParseHelper.TryParse<long>(dict, nameof(MusicItemModel.Duration)) ?? Error(0)),
                SampleRate = ParseHelper.TryParse<int>(dict, nameof(MusicItemModel.SampleRate)) ?? Error(44100),
                Channels = ParseHelper.TryParse<int>(dict, nameof(MusicItemModel.Channels)) ?? Error(2),
                CoverColors = ParseHelper.TryParse(dict, nameof(MusicItemModel.CoverColors))?.Split("、"),
                Gain = ParseHelper.TryParse<double>(dict, nameof(MusicItemModel.Gain)) ?? Error(0),
                EncodingFormat = ParseHelper.TryParse(dict, nameof(MusicItemModel.EncodingFormat)) ?? Error("Unknown"),
                Comment = ParseHelper.TryParse(dict, nameof(MusicItemModel.Comment)) ?? Error(""),
                AudioQualityLevel =
                    (AudioQualityLevel?)ParseHelper.TryParse<int>(dict, nameof(MusicItemModel.AudioQualityLevel)) ??
                    Error(AudioQualityLevel.Unknown),
                Remarks = ParseHelper.TryParse(dict, nameof(MusicItemModel.Remarks)) ?? Error(""),
                LyricOffset = ParseHelper.TryParse<double>(dict, nameof(MusicItemModel.LyricOffset)) ?? Error(0),
                InsertTime =
                    new DateTime(ParseHelper.TryParse<long>(dict, nameof(MusicItemModel.InsertTime)) ?? Error(0)),
                ModificationTime = new DateTime(
                    ParseHelper.TryParse<long>(dict, nameof(MusicItemModel.ModificationTime)) ??
                    Error(DateTime.Now.Ticks))
            };

            if (critical) {
                NotificationService.Warning($"检测到《{model.Title}》严重的音频存储错误。音频信息将无法恢复。");
                return null;
            }

            if (errors > 0)
                NotificationService.Warning($"检测到对{model.Title}的信息存储存在{errors}处错误。请在设置>播放中点击[强制刷新标签]按钮以修复。");

            return model;
        } catch (Exception) {
            NotificationService.Warning("检测到严重的音频存储错误。可能是由于大版本更新导致的数据缺失。请在设置>播放中点击[强制刷新标签]按钮以尝试修复。");
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T Error<T>(T value) {
            errors++;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        T CriticalError<T>(T value) {
            critical = true;
            return value;
        }
    }

    private static Dictionary<string, object?> ToDictionary(MusicItemModel model) {
        var dict = new Dictionary<string, object?> {
            [nameof(MusicItemModel.Title)] = model.Title,
            [nameof(MusicItemModel.Artists)] = model.Artists,
            [nameof(MusicItemModel.Composer)] = model.Composer,
            [nameof(MusicItemModel.Album)] = model.Album,
            [nameof(MusicItemModel.AlbumArtists)] = model.AlbumArtists,
            [nameof(MusicItemModel.AlbumId)] = $"{model.AlbumId.Name}\u001F{model.AlbumId.Artists} ",
            [nameof(MusicItemModel.FilePath)] = model.FilePath,
            [nameof(MusicItemModel.FileSize)] = model.FileSize,
            [nameof(MusicItemModel.Record)] = model.Record.Ticks,
            [nameof(MusicItemModel.Duration)] = model.Duration.Ticks,
            [nameof(MusicItemModel.CoverColors)] =
                model.CoverColors != null ? string.Join("、", model.CoverColors) : null,
            [nameof(MusicItemModel.Gain)] = model.Gain,
            [nameof(MusicItemModel.SampleRate)] = model.SampleRate,
            [nameof(MusicItemModel.Channels)] = model.Channels,
            [nameof(MusicItemModel.EncodingFormat)] = model.EncodingFormat,
            [nameof(MusicItemModel.Comment)] = model.Comment,
            [nameof(MusicItemModel.AudioQualityLevel)] = (int)model.AudioQualityLevel,
            [nameof(MusicItemModel.Remarks)] = model.Remarks,
            [nameof(MusicItemModel.LyricOffset)] = model.LyricOffset,
            [nameof(MusicItemModel.InsertTime)] = model.InsertTime.Ticks,
            [nameof(MusicItemModel.ModificationTime)] = model.ModificationTime.Ticks
        };

        return dict;
    }

    #endregion
}