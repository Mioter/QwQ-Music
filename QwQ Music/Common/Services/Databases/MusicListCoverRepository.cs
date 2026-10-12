using Avalonia.Media.Imaging;
using Microsoft.Data.Sqlite;
using QwQ_Music.Common.Helpers;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Common.Services.Databases;

public class
    MusicListCoverRepository : IAsyncDatabaseRepository<(string Name, string Creator), MusicListModel, Bitmap?> {
    public const string TABLE_NAME = "music_list_cover";
    public static readonly MusicListCoverRepository Instance = new(StaticConfig.CachePath);

    private readonly AsyncDatabaseService _db;

    private MusicListCoverRepository(string path) {
        _db = new AsyncDatabaseService(path);
        InitializeAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() {
        await _db.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    public async Task<Bitmap?> SingleAsync((string Name, string Creator) key) {
        await LoggerService.DebugAsync($"正在获取歌单'{key.Name} - {key.Creator}'的封面").ConfigureAwait(false);
        Dictionary<string, object?>? result = await _db.SingleAsync(
                                                           $"SELECT * FROM {TABLE_NAME} WHERE {
                                                               nameof(MusicListModel.Name)} = @{
                                                                   nameof(MusicListModel.Name)} AND {
                                                                       nameof(MusicListModel.Creator)} = @{
                                                                           nameof(MusicListModel.Creator)}",
                                                           new Dictionary<string, object> {
                                                               [nameof(MusicListModel.Name)] = key.Name,
                                                               [nameof(MusicListModel.Creator)] = key.Creator
                                                           })
                                                       .ConfigureAwait(false);
        return ParseHelper.ParseToBitmap(result?[nameof(MusicListModel.Cover)]);
    }

    public async Task<IEnumerable<Bitmap?>> GetAsync(
        string? whereClause = null,
        Dictionary<string, object>? whereParams = null,
        int skip = 0,
        int limit = -1) {
        string sql = $"SELECT * FROM {TABLE_NAME} ";
        if (whereClause is not null)
            sql += $" WHERE {whereClause}";

        List<Dictionary<string, object?>> results =
            await _db.QueryAsync(sql, whereParams, skip, limit).ConfigureAwait(false);
        return results.Select(item => ParseHelper.ParseToBitmap(item[nameof(MusicListModel.Cover)]));
    }

    public async Task<int> CountAsync() {
        await LoggerService.DebugAsync("正在获取歌单封面数量").ConfigureAwait(false);
        return await _db.CountAsync(TABLE_NAME).ConfigureAwait(false);
    }

    public async Task InsertAsync(MusicListModel item, InsertExist onInsertExist = InsertExist.REPLACE) {
        await LoggerService.DebugAsync($"正在添加歌单'{item.Name} - {item.Creator}'的封面，模式{onInsertExist}")
                           .ConfigureAwait(false);

        Dictionary<string, object?> data = ToDictionary(item);
        object? thumbnail = data[nameof(MusicListModel.Thumbnail)];
        data.Remove(nameof(MusicListModel.Thumbnail));
        // SqliteTransaction transaction = _db.BeginTransaction();
        SqliteCommand coverCmd = _db.InsertNonExecute(null, TABLE_NAME, data, onInsertExist);
        data.Remove(nameof(MusicListModel.Cover));
        data[nameof(MusicListModel.Thumbnail)] = thumbnail;
        SqliteCommand thumbnailCmd = _db.InsertNonExecute(
            null,
            MusicListThumbnailRepository.TABLE_NAME,
            data,
            onInsertExist);
        await AsyncDatabaseService.ExecuteAsync(coverCmd, thumbnailCmd).ConfigureAwait(false);
    }

    public async Task UpdateAsync(MusicListModel item) {
        await UpdateAsync((item.Name, item.Creator), ToRawDictionary(item)).ConfigureAwait(false);
    }

    public async Task UpdateAsync((string Name, string Creator) key, Dictionary<string, object?> fieldValues) {
        if (string.IsNullOrWhiteSpace(key.Creator))
            throw new InvalidOperationException("There must have at least one creator for a music list.");
        await LoggerService.DebugAsync($"正在更新歌单'{key.Name} - {key.Creator}'的如下字段：{string.Join(',', fieldValues.Keys)}")
                           .ConfigureAwait(false);

        string whereClause = $"{nameof(MusicListModel.Name)} = @{nameof(MusicListModel.Name)} AND {
            nameof(MusicListModel.Creator)} = @{nameof(MusicListModel.Creator)}";
        var whereParams = new Dictionary<string, object?> {
            [nameof(MusicListModel.Name)] = key.Name, [nameof(MusicListModel.Creator)] = key.Creator
        };
        if (!fieldValues.Remove(nameof(MusicListModel.Thumbnail), out object? thumbnail)) {
            await _db.UpdateAsync(null, TABLE_NAME, fieldValues, whereClause, whereParams).ConfigureAwait(false);
            return;
        }

        // SqliteTransaction transaction = _db.BeginTransaction();
        SqliteCommand coverCmd = _db.UpdateNonExecute(null, TABLE_NAME, fieldValues, whereClause, whereParams);
        fieldValues.Remove(nameof(MusicListModel.Cover));
        fieldValues[nameof(MusicListModel.Thumbnail)] = thumbnail;
        SqliteCommand thumbnailCmd = _db.UpdateNonExecute(
            null,
            MusicListThumbnailRepository.TABLE_NAME,
            fieldValues,
            whereClause,
            whereParams);
        await AsyncDatabaseService.ExecuteAsync(coverCmd, thumbnailCmd).ConfigureAwait(false);
    }

    public async Task DeleteAsync((string Name, string Creator) key) {
        await LoggerService.DebugAsync($"正在删除歌单'{key.Name} - {key.Creator}'的封面").ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(key.Creator))
            throw new InvalidOperationException("There must have at least one creator for a music list.");

        await _db.DeleteAsync(
                     null,
                     TABLE_NAME,
                     $"{nameof(MusicListModel.Name)} = @{nameof(MusicListModel.Name)} AND {
                         nameof(MusicListModel.Creator)} = @{
                             nameof(MusicListModel.Creator)}",
                     new Dictionary<string, object> {
                         [nameof(MusicListModel.Name)] = key.Name, [nameof(MusicListModel.Creator)] = key.Creator
                     })
                 .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync((string Name, string Creator) key) {
        await LoggerService.DebugAsync($"正在检测歌单'{key.Name} - {key.Creator}'是否存在封面").ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(key.Creator))
            throw new InvalidOperationException("There must have at least one creator for a music list.");
        Dictionary<string, object?>? result = await _db.SingleAsync(
                                                           $"SELECT 1 FROM {TABLE_NAME} WHERE {
                                                               nameof(MusicListModel.Name)} = @{
                                                                   nameof(MusicListModel.Name)} AND {
                                                                       nameof(MusicListModel.Creator)} = @{
                                                                           nameof(MusicListModel.Creator)}",
                                                           new Dictionary<string, object> {
                                                               [nameof(MusicListModel.Name)] = key.Name,
                                                               [nameof(MusicListModel.Creator)] = key.Creator
                                                           })
                                                       .ConfigureAwait(false);
        return result is not null;
    }

    private async Task InitializeAsync() {
        await LoggerService.DebugAsync("正在初始化歌单封面数据库").ConfigureAwait(false);
        await _db.CreateTableAsync(
                     TABLE_NAME,
                     $"""
                        {nameof(MusicListModel.Name)} TEXT,
                        {nameof(MusicListModel.Creator)} TEXT, 
                        {nameof(MusicListModel.Cover)} BLOB,
                        PRIMARY KEY({nameof(MusicListModel.Name)},{nameof(MusicListModel.Creator)})
                      """)
                 .ConfigureAwait(false);

        // Cannot constraint foreign key from another schema
        // ,
        // FOREIGN KEY({nameof(MusicListModel.Name)},{nameof(MusicListModel.Creator)}) REFERENCES {
        //     MusicListRepository.TABLE_NAME}({nameof(MusicListModel.Name)},{
        //         nameof(MusicListModel.Creator)
        //     }) ON UPDATE CASCADE ON DELETE CASCADE
    }

    private static Dictionary<string, object?> ToDictionary(MusicListModel model) {
        return new Dictionary<string, object?> {
            [nameof(MusicListModel.Name)] = model.Name,
            [nameof(MusicListModel.Creator)] = model.Creator,
            [nameof(MusicListModel.Cover)] = DumpHelper.BitmapToBytes(model.Cover),
            [nameof(MusicListModel.Thumbnail)] = DumpHelper.BitmapToBytes(model.Thumbnail)
        };
    }

    private static Dictionary<string, object?> ToRawDictionary(MusicListModel model) {
        return new Dictionary<string, object?> {
            [nameof(MusicListModel.Name)] = model.Name,
            [nameof(MusicListModel.Creator)] = model.Creator,
            [nameof(MusicListModel.Cover)] = model.Cover
        };
    }
}

