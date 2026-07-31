using Avalonia.Platform.Storage;
using Avalonia.Threading;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Dialogs;
using QwQ_Music.Views.Dialogs;
using Ursa.Controls;
using MusicItemsManager = QwQ_Music.Common.Managers.MusicItemsManager;

namespace QwQ_Music.Common.Services;

public static class AudioFileService {
    public static async Task FileOpenAsync(string[] paths) {
        IEnumerable<string> musicPaths = AudioFileValidator.FilterAudioFiles(
            paths.Select(path => (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory ?
                             Directory.EnumerateFiles(path) : [path])
                 .SelectMany(item => item));
        List<MusicItemModel> items = [];
        List<MusicItemModel> newItems = [];
        foreach (string t in musicPaths) {
            MusicItemModel item = await MusicItemRepository.Instance.SingleAsync(t).ConfigureAwait(false) ??
                                  new MusicItemModel { FilePath = t, IsTemporary = true };
            items.Add(item);
            if (!item.IsTemporary)
                continue;
            if (items.Count == 100) {
                NotificationService.Info("您提供的文件较多，解析需要一定时间，请稍候...");
            }

            newItems.Add(item);
            _ = item.UpdateMetaDataAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
        }


        await AudioPlayManager.Instance.PlaylistManager
                              .ReplaceAsync(I18NService.Lang.Translation["FILE IMPORT"], items, 0, true, true)
                              .ConfigureAwait(false);
        if (newItems.Count == 0) {
            NotificationService.Info($"提供的{(items.Count == 1 ? "文件" : $"{items.Count}个文件均")}已导入过了。");
            return;
        }

        if (!await await Dispatcher.UIThread.InvokeAsync(() => OverlayDialog
                                                               .ShowCustomAsync<ImportConfirm, ImportConfirmViewModel,
                                                                   bool>(
                                                                   new ImportConfirmViewModel(),
                                                                   options: new OverlayDialogOptions {
                                                                       Mode = DialogMode.Question,
                                                                       IsCloseButtonVisible = false
                                                                   })
                                                               .ConfigureAwait(true)))

            return;
        await LoggerService.DebugAsync($"正在导入：{string.Join(",", newItems)}").ConfigureAwait(false);
        NotificationService.Info("正在导入文件，请稍候");
        newItems.AsParallel().ForAll(item => item.IsTemporary = false);
        await MusicItemsManager.All.AddAsync(newItems.ToAsyncEnumerable()).ConfigureAwait(false);
    }


    /// <summary>
    ///     处理存储项目并导入音乐文件
    /// </summary>
    public static async Task ProcessStorageItemsAsync(IReadOnlyList<IStorageItem> items) {
        List<string> paths = FileOperationService.ConvertStorageItemsToPathStrings(items);

        if (paths.Count == 0) {
            NotificationService.Info("提示", "获取的文件数量为 0 ！");

            return;
        }

        NotificationService.Info("提示", "开始导入中，请稍等....！");

        List<string> allFilePaths = FileOperationService.GetAllFilePaths(paths);
        IAsyncEnumerable<MusicItemModel> musicItems = ImportMusicFilesAsync(allFilePaths);

        await MusicItemsManager.All.AddAsync(musicItems).ConfigureAwait(false);
    }

    /// <summary>
    ///     导入音乐文件
    /// </summary>
    /// <param name="filePaths">要导入的文件路径列表</param>
    /// <returns>导入的音乐文件信息</returns>
    private static async IAsyncEnumerable<MusicItemModel>
        ImportMusicFilesAsync(params IReadOnlyList<string> filePaths) {
        // 过滤出音频文件
        string[] audioFilePaths = [.. AudioFileValidator.FilterAudioFiles(filePaths)];

        if (audioFilePaths.Length == 0) {
            NotificationService.Info("提示", "没有找到可导入的音频文件！");

            yield break;
        }

        // 过滤掉已存在的路径
        List<string> existingFilePaths =
            audioFilePaths.Where(path => MusicItemsManager.All.MusicItems.ContainsKey(path)).ToList();
        IEnumerable<string> newFilePaths = audioFilePaths.Except(existingFilePaths);

        // 如果有已存在的文件，显示提示
        if (existingFilePaths.Count > 0) {
            string existingTitles = string.Join(
                "、",
                existingFilePaths.Select(path => $"《{Path.GetFileNameWithoutExtension(path)}》"));

            NotificationService.Info($"歌曲{existingTitles}已存在于音乐库中！");
        }

        foreach (MusicItemModel model in newFilePaths.Select(path => new MusicItemModel { FilePath = path })) {
            await model.UpdateMetaDataAsync().ConfigureAwait(false);
            yield return model;
        }

        if (filePaths.Count - existingFilePaths.Count is var succeed and > 0)
            NotificationService.Info($"成功导入{succeed}个文件");
    }
}