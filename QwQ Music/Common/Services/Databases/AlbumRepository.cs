using QwQ_Music.Common.Helpers;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Common.Services.Databases;

public class AlbumRepository : IAsyncDatabaseRepository<(string Name, string Artists), AlbumModel, AlbumModel> {
    public const string TABLE_NAME = "albums";
    public static readonly AlbumRepository Instance = new(StaticConfig.DatabasePath);
    private readonly AsyncDatabaseService _db;

    protected AlbumRepository(string path) {
        _db = new AsyncDatabaseService(path);
        InitializeAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() {
        await _db.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    public async Task<AlbumModel?> SingleAsync((string Name, string Artists) key) {
        await LoggerService.DebugAsync($"正在获取专辑'{key.Name} - {key.Artists}'的信息。").ConfigureAwait(false);
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

        return result is null ? null : MapToModel(result);
    }

    public async Task<IEnumerable<AlbumModel>> GetAsync(
        string? whereClause = null,
        Dictionary<string, object>? whereParams = null,
        int skip = 0,
        int limit = -1) {
        string sql = $"SELECT * FROM {TABLE_NAME} ";
        if (whereClause is not null)
            sql += $" WHERE {whereClause} ";

        List<Dictionary<string, object?>> rows =
            await _db.QueryAsync(sql, whereParams, skip, limit).ConfigureAwait(false);
        return rows.Select(MapToModel).Where(x => x is not null).Cast<AlbumModel>();
    }

    public async Task<int> CountAsync() {
        await LoggerService.DebugAsync("正在获取专辑数量").ConfigureAwait(false);

        List<Dictionary<string, object?>> result =
            await _db.QueryAsync($"SELECT COUNT(*) AS cnt FROM {TABLE_NAME}").ConfigureAwait(false);

        return Convert.ToInt32(result[0]["cnt"]);
    }

    public async Task InsertAsync(AlbumModel item, InsertExist onInsertExist = InsertExist.FAIL) {
        Dictionary<string, object?> data = ModelToDictionary(item);
        await _db.InsertAsync(TABLE_NAME, data, onInsertExist).ConfigureAwait(false);
    }

    public async Task UpdateAsync(AlbumModel item) {
        Dictionary<string, object?> data = ModelToDictionary(item);

        // 移除主键字段，因为主键不应该被更新
        data.Remove(nameof(AlbumModel.Name));
        data.Remove(nameof(AlbumModel.Artists));
        await UpdateAsync((item.Name, item.Artists), data).ConfigureAwait(false);
    }


    public async Task UpdateAsync((string Name, string Artists) key, Dictionary<string, object?> fieldValues) {
        if (fieldValues.Count == 0)
            return;
        await LoggerService.DebugAsync($"正在更新专辑'{key.Name} - {key.Artists}'的如下字段：{string.Join(',', fieldValues.Keys)}")
                           .ConfigureAwait(false);

        const string whereClause = $"{nameof(AlbumModel.Name)} = @{nameof(AlbumModel.Name)} AND {
            nameof(AlbumModel.Artists)} = @{nameof(AlbumModel.Artists)}";

        await _db.UpdateAsync(
                     null,
                     TABLE_NAME,
                     fieldValues,
                     whereClause,
                     new Dictionary<string, object?> {
                         [nameof(AlbumModel.Name)] = key.Name, [nameof(AlbumModel.Artists)] = key.Artists
                     })
                 .ConfigureAwait(false);
    }

    public async Task DeleteAsync((string Name, string Artists) key) {
        await LoggerService.DebugAsync($"正在删除专辑'{key.Name} - {key.Artists}'").ConfigureAwait(false);

        const string whereClause = $"{nameof(AlbumModel.Name)} = @{nameof(AlbumModel.Name)} AND {
            nameof(AlbumModel.Artists)} = @{nameof(AlbumModel.Artists)}";

        await _db.DeleteAsync(
                     null,
                     TABLE_NAME,
                     whereClause,
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
        await LoggerService.DebugAsync("正在初始化专辑数据库").ConfigureAwait(false);

        await _db.CreateTableAsync(
                     TABLE_NAME,
                     $"""
                      {nameof(AlbumModel.Name)} TEXT NOT NULL,
                      {nameof(AlbumModel.Artists)} TEXT NOT NULL,
                      {nameof(AlbumModel.Description)} TEXT,
                      {nameof(AlbumModel.PublishTime)} INTEGER,
                      {nameof(AlbumModel.Company)} TEXT,
                      PRIMARY KEY({nameof(AlbumModel.Name)},{nameof(AlbumModel.Artists)})
                      """)
                 .ConfigureAwait(false);
    }

    // 添加或更新专辑项
    public async Task AddOrUpdateAlbumItemAsync(MusicItemModel musicItem) {
        if (!musicItem.HasCover)
            return;
        AlbumModel? album = await SingleAsync((musicItem.Album, musicItem.AlbumArtists)).ConfigureAwait(false);
        if (album is not null) {
            await UpdateAlbumProperties(album).ConfigureAwait(false);
            return;
        }

        album = new AlbumModel { Name = musicItem.Album, Artists = musicItem.Artists };
        await InsertAsync(album).ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    private async Task UpdateAlbumProperties(AlbumModel model) {
        bool isDescriptionExist = string.IsNullOrWhiteSpace(model.Description);
        bool isPublishTimeExist = model.PublishTime == null;
        bool isCompanyExist = string.IsNullOrWhiteSpace(model.Company);
        bool needUpdate = isDescriptionExist || isPublishTimeExist || isCompanyExist;
        if (!needUpdate)
            return;
        await model.UpdateAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    // 如果该音乐是某专辑的最后一首，则移除该专辑
    public async Task RemoveAlbumIfClearAsync(MusicItemModel? musicItem) {
        if (musicItem is null || !musicItem.HasCover)
            return;

        bool isLastItem = await _db.SingleAsync(
                                       $"SELECT 1 FROM TABLE {MusicItemRepository.TABLE_NAME} WHERE {
                                           nameof(MusicItemModel.AlbumId)} = @{
                                               nameof(MusicItemModel.AlbumId)}",
                                       new Dictionary<string, object> {
                                           [nameof(MusicItemModel.AlbumId)] = musicItem.AlbumId
                                       })
                                   .ConfigureAwait(false) is null;

        if (isLastItem)
            await DeleteAsync((musicItem.Album, musicItem.AlbumArtists)).ConfigureAwait(false);
    }

    #region Helper Methods

    private static AlbumModel? MapToModel(Dictionary<string, object?> dict) {
        try {
            long? publishTime = ParseHelper.TryParse<long>(dict, nameof(AlbumModel.PublishTime));
            var model = new AlbumModel {
                Name = ParseHelper.TryParse(dict, nameof(AlbumModel.Name), true),
                Artists = ParseHelper.TryParse(dict, nameof(AlbumModel.Artists), true),
                Description = ParseHelper.TryParse(dict, nameof(AlbumModel.Description)) ?? "",
                PublishTime = publishTime is null ? null : new DateTime(publishTime.Value)
            };
            LoggerService.Info($"加载了专辑'{model.Name}'。");

            return model;
        } catch (Exception ex) {
            NotificationService.Warning($"检测到严重的专辑存储错误。{dict[nameof(AlbumModel.Name)]}的数据将无法恢复。");
            LoggerService.Warning($"{dict[nameof(AlbumModel.Name)]}加载失败：{ex.Message}\n{ex.StackTrace}");
            return null;
        }
    }

    private static Dictionary<string, object?> ModelToDictionary(AlbumModel model) {
        return new Dictionary<string, object?> {
            [nameof(AlbumModel.Name)] = model.Name,
            [nameof(AlbumModel.Artists)] = model.Artists,
            [nameof(AlbumModel.Description)] = model.Description,
            [nameof(AlbumModel.PublishTime)] = model.PublishTime?.Ticks,
            [nameof(AlbumModel.Company)] = model.Company
        };
    }

    #endregion
}