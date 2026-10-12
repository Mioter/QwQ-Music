using Avalonia.Media.Imaging;
using Microsoft.Data.Sqlite;
using QwQ_Music.Common.Helpers;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Common.Managers;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Common.Services.Databases;

public class AlbumCoverRepository : IAsyncDatabaseRepository<(string Name, string Artists), MusicItemModel, Bitmap?> {
    public const string TABLE_NAME = "album_cover";
    public static readonly AlbumCoverRepository Instance = new(StaticConfig.CachePath);
    private readonly AsyncDatabaseService _db;

    private AlbumCoverRepository(string dbPath) {
        _db = new AsyncDatabaseService(dbPath);
        InitializeAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() {
        await _db.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    public async Task<Bitmap?> SingleAsync((string Name, string Artists) key) {
        await LoggerService.DebugAsync($"正在获取专辑'{key.Name} - {key.Artists}'的封面").ConfigureAwait(false);

        Dictionary<string, object?>? result = await _db.SingleAsync(
                                                           $"SELECT * FROM {TABLE_NAME} WHERE {nameof(AlbumModel.Name)
                                                           } = @{nameof(AlbumModel.Name)} AND {
                                                               nameof(AlbumModel.Artists)} = @{
                                                                   nameof(AlbumModel.Artists)}",
                                                           new Dictionary<string, object> {
                                                               [nameof(AlbumModel.Name)] = key.Name,
                                                               [nameof(AlbumModel.Artists)] = key.Artists
                                                           })
                                                       .ConfigureAwait(false);
        return ParseHelper.ParseToBitmap(result?[nameof(AlbumModel.Cover)]);
    }

    public async Task<IEnumerable<Bitmap?>> GetAsync(
        string? whereClause = null,
        Dictionary<string, object>? whereParams = null,
        int skip = 0,
        int limit = -1) {
        string sql = $"SELECT * FROM {TABLE_NAME} ";
        if (whereClause is not null)
            sql += $" WHERE {whereClause}";

        List<Dictionary<string, object?>> result =
            await _db.QueryAsync(sql, whereParams, skip, limit).ConfigureAwait(false);
        return result.Select(item => ParseHelper.ParseToBitmap(item[nameof(AlbumModel.Cover)]) ?? CacheManager.Damaged);
    }

    public async Task<int> CountAsync() { return await _db.CountAsync(TABLE_NAME).ConfigureAwait(false); }

    public async Task InsertAsync(MusicItemModel item, InsertExist onInsertExist = InsertExist.REPLACE) {
        await InsertAsync(item, null, onInsertExist).ConfigureAwait(false);
    }


    public async Task UpdateAsync(MusicItemModel item) {
        Dictionary<string, object?>? value = ToDictionary(item);
        if (value is null)
            return;
        string name = ParseHelper.TryParse(value, nameof(AlbumModel.Name), true);
        string artists = ParseHelper.TryParse(value, nameof(AlbumModel.Artists), true);
        await UpdateAsync(
                (name, artists),
                new Dictionary<string, object?> { [nameof(AlbumModel.Cover)] = value[nameof(AlbumModel.Cover)] })
            .ConfigureAwait(false);
    }


    public async Task UpdateAsync((string Name, string Artists) key, Dictionary<string, object?> fieldValues) {
        await LoggerService.DebugAsync($"正在更新专辑'{key.Name} - {key.Artists}'的如下字段：{string.Join(',', fieldValues.Keys)}。")
                           .ConfigureAwait(false);
        if (fieldValues.Count == 0)
            return;
        string whereClause = $"{nameof(AlbumModel.Name)} = @{nameof(AlbumModel.Name)} AND {nameof(AlbumModel.Artists)
        } = @{nameof(AlbumModel.Artists)}";
        var whereParams = new Dictionary<string, object?> {
            [nameof(AlbumModel.Name)] = key.Name, [nameof(AlbumModel.Artists)] = key.Artists
        };

        if (!fieldValues.Remove(nameof(AlbumModel.Thumbnail), out object? thumbnail)) {
            await _db.UpdateAsync(null, TABLE_NAME, fieldValues, whereClause, whereParams).ConfigureAwait(false);
            return;
        }

        // SqliteTransaction transaction = _db.BeginTransaction();
        SqliteCommand coverCmd = _db.UpdateNonExecute(null, TABLE_NAME, fieldValues, whereClause, whereParams);
        fieldValues.Remove(nameof(AlbumModel.Cover));
        fieldValues[nameof(AlbumModel.Thumbnail)] = thumbnail;
        SqliteCommand thumbnailCmd = _db.UpdateNonExecute(
            null,
            AlbumThumbnailRepository.TABLE_NAME,
            fieldValues,
            whereClause,
            whereParams);
        await AsyncDatabaseService.ExecuteAsync(coverCmd, thumbnailCmd).ConfigureAwait(false);
    }