public class MusicListThumbnailRepository : IAsyncReadonlyDatabaseRepository<(string Name, string Creator), Bitmap?> {
    public const string TABLE_NAME = "music_list_thumbnail";
    public static readonly MusicListThumbnailRepository Instance;

    private readonly AsyncDatabaseService _db;

    static MusicListThumbnailRepository() { Instance = new MusicListThumbnailRepository(StaticConfig.CachePath); }

    private MusicListThumbnailRepository(string path) {
        _db = new AsyncDatabaseService(path);
        InitializeAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() {
        await _db.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    public async Task<Bitmap?> SingleAsync((string Name, string Creator) key) {
        await LoggerService.DebugAsync($"正在获取歌单'{key.Name} - {key.Creator}'的缩略图").ConfigureAwait(false);

        Dictionary<string, object?>? result = await _db.SingleAsync(
                                                           $"SELECT * FROM {TABLE_NAME} WHERE {
                                                               nameof(MusicListModel.Name)} = @{
                                                                   nameof(MusicListModel.Name)} AND {
                                                                       nameof(MusicListModel.Creator)} = @{
                                                                           nameof(MusicListModel.Creator)}",
                                                           new Dictionary<string, object> {
                                                               [nameof(MusicListModel.Name)] = key.Name,
                                                               [nameof(MusicListModel.Creator)] = key.Creator
                                                           })
                                                       .ConfigureAwait(false);
        return ParseHelper.ParseToBitmap(result?[nameof(MusicListModel.Thumbnail)]);
    }

    public async Task<IEnumerable<Bitmap?>> GetAsync(
        string? whereClause = null,
        Dictionary<string, object>? whereParams = null,
        int skip = 0,
        int limit = -1) {
        string sql = $"SELECT * FROM {TABLE_NAME} ";
        if (whereClause is not null)
            sql += $" WHERE {whereClause}";

        List<Dictionary<string, object?>> results =
            await _db.QueryAsync(sql, whereParams, skip, limit).ConfigureAwait(false);
        return results.Select(item => ParseHelper.ParseToBitmap(item[nameof(MusicListModel.Thumbnail)]));
    }

    public async Task<int> CountAsync() {
        await LoggerService.DebugAsync("正在获取歌单缩略图数量").ConfigureAwait(false);
        return await _db.CountAsync(TABLE_NAME).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync((string Name, string Creator) key) {
        if (string.IsNullOrWhiteSpace(key.Creator))
            throw new InvalidOperationException("There must have at least one creator for a music list.");

        await LoggerService.DebugAsync($"正在检测歌单'{key.Name} - {key.Creator}'是否存在缩略图").ConfigureAwait(false);
        Dictionary<string, object?>? result = await _db.SingleAsync(
                                                           $"SELECT 1 FROM {TABLE_NAME} WHERE {
                                                               nameof(MusicListModel.Name)} = @{
                                                                   nameof(MusicListModel.Name)} AND {
                                                                       nameof(MusicListModel.Creator)} = @{
                                                                           nameof(MusicListModel.Creator)}",
                                                           new Dictionary<string, object> {
                                                               [nameof(MusicListModel.Name)] = key.Name,
                                                               [nameof(MusicListModel.Creator)] = key.Creator
                                                           })
                                                       .ConfigureAwait(false);
        return result is not null;
    }

    private async Task InitializeAsync() {
        await LoggerService.DebugAsync("正在初始化歌单缩略图").ConfigureAwait(false);

        await _db.CreateTableAsync(
                     TABLE_NAME,
                     $"""
                        {nameof(MusicListModel.Name)} string,
                        {nameof(MusicListModel.Creator)} string, 
                        {nameof(MusicListModel.Thumbnail)} BLOB,
                        PRIMARY KEY({nameof(MusicListModel.Name)},{nameof(MusicListModel.Creator)})
                      """)
                 .ConfigureAwait(false);
        // Cannot constraint foreign key from another schema
        // ,
        // FOREIGN KEY({nameof(MusicListModel.Name)},{nameof(MusicListModel.Creator)}) REFERENCES {
        //     MusicListCoverRepository.TABLE_NAME}({nameof(MusicListModel.Name)},{
        //         nameof(MusicListModel.Creator)
        //     }) ON UPDATE CASCADE ON DELETE CASCADE
    }
}