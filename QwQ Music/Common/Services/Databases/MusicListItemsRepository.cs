using System.Collections.Frozen;
using Microsoft.Data.Sqlite;
using QwQ_Music.Common.Helpers;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Common.Services.Databases;

public class MusicListItemsRepository : IAsyncDisposable {
    public const string NextPath = nameof(NextPath);
    public const string AddTime = nameof(AddTime);

    public const string TABLE_NAME = "music_list_items";
    public static readonly MusicListItemsRepository Instance = new(StaticConfig.DatabasePath);
    private readonly AsyncDatabaseService _db;

    public MusicListItemsRepository(string dbPath) {
        _db = new AsyncDatabaseService(dbPath);
        InitializeAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() {
        await _db.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private async Task InitializeAsync() {
        await LoggerService.DebugAsync("正在初始化歌单项数据库").ConfigureAwait(false);
        // 创建表（如果不存在）
        await _db.CreateTableAsync(
                     TABLE_NAME,
                     $"""
                      {nameof(MusicListModel.Name)} TEXT,
                      {nameof(MusicListModel.Creator)} TEXT,
                      {nameof(MusicItemModel.FilePath)} TEXT,
                      {nameof(AddTime)} INTEGER,
                      {nameof(NextPath)} TEXT,
                      PRIMARY KEY ({nameof(MusicListModel.Name)}, {nameof(MusicListModel.Creator)},{
                          nameof(MusicItemModel.FilePath)}),
                      FOREIGN KEY ({nameof(MusicListModel.Name)},{nameof(MusicListModel.Creator)}) REFERENCES {
                          MusicListRepository.TABLE_NAME}({nameof(MusicListModel.Name)},{nameof(MusicListModel.Creator)
                          }) ON UPDATE CASCADE ON DELETE CASCADE
                      """)
                 .ConfigureAwait(false);
        await _db.CreateTriggerAsync(
                     "save_removed_audio_info",
                     $"""
                      AFTER DELETE ON {MusicItemRepository.TABLE_NAME} FOR EACH ROW
                      BEGIN
                          UPDATE {TABLE_NAME} SET {nameof(MusicItemModel.FilePath)} = OLD.{nameof(MusicItemModel.Title)
                          } || '-' ||  OLD.{nameof(MusicItemModel.Artists)}
                          WHERE {nameof(MusicItemModel.FilePath)} = OLD.{nameof(MusicItemModel.FilePath)};
                      END;
                      """)
                 .ConfigureAwait(false);
        await _db.CreateTriggerAsync(
                     "update_audio_path",
                     $"""
                      AFTER UPDATE ON {MusicItemRepository.TABLE_NAME} FOR EACH ROW
                      BEGIN
                          UPDATE {TABLE_NAME} SET {nameof(MusicItemModel.FilePath)} = NEW.{
                              nameof(MusicItemModel.FilePath)}
                          WHERE {nameof(MusicItemModel.FilePath)} = OLD.{nameof(MusicItemModel.FilePath)};
                      END;
                      """)
                 .ConfigureAwait(false);
    }

    private async Task<string?> GetFirstAsync(string musicListName) {
        return (await _db.SingleAsync(
                             $"SELECT {NextPath} from {TABLE_NAME} " +
                             $"WHERE {nameof(MusicItemModel.FilePath)} = @{nameof(MusicListModel.Name)}",
                             new Dictionary<string, object> {
                                 [nameof(MusicListModel.Name)] = $"QWQ_PLAYLIST_{musicListName}_HEAD"
                             })
                         .ConfigureAwait(false))?["NextPath"] as string;
    }

    /// <summary>
    ///     添加歌曲到歌单
    /// </summary>
    public async Task InsertAsync(MusicListModel musicList, params ICollection<MusicItemModel> items) {
        await LoggerService.DebugAsync(
                               $"正在向歌单'{musicList.Name} - {musicList.Creator}'添加如下项目：{string.Join(
                                   ',',
                                   items.Select(item => $"《{item.Title} - {item.Artists}》"))}")
                           .ConfigureAwait(false);
        string musicListName = musicList.Name;

        string[] itemsArray = items.Select(item => item.FilePath)
                                   .Append((await GetFirstAsync(musicListName).ConfigureAwait(false))!)
                                   .ToArray();
        long time = DateTime.Now.Ticks;
        //  更新 HEAD，将首项更换为 items的首项。
        await EnsureChainHeader(musicList.Name, musicList.Creator).ConfigureAwait(false);
        // SqliteTransaction transaction = _db.BeginTransaction();
        List<SqliteCommand> cmd = [
            _db.UpdateNonExecute(
                null,
                TABLE_NAME,
                new Dictionary<string, object?> { [NextPath] = itemsArray.First() },
                $"{nameof(MusicListModel.Name)} = @{nameof(MusicListModel.Name)}",
                new Dictionary<string, object?> { [nameof(MusicListModel.Name)] = musicList.Name })
        ];
        IEnumerable<Dictionary<string, object?>> data = itemsArray.Take(..^1)
                                                                  .Select((item, index) =>
                                                                              new Dictionary<string, object?> {
                                                                                  [nameof(MusicListModel.Name)] =
                                                                                      musicListName,
                                                                                  [nameof(MusicItemModel.FilePath)] =
                                                                                      item,
                                                                                  [AddTime] = time,
                                                                                  [NextPath] = itemsArray[index + 1]
                                                                              });

        _db.InsertManyNonExecute(null, cmd, TABLE_NAME, data, InsertExist.REPLACE);
        await AsyncDatabaseService.ExecuteAsync(cmd).ConfigureAwait(false);
    }

    public async Task EnsureChainHeader(string name, string creator) {
        if (await ContainsAsync((name, creator), $"QWQ_PLAYLIST_{name}_{creator}_HEAD").ConfigureAwait(false))
            return;
        await InitializeMusicListAsync((name, creator)).ConfigureAwait(false);
    }

    /// <summary>
    ///     初始化歌单
    /// </summary>
    public async Task InitializeMusicListAsync((string Name, string Creator) key) {
        await LoggerService.DebugAsync($"正在初始化歌单'{key.Name} - {key.Creator}'").ConfigureAwait(false);
        await _db.InsertAsync(
                     TABLE_NAME,
                     new Dictionary<string, object?> {
                         [nameof(MusicListModel.Name)] = key.Name,
                         [nameof(MusicListModel.Creator)] = key.Creator,
                         [nameof(MusicItemModel.FilePath)] = $"QWQ_PLAYLIST_{key.Name}_{key.Creator}_HEAD"
                     },
                     InsertExist.IGNORE)
                 .ConfigureAwait(false);
    }


    /// <summary>
    ///     从歌单中删除指定歌曲
    /// </summary>
    public async Task RemoveAsync(MusicListModel musicList, params ICollection<MusicItemModel> items) {
        await LoggerService.DebugAsync(
                               $"正在从歌单'{musicList.Name} - {musicList.Creator}'删除如下项目：{string.Join(
                                   ',',
                                   items.Select(item => $"《{item.Title} - {item.Artists}》"))}")
                           .ConfigureAwait(false);
        // SqliteTransaction transaction = _db.BeginTransaction();
        List<SqliteCommand> commands = [];
        foreach (MusicItemModel item in items) {
            commands.Add(
                _db.UpdateNonExecute(
                    null,
                    TABLE_NAME,
                    new Dictionary<string, object?> { [NextPath] = null },
                    $"{nameof(MusicListModel.Name)} = @{nameof(MusicListModel.Name)} AND {NextPath} = @{NextPath}",
                    new Dictionary<string, object?> {
                        [nameof(MusicListModel.Name)] = musicList.Name, [NextPath] = item.FilePath
                    }));
            commands.Add(
                _db.DeleteNonExecute(
                    null,
                    TABLE_NAME,
                    $"{nameof(MusicListModel.Name)} = @{nameof(MusicListModel.Name)} AND {
                        nameof(MusicItemModel.FilePath)} = @{nameof(MusicItemModel.FilePath)}",
                    new Dictionary<string, object> {
                        [nameof(MusicListModel.Name)] = musicList.Name,
                        [nameof(MusicItemModel.FilePath)] = item.FilePath
                    }));
        }