    public async Task DeleteAsync((string Name, string Artists) key) {
        await LoggerService.DebugAsync($"正在删除专辑{key.Name} - {key.Artists}的封面。").ConfigureAwait(false);
        await _db.DeleteAsync(
                     null,
                     TABLE_NAME,
                     $"{nameof(AlbumModel.Name)} = @{nameof(AlbumModel.Name)} AND {nameof(AlbumModel.Artists)} = @{
                         nameof(AlbumModel.Artists)}",
                     new Dictionary<string, object> {
                         [nameof(AlbumModel.Name)] = key.Name, [nameof(AlbumModel.Artists)] = key.Artists
                     })
                 .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync((string Name, string Artists) key) {
        await LoggerService.DebugAsync($"正在检测是否存在专辑'{key.Name} - {key.Artists}'").ConfigureAwait(false);
        Dictionary<string, object?>? result = await _db.SingleAsync(
                                                           $"SELECT 1 FROM {TABLE_NAME} WHERE {nameof(AlbumModel.Name)
                                                           } = @{nameof(AlbumModel.Name)} AND {
                                                               nameof(AlbumModel.Artists)} = @{
                                                                   nameof(AlbumModel.Artists)}",
                                                           new Dictionary<string, object> {
                                                               [nameof(AlbumModel.Name)] = key.Name,
                                                               [nameof(AlbumModel.Artists)] = key.Artists
                                                           })
                                                       .ConfigureAwait(false);
        return result is not null;
    }

    private async Task InitializeAsync() {
        await LoggerService.DebugAsync("正在初始化专辑封面仓库").ConfigureAwait(false);

        await _db.CreateTableAsync(
                     TABLE_NAME,
                     $"""
                      {nameof(AlbumModel.Name)} TEXT,
                      {nameof(AlbumModel.Artists)} TEXT,
                      {nameof(AlbumModel.Cover)} BLOB,
                      PRIMARY KEY({nameof(AlbumModel.Name)},{nameof(AlbumModel.Artists)})
                      """)
                 .ConfigureAwait(false);

        // Cannot constraint foreign key from another schema
        //,
        // FOREIGN KEY ({nameof(AlbumModel.Name)},{nameof(AlbumModel.Artists)}) REFERENCES {
        //                           AlbumRepository.TABLE_NAME
        //                       }({nameof(AlbumModel.Name)},{nameof(AlbumModel.Artists)}) ON DELETE CASCADE
    }

    public async Task RebuildAsync() {
        await LoggerService.DebugAsync("正在重建专辑封面仓库").ConfigureAwait(false);

        await _db.DropTableAsync(TABLE_NAME).ConfigureAwait(false);
        await InitializeAsync().ConfigureAwait(false);
    }

    public async Task InsertAsync(MusicItemModel item, byte[]? image, InsertExist onInsertExist = InsertExist.REPLACE) {
        Dictionary<string, object?>? value = ToDictionary(item, image);
        if (value is null)
            return;
        await LoggerService.DebugAsync(
                               $"正在更新专辑'{value[nameof(AlbumModel.Name)]} - {value[nameof(AlbumModel.Artists)]}'的如下字段：{
                                   string.Join(',', value.Keys)}。")
                           .ConfigureAwait(false);
        if (!value.Remove(nameof(AlbumModel.Thumbnail), out object? thumbnail)) {
            await _db.InsertAsync(TABLE_NAME, value, onInsertExist).ConfigureAwait(false);
            return;
        }

        // SqliteTransaction transaction = _db.BeginTransaction();
        SqliteCommand coverCmd = _db.InsertNonExecute(null, TABLE_NAME, value, onInsertExist);
        value.Remove(nameof(AlbumModel.Cover));
        value[nameof(AlbumModel.Thumbnail)] = thumbnail;
        SqliteCommand thumbnailCmd = _db.InsertNonExecute(
            null,
            AlbumThumbnailRepository.TABLE_NAME,
            value,
            onInsertExist);
        await AsyncDatabaseService.ExecuteAsync(coverCmd, thumbnailCmd).ConfigureAwait(false);
    }

    private static Dictionary<string, object?>? ToDictionary(MusicItemModel model, Bitmap? bitmap = null) {
        Bitmap cover = bitmap ?? model.Cover;
        if (!CacheManager.IsValid(cover))
            return null;
        return new Dictionary<string, object?> {
            [nameof(AlbumModel.Name)] = string.IsNullOrEmpty(model.Album) ? model.AlbumId : model.Album,
            [nameof(AlbumModel.Artists)] = model.AlbumArtists,
            [nameof(AlbumModel.Cover)] = DumpHelper.BitmapToBytes(cover),
            [nameof(AlbumModel.Thumbnail)] = DumpHelper.BitmapToBytes(model.Thumbnail)
        };
    }

    private static Dictionary<string, object?>? ToDictionary(MusicItemModel model, byte[]? image) {
        if (!model.HasCover)
            return null;
        return new Dictionary<string, object?> {
            [nameof(AlbumModel.Name)] = model.AlbumId.Name,
            [nameof(AlbumModel.Artists)] = model.AlbumId.Artists,
            [nameof(AlbumModel.Cover)] = image ?? DumpHelper.BitmapToBytes(model.Cover),
            [nameof(AlbumModel.Thumbnail)] = DumpHelper.BitmapToBytes(model.Thumbnail)
        };
    }
}

