using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.ViewModels;
using QwQ_Music.ViewModels.Pages;
using QwQ.Avalonia.Utilities.MessageBus;

namespace QwQ_Music.Services;

public static class MusicDataPersistence
{
    /// <summary>
    /// 批量保存音乐项到数据库
    /// </summary>
    /// <param name="items">要保存的音乐项集合</param>
    /// <returns>保存成功的音乐项列表</returns>
    private static async Task<List<MusicItemModel>> SaveMusicItemsFromDataBaseAsync(IEnumerable<MusicItemModel> items)
    {
        var successItems = new List<MusicItemModel>();
        var itemsList = items.ToList();

        // 批量检查记录是否存在
        var checkTasks = itemsList.Select(item =>
            DataBaseService.RecordExistsAsync(
                DataBaseService.Table.MUSICS,
                nameof(MusicItemModel.FilePath),
                item.FilePath
            )
        );

        bool[] existsResults = await Task.WhenAll(checkTasks).ConfigureAwait(false);
        var itemExistsMap = itemsList.Zip(existsResults, (item, exists) => (item, exists)).ToList();

        // 批量处理更新和插入
        var saveTasks = itemExistsMap.Select(async pair =>
        {
            (var item, bool exists) = pair;
            var data = item.Dump();
            string filePath = item.FilePath;

            bool isSuccess;
            if (exists)
            {
                isSuccess =
                    await DataBaseService
                        .UpdateDataAsync(data, DataBaseService.Table.MUSICS, nameof(MusicItemModel.FilePath), filePath)
                        .ConfigureAwait(false) != DataBaseService.OperationResult.Failure;
            }
            else
            {
                isSuccess =
                    await DataBaseService.InsertDataAsync(data, DataBaseService.Table.MUSICS).ConfigureAwait(false)
                    != DataBaseService.OperationResult.Failure;
            }

            if (isSuccess)
            {
                lock (successItems)
                {
                    successItems.Add(item);
                }
            }
        });

        await Task.WhenAll(saveTasks).ConfigureAwait(false);
        return successItems;
    }

    public static async Task SaveMusicItemsAsync(
        IEnumerable<MusicItemModel> items,
        bool isEnableSuccessPrompt = true,
        bool isEnableFailedPrompt = true
    )
    {
        var itemsList = items.ToList(); // 只枚举一次
        var successItems = await SaveMusicItemsFromDataBaseAsync(itemsList);

        // 使用HashSet提高查找效率
        var successSet = new HashSet<MusicItemModel>(successItems);
        var failedItems = itemsList.Where(item => !successSet.Contains(item)).ToList();

        await MessageBus
            .CreateMessage(new OperateCompletedMessage(nameof(MusicPlayerViewModel.MusicItems)))
            .AddReceivers(typeof(PlayConfigPageViewModel), typeof(AlbumClassPageViewModel))
            .SetAsOneTime()
            .PublishAsync();

        // 显示保存结果通知
        if (successItems.Count > 0 && isEnableSuccessPrompt)
        {
            string successTitles = string.Join("、", successItems.Select(item => $"《{item.Title}》"));
            NotificationService.ShowLight("好欸", $"保存{successTitles}成功了！", NotificationType.Success);
        }

        if (failedItems.Count > 0 && isEnableFailedPrompt)
        {
            string failedTitles = string.Join("、", failedItems.Select(item => $"《{item.Title}》"));
            NotificationService.ShowLight("坏欸", $"保存{failedTitles}失败了！", NotificationType.Error);
        }
    }

    /// <summary>
    /// 保存播放列表到数据库
    /// </summary>
    public static async Task SaveMusicListAsync(MusicListModel musicList)
    {
        // 批量获取和更新数据
        var existingPaths = await DataBaseService
            .LoadSpecifyFieldsAsync(
                DataBaseService.Table.MUSICLISTS,
                [nameof(MusicItemModel.FilePath)],
                dict => dict.TryGetValue(nameof(MusicItemModel.FilePath), out object? path) ? path.ToString() : null,
                search: $"{nameof(MusicListModel.Name)} = '{musicList.Name.Replace("'", "''")}'"
            )
            .ConfigureAwait(false);

        if (existingPaths == null)
        {
            NotificationService.ShowLight("错误", "获取播放列表播放路径失败！", NotificationType.Error);
            return;
        }

        var existingPathsSet = new HashSet<string?>(existingPaths);
        var currentPaths = new HashSet<string?>(musicList.MusicItems.Select(item => item.FilePath));

        // 批量处理插入和删除操作
        var insertTasks = musicList
            .MusicItems.Where(item => !existingPathsSet.Contains(item.FilePath))
            .Select(item =>
            {
                var playlistData = new Dictionary<string, string?>
                {
                    [nameof(MusicListModel.Name)] = musicList.Name,
                    [nameof(MusicItemModel.FilePath)] = item.FilePath,
                };
                return DataBaseService.InsertDataAsync(playlistData, DataBaseService.Table.MUSICLISTS);
            });

        var deleteTasks = existingPathsSet
            .OfType<string>()
            .Where(path => !currentPaths.Contains(path))
            .Select(path =>
                DataBaseService.DeleteDataAsync(DataBaseService.Table.MUSICLISTS, nameof(MusicItemModel.FilePath), path)
            );

        var allTasks = insertTasks.Concat(deleteTasks).ToList();

        // 添加列表名称更新任务
        var listNameData = musicList.Dump();
        bool exists = await DataBaseService
            .RecordExistsAsync(DataBaseService.Table.LISTINFO, nameof(MusicListModel.Name), musicList.Name)
            .ConfigureAwait(false);

        if (exists)
        {
            allTasks.Add(
                DataBaseService.UpdateDataAsync(
                    listNameData,
                    DataBaseService.Table.LISTINFO,
                    nameof(MusicListModel.Name),
                    musicList.Name
                )
            );
        }
        else
        {
            allTasks.Add(DataBaseService.InsertDataAsync(listNameData, DataBaseService.Table.LISTINFO));
        }

        await Task.WhenAll(allTasks).ConfigureAwait(false);
    }
}