        await AsyncDatabaseService.ExecuteAsync(commands).ConfigureAwait(false);
    }

    /// <summary>
    ///     清空整个歌单
    /// </summary>
    public async Task ClearAsync((string Name, string Creator) key) {
        await LoggerService.DebugAsync($"正在清空歌单'{key.Name} - {key.Creator}'").ConfigureAwait(false);
        var whereParams = new Dictionary<string, object> {
            [nameof(MusicListModel.Name)] = key.Name, [nameof(MusicListModel.Creator)] = key.Creator
        };

        await _db.DeleteAsync(
                     null,
                     TABLE_NAME,
                     $"{nameof(MusicListModel.Name)} = @{nameof(MusicListModel.Name)}",
                     whereParams)
                 .ConfigureAwait(false);
        await InitializeMusicListAsync(key).ConfigureAwait(false);
    }

    /// <summary>
    ///     获取歌单中所有歌曲
    /// </summary>
    /// <returns>文件路径列表</returns>
    public async Task<(int Count, IEnumerable<string> Paths)> GetAllAsync((string Name, string Creator) key) {
        await LoggerService.DebugAsync($"正在获取歌单'{key.Name} - {key.Creator}'的所有项目。警告：可能发生长时磁盘IO").ConfigureAwait(false);
        await EnsureChainHeader(key.Name, key.Creator).ConfigureAwait(false);
        const string sql = $"SELECT {nameof(MusicItemModel.FilePath)} FROM {TABLE_NAME} WHERE {
            nameof(MusicListModel.Name)} = @{nameof(MusicListModel.Name)} AND {nameof(MusicListModel.Creator)} = @{
                nameof(MusicListModel.Creator)}";

        FrozenDictionary<string, string?> result =
            (await _db.QueryAsync(
                          sql,
                          new Dictionary<string, object> {
                              [nameof(MusicListModel.Name)] = key.Name, [nameof(MusicListModel.Creator)] = key.Creator
                          })
                      .ConfigureAwait(false)).ToFrozenDictionary(
                item => ParseHelper.TryParse(item, nameof(MusicItemModel.FilePath))!,
                item => ParseHelper.TryParse(item, nameof(NextPath)));

        return (result.Count, Paths: Order(result));

        IEnumerable<string> Order(IDictionary<string, string?> raw) {
            if (!raw.TryGetValue($"QWQ_PLAYLIST_{key.Name}_{key.Creator}_HEAD", out string? current)) {
                LoggerService.Error("不存在头节点，无法生成音频项目链。请检查歌单是否存在。数据库可能损坏");
                yield break;
            }

            while (current != null) {
                yield return current;
                if (raw.TryGetValue(current, out current))
                    continue;
                LoggerService.Warning("音频项目链已断裂，在该处截止。");
                yield break;
            }
        }
    }

    /// <summary>
    ///     检查某首歌是否在歌单中
    /// </summary>
    public async ValueTask<bool> ContainsAsync((string Name, string Creator) key, string filePath) {
        await LoggerService.DebugAsync($"正在获取歌单'{key.Name} - {key.Creator}'的封面").ConfigureAwait(false);
        await LoggerService.DebugAsync($"正在检测歌单'{key.Name} - {key.Creator}'是否包含歌曲{filePath}").ConfigureAwait(false);

        const string sql = $"SELECT 1 FROM {TABLE_NAME} " +
                           $"WHERE {nameof(MusicListModel.Name)} = @{nameof(MusicListModel.Name)} " +
                           $"AND {nameof(MusicListModel.Creator)} = @{nameof(MusicListModel.Creator)} " +
                           $"AND {nameof(MusicItemModel.FilePath)} = @{nameof(MusicItemModel.FilePath)} ";

        var parameters = new Dictionary<string, object> {
            [nameof(MusicItemModel.FilePath)] = filePath,
            [nameof(MusicListModel.Name)] = key.Name,
            [nameof(MusicListModel.Creator)] = key.Creator
        };

        return await _db.SingleAsync(sql, parameters).ConfigureAwait(false) is not null;
    }
}