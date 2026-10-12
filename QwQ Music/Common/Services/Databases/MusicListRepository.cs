using QwQ_Music.Common.Helpers;
using QwQ_Music.Common.Interfaces;
using QwQ_Music.Common.Managers;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Common.Services.Databases;

public class
    MusicListRepository : IAsyncDatabaseRepository<(string Name, string Creator), MusicListModel, MusicListModel> {
    public const string TABLE_NAME = "music_lists";
    public static readonly MusicListRepository Instance = new(StaticConfig.DatabasePath);
    private readonly AsyncDatabaseService _db;

    protected MusicListRepository(string path) {
        _db = new AsyncDatabaseService(path);
        InitializeAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() {
        await _db.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    public async Task<MusicListModel?> SingleAsync((string Name, string Creator) key) {
        await LoggerService.DebugAsync($"正在获取歌单'{key.Name} - {key.Creator}'").ConfigureAwait(false);
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

        return result is null ? null : MapToModel(result);
    }

    public async Task<IEnumerable<MusicListModel>> GetAsync(
        string? whereClause = null,
        Dictionary<string, object>? whereParams = null,
        int skip = 0,
        int limit = -1) {
        string sql = $"SELECT * FROM {TABLE_NAME} ";
        if (whereClause is not null)
            sql += $" WHERE {whereClause}";

        List<Dictionary<string, object?>> rows =
            await _db.QueryAsync(sql, whereParams, skip, limit).ConfigureAwait(false);
        return rows.Select(MapToModel).Where(x => x is not null).Cast<MusicListModel>();
    }

    public async Task<int> CountAsync() {
        await LoggerService.DebugAsync("正在获取歌单数量").ConfigureAwait(false);
        List<Dictionary<string, object?>> result =
            await _db.QueryAsync($"SELECT COUNT(*) AS cnt FROM {TABLE_NAME}").ConfigureAwait(false);

        return Convert.ToInt32(result[0]["cnt"]);
    }

    public async Task InsertAsync(MusicListModel item, InsertExist onInsertExist = InsertExist.FAIL) {
        await LoggerService.DebugAsync($"正在创建歌单'{item.Name} - {item.Creator}'，模式{onInsertExist}").ConfigureAwait(false);
        Dictionary<string, object?> data = ModelToDictionary(item);
        await _db.InsertAsync(TABLE_NAME, data, onInsertExist).ConfigureAwait(false);
        await MusicListItemsRepository.Instance.InitializeMusicListAsync((item.Name, item.Creator))
                                      .ConfigureAwait(false);
    }

    public async Task UpdateAsync(MusicListModel item) {
        Dictionary<string, object?> data = ModelToDictionary(item);

        // 移除主键字段，因为主键不应该被更新
        data.Remove(nameof(MusicListModel.Name));
        await UpdateAsync((item.Name, item.Creator), data).ConfigureAwait(false);
    }


    public async Task UpdateAsync((string Name, string Creator) key, Dictionary<string, object?> fieldValues) {
        if (fieldValues.Count == 0)
            return;
        await LoggerService.DebugAsync($"正在更新歌单'{key.Name} - {key.Creator}'的如下字段：{string.Join(',', fieldValues.Keys)}")
                           .ConfigureAwait(false);

        const string whereClause = $"{nameof(MusicListModel.Name)} = @{nameof(MusicListModel.Name)} AND {
            nameof(MusicListModel.Creator)} = @{nameof(MusicListModel.Creator)}";

        await _db.UpdateAsync(
                     null,
                     TABLE_NAME,
                     fieldValues,
                     whereClause,
                     new Dictionary<string, object?> {
                         [nameof(MusicListModel.Name)] = key.Name, [nameof(MusicListModel.Creator)] = key.Creator
                     })
                 .ConfigureAwait(false);
    }

    public async Task DeleteAsync((string Name, string Creator) key) {
        await LoggerService.DebugAsync($"正在删除歌单'{key.Name} - {key.Creator}'").ConfigureAwait(false);
        const string whereClause = $"{nameof(MusicListModel.Name)} = @{nameof(MusicListModel.Name)} AND {
            nameof(MusicListModel.Creator)} = @{nameof(MusicListModel.Creator)}";

        await _db.DeleteAsync(
                     null,
                     TABLE_NAME,
                     whereClause,
                     new Dictionary<string, object> {
                         [nameof(MusicListModel.Name)] = key.Name, [nameof(MusicListModel.Creator)] = key.Creator
                     })
                 .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync((string Name, string Creator) key) {
        await LoggerService.DebugAsync($"正在检测歌单'{key.Name} - {key.Creator}'是否存在").ConfigureAwait(false);
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
        await LoggerService.DebugAsync("正在初始化歌单数据库").ConfigureAwait(false);
        await _db.CreateTableAsync(
                     TABLE_NAME,
                     $"""
                      {nameof(MusicListModel.Name)} TEXT NOT NULL,
                      {nameof(MusicListModel.Creator)} TEXT NOT NULL,
                      {nameof(MusicListModel.Description)} TEXT,
                      {nameof(MusicListModel.IsCoverExist)} INTEGER,
                      {nameof(MusicListModel.CreateTime)} INTEGER,
                      {nameof(MusicListModel.ModifyTime)} INTEGER,
                      PRIMARY KEY({nameof(MusicListModel.Name)},{nameof(MusicListModel.Creator)})
                      """)
                 .ConfigureAwait(false);
    }

    #region Helper Methods

    private static MusicListModel? MapToModel(Dictionary<string, object?> dict) {
        try {
            var model = new MusicListModel {
                Name = ParseHelper.TryParse(dict, nameof(MusicListModel.Name), true),
                Creator = ParseHelper.TryParse(dict, nameof(MusicListModel.Creator), true),
                Description = ParseHelper.TryParse(dict, nameof(MusicListModel.Description), true),
                CreateTime =
                    new DateTime(
                        ParseHelper.TryParse<long>(dict, nameof(MusicListModel.CreateTime)) ?? DateTime.Now.Ticks),
                ModifyTime = new DateTime(
                    ParseHelper.TryParse<long>(dict, nameof(MusicListModel.ModifyTime)) ?? DateTime.Now.Ticks)
            };
            if (ParseHelper.TryParse<bool>(dict, nameof(MusicListModel.IsCoverExist)) is not true) {
                CacheManager.SetImage(model.Name, "歌单", CacheManager.NotExist);
                LoggerService.Warning($"歌单'{model.Name}'的封面不存在。");
            }

            LoggerService.Info($"加载了歌单'{model.Name}'。");

            return model;
        } catch (Exception ex) {
            NotificationService.Warning($"检测到严重的歌单存储错误。{dict[nameof(MusicListModel.Name)]}的数据将无法恢复。");
            LoggerService.Warning($"{dict[nameof(MusicListModel.Name)]}加载失败：{ex.Message}\n{ex.StackTrace}");

            return null;
        }
    }

    private static Dictionary<string, object?> ModelToDictionary(MusicListModel model) {
        return new Dictionary<string, object?> {
            [nameof(MusicListModel.Name)] = model.Name,
            [nameof(MusicListModel.Creator)] = model.Creator,
            [nameof(MusicListModel.Description)] = model.Description,
            [nameof(MusicListModel.IsCoverExist)] = model.IsCoverExist,
            [nameof(MusicListModel.CreateTime)] = model.CreateTime,
            [nameof(MusicListModel.ModifyTime)] = model.ModifyTime
        };
    }

    #endregion
}