using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common;
using QwQ_Music.Models;
using QwQ_Music.Tools;

namespace QwQ_Music.ViewModels;

public partial class MusicPageViewModel : ViewModelBase
{

    [ObservableProperty] private MusicItemModel? _selectedItem;

    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    [RelayCommand]
    private void ToggleMusic()
    {
        if (SelectedItem != null)
            MusicPlayerViewModel.UpdateMusicPlaylist(SelectedItem);
    }

    [RelayCommand]
    private async Task DropFilesAsync(DragEventArgs? e)
    {
        if (e?.Data.Contains(DataFormats.Files) != true) return;

        var items = e.Data.GetFiles()?.ToList();

        if (items == null) return;

        var allFilePaths = GetAllFilePaths(items);
        var audioFilePaths = AudioFileValidator.FilterAudioFiles(allFilePaths);

        if (audioFilePaths == null || audioFilePaths.Count == 0) return;

        foreach (var item in await Task.WhenAll(audioFilePaths.Select(MusicExtractor.ExtractMusicInfoAsync)))
        {
            if (item != null)
            {
                MusicPlayerViewModel.MusicItems.Add(item);
            }
        }
    }

    private static List<string> GetAllFilePaths(List<IStorageItem> items)
    {
        var allFilePaths = new List<string>();
        foreach (var item in items)
        {
            var uri = item.Path;
            if (!uri.IsAbsoluteUri)
            {
                Console.WriteLine($"跳过非绝对路径的项: {uri}");
                continue; // 跳过无效项
            }

            string path;
            try
            {
                path = uri.LocalPath;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"无法解析路径: {uri} (错误: {ex.Message})");
                continue;
            }

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