public class AlbumThumbnailRepository : IAsyncReadonlyDatabaseRepository<(string Name, string Artists), Bitmap?> {
    public const string TABLE_NAME = "album_thumbnail";
    public static readonly AlbumThumbnailRepository Instance = new(StaticConfig.CachePath);
    private readonly AsyncDatabaseService _db;


    private AlbumThumbnailRepository(string dbPath) {
        _db = new AsyncDatabaseService(dbPath);
        InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync() {
        await _db.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    public async Task<Bitmap?> SingleAsync((string Name, string Artists) key) {
        await LoggerService.DebugAsync($"正在获取专辑'{key.Name} - {key.Artists}'的缩略图").ConfigureAwait(false);
        Dictionary<string, object?>? result = await _db.SingleAsync(
                                                           $"SELECT * FROM {TABLE_NAME} WHERE {nameof(AlbumModel.Name)
                                                           } = @{nameof(AlbumModel.Name)} AND {
                                                               nameof(AlbumModel.Artists)} = @{
                                                                   nameof(AlbumModel.Artists)}",
                                                           new Dictionary<string, object> {
                                                               [nameof(AlbumModel.Name)] = key.Name,
                                                               [nameof(AlbumModel.Artists)] = key.Artists
                                                           })
                                                       .ConfigureAwait(false);
        return ParseHelper.ParseToBitmap(result?[nameof(AlbumModel.Thumbnail)]);
    }

    public async Task<IEnumerable<Bitmap?>> GetAsync(
        string? whereClause = null,
        Dictionary<string, object>? whereParams = null,
        int skip = 0,
        int limit = -1) {
        string sql = $"SELECT * FROM {TABLE_NAME} ";
        if (whereClause is not null)
            sql += $" WHERE {whereClause}";

        List<Dictionary<string, object?>> result =
            await _db.QueryAsync(sql, whereParams, skip, limit).ConfigureAwait(false);
        return result.Select(item => ParseHelper.ParseToBitmap(item[nameof(AlbumModel.Thumbnail)]) ??
                                     CacheManager.Damaged);
    }

    public async Task<int> CountAsync() {
        await LoggerService.DebugAsync("正在计算专辑缩略图数量").ConfigureAwait(false);
        return await _db.CountAsync(TABLE_NAME).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync((string Name, string Artists) key) {
        await LoggerService.DebugAsync($"正在检测是否存在专辑{key.Name} - {key.Artists}的缩略图").ConfigureAwait(false);

        Dictionary<string, object?>? result = await _db.SingleAsync(
                                                           $"SELECT 1 FROM {TABLE_NAME} WHERE {nameof(AlbumModel.Name)
                                                           } = @{nameof(AlbumModel.Name)} AND {
                                                               nameof(AlbumModel.Artists)} = @{
                                                                   nameof(AlbumModel.Artists)}",
                                                           new Dictionary<string, object> {
                                                               [nameof(AlbumModel.Name)] = key.Name,
                                                               [nameof(AlbumModel.Artists)] = key.Artists
                                                           })
                                                       .ConfigureAwait(false);
        return result is not null;
    }

    private async Task InitializeAsync() {
        await LoggerService.DebugAsync("正在初始化专辑缩略图数据库。").ConfigureAwait(false);
        await _db.CreateTableAsync(
                     TABLE_NAME,
                     $"""
                      {nameof(AlbumModel.Name)} TEXT,
                      {nameof(AlbumModel.Artists)} TEXT,
                      {nameof(AlbumModel.Thumbnail)} BLOB,
                      PRIMARY KEY({nameof(AlbumModel.Name)},{nameof(AlbumModel.Artists)})
                      """)
                 .ConfigureAwait(false);
        // Cannot constraint foreign key from another schema
        //     ,
        // FOREIGN KEY ({nameof(AlbumModel.Name)},{nameof(AlbumModel.Artists)}) REFERENCES {
        //     AlbumRepository.TABLE_NAME
        // }({nameof(AlbumModel.Name)},{nameof(AlbumModel.Artists)}) ON DELETE CASCADE
    }

    public async Task RebuildAsync() {
        await LoggerService.DebugAsync("正在重建专辑缩略图数据库。").ConfigureAwait(false);
        await _db.DropTableAsync(TABLE_NAME).ConfigureAwait(false);
        await InitializeAsync().ConfigureAwait(false);
    }
}