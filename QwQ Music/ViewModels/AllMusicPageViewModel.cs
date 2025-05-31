using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Services;

namespace QwQ_Music.ViewModels;

public partial class AllMusicPageViewModel : ViewModelBase
{
    private const string FILE_PICKER_TITLE = "选择音乐文件";

    [ObservableProperty]
    public partial ObservableCollection<MusicItemModel> AllMusicItems { get; set; } = MusicPlayerViewModel.MusicItems;

    [ObservableProperty]
    public partial string? SearchText { get; set; }

    [ObservableProperty]
    public partial MusicItemModel? SelectedItem { get; set; }

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    partial void OnSearchTextChanged(string? value)
    {
        var source = string.IsNullOrEmpty(value)
            ? MusicPlayerViewModel.MusicItems
            : MusicPlayerViewModel.MusicItems.Where(MatchesSearchCriteria);

        AllMusicItems = new ObservableCollection<MusicItemModel>(source);
        return;

        bool MatchesSearchCriteria(MusicItemModel item) =>
            item.Title.Contains(value, StringComparison.OrdinalIgnoreCase)
            || item.Artists.Contains(value, StringComparison.OrdinalIgnoreCase)
            || item.Album.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private async Task ToggleMusicAsync()
    {
        if (SelectedItem == null)
            return;

        MusicPlayerViewModel.IsPlaying = false;
        await MusicPlayerViewModel.SetCurrentMusicItem(SelectedItem);
    }

    [RelayCommand]
    private void SelectedCurrentMusicItem()
    {
        SelectedItem = null;
        SelectedItem = MusicPlayerViewModel.CurrentMusicItem;
    }

    [RelayCommand]
    private async Task DropFilesAsync(DragEventArgs? e)
    {
        if (e?.Data.Contains(DataFormats.Files) != true)
            return;

        var items = e.Data.GetFiles()?.ToList();
        if (items == null || items.Count == 0)
            return;

        await ProcessStorageItemsAsync(items);
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var topLevel = App.TopLevel;
        if (topLevel == null)
            return;

        var items = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = FILE_PICKER_TITLE, AllowMultiple = true }
        );

        if (items.Count == 0)
            return;

        await ProcessStorageItemsAsync(items);
    }

    /// <summary>
    /// 处理存储项目并导入音乐文件
    /// </summary>
    private async Task ProcessStorageItemsAsync(IReadOnlyList<IStorageItem> items)
    {
        var paths = ConvertStorageItemsToPathStrings(items);
        if (paths.Count == 0)
            return;

        var allFilePaths = await Task.Run(() => GetAllFilePaths(paths)).ConfigureAwait(false);
        await ImportMusicFilesAsync(allFilePaths).ConfigureAwait(false);
    }

    /// <summary>
    /// 将存储项目转换为路径字符串列表
    /// </summary>
    private static List<string> ConvertStorageItemsToPathStrings(IEnumerable<IStorageItem> items)
    {
        return items
            .Select(item =>
            {
                try
                {
                    return item.Path.IsAbsoluteUri ? item.Path.LocalPath : null;
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is UriFormatException)
                {
                    Console.WriteLine($"无法解析路径: {item.Path} (错误: {ex.Message})");
                    return null;
                }
            })
            .Where(path => !string.IsNullOrEmpty(path))
            .ToList()!;
    }

    /// <summary>
    /// 导入音乐文件到播放列表
    /// </summary>
    /// <param name="filePaths">要导入的文件路径列表</param>
    /// <returns>导入任务</returns>
    private async Task ImportMusicFilesAsync(IReadOnlyList<string> filePaths)
    {
        // 过滤出音频文件
        var audioFilePaths = await Task.Run(() => AudioFileValidator.FilterAudioFiles(filePaths));

        if (audioFilePaths == null || audioFilePaths.Count == 0)
            return;

        // 预加载现有路径集合
        var existingPaths = new HashSet<string?>(
            MusicPlayerViewModel.MusicItems.Select(x => x.FilePath),
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
                MusicPlayerViewModel.MusicItems.Add(musicItem!);
            }
        });
    }

    /// <summary>
    /// 从路径列表获取所有文件路径（包括子目录）
    /// </summary>
    /// <param name="paths">路径列表</param>
    /// <returns>所有文件路径</returns>
    private static List<string> GetAllFilePaths(IReadOnlyList<string> paths)
    {
        var allFilePaths = new List<string>();

        foreach (string path in paths)
        {
            if (string.IsNullOrEmpty(path))
                continue;

            if (Directory.Exists(path))
            {
                try
                {
                    string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                    allFilePaths.AddRange(files);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法访问目录 {path}: {ex.Message}");
                }
            }
            else if (File.Exists(path))
            {
                allFilePaths.Add(path);
            }
            else
            {
                Console.WriteLine($"路径不存在: {path}");
            }
        }

        return allFilePaths;
    }
}
