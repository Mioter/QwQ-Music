using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using QwQ_Music.Common.Manager;
using QwQ_Music.Models;

namespace QwQ_Music.Common.Services;

public static class AudioFileManager
{
    /// <summary>
    ///     处理存储项目并导入音乐文件
    /// </summary>
    public static async Task ProcessStorageItemsAsync(IReadOnlyList<IStorageItem> items)
    {
        var paths = FileOperationService.ConvertStorageItemsToPathStrings(items);

        if (paths.Count == 0)
        {
            NotificationService.Info("提示", "获取的文件数量为 0 ！");

            return;
        }

        NotificationService.Info("提示", "开始导入中，请稍等....！");

        var allFilePaths = await Task.Run(() => FileOperationService.GetAllFilePaths(paths));
        await ImportMusicFilesAsync(allFilePaths);
    }

    /// <summary>
    ///     导入音乐文件到播放列表
    /// </summary>
    /// <param name="filePaths">要导入的文件路径列表</param>
    /// <returns>导入任务</returns>
    private static async Task ImportMusicFilesAsync(IReadOnlyList<string> filePaths)
    {
        // 过滤出音频文件
        var audioFilePaths = await Task.Run(() => AudioFileValidator.FilterAudioFiles(filePaths));

        if (audioFilePaths == null || audioFilePaths.Count == 0)
        {
            NotificationService.Info("提示", "没有找到可导入的音频文件！");

            return;
        }

        // 预加载现有路径集合
        var existingPaths = new HashSet<string?>(
            MusicItemManager.Default.MusicItems.Select(x => x.FilePath),
            StringComparer.OrdinalIgnoreCase
        );

        // 过滤掉已存在的路径
        var newFilePaths = audioFilePaths.Where(path => !existingPaths.Contains(path)).ToList();
        var existingFilePaths = audioFilePaths.Except(newFilePaths).ToList();

        // 如果有已存在的文件，显示提示
        if (existingFilePaths.Count > 0)
        {
            string existingTitles = string.Join("、",
                existingFilePaths.Select(path => $"《{Path.GetFileNameWithoutExtension(path)}》")
            );

            NotificationService.Info($"歌曲{existingTitles}已存在于音乐库中！");
        }

        if (newFilePaths.Count == 0)
            return;

        // 并行提取音乐信息，并过滤掉 null 结果
        var musicItems = (await Task.WhenAll(newFilePaths.Select(MusicExtractor.ExtractMusicInfoAsync)))
            .Where(m => m != null)
            .Cast<MusicItemModel>()
            .ToList();

        if (musicItems.Count == 0)
            return;

        // 批量添加到UI集合
        await MusicItemManager.Default.AddAsync(musicItems);
    }
}
