using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Services;

public static class AudioFileManager
{
    /// <summary>
    /// 处理存储项目并导入音乐文件
    /// </summary>
    public static async Task ProcessStorageItemsAsync(IReadOnlyList<IStorageItem> items)
    {
        var paths = FileOperation.ConvertStorageItemsToPathStrings(items);
        if (paths.Count == 0)
            return;

        var allFilePaths = await Task.Run(() => FileOperation.GetAllFilePaths(paths)).ConfigureAwait(false);
        await ImportMusicFilesAsync(allFilePaths).ConfigureAwait(false);
    }

    /// <summary>
    /// 导入音乐文件到播放列表
    /// </summary>
    /// <param name="filePaths">要导入的文件路径列表</param>
    /// <returns>导入任务</returns>
    public static async Task ImportMusicFilesAsync(IReadOnlyList<string> filePaths)
    {
        // 过滤出音频文件
        var audioFilePaths = await Task.Run(() => AudioFileValidator.FilterAudioFiles(filePaths));

        if (audioFilePaths == null || audioFilePaths.Count == 0)
            return;

        // 预加载现有路径集合
        var existingPaths = new HashSet<string?>(
            MusicPlayerViewModel.Instance.MusicItems.Select(x => x.FilePath),
            StringComparer.OrdinalIgnoreCase
        );

        // 过滤掉已存在的路径
        var newFilePaths = audioFilePaths.Where(path => !existingPaths.Contains(path)).ToList();

        if (newFilePaths.Count == 0)
            return;

        // 并行提取音乐信息，并过滤掉 null 结果
        var musicItems = (
            await Task.WhenAll(newFilePaths.Select(async path => await MusicExtractor.ExtractMusicInfoAsync(path)))
        )
            .Where(m => m != null)
            .ToList(); // 过滤 null 值

        // 批量添加到UI集合
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var musicItem in musicItems)
            {
                MusicPlayerViewModel.Instance.MusicItems.Add(musicItem!);
            }
        });
    }
}
