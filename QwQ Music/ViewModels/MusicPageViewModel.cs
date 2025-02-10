using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        if (audioFilePaths == null) return;

        var musicItems = await Task.WhenAll(audioFilePaths.Select(MusicExtractor.ExtractMusicInfoAsync));
        MusicPlayerViewModel.MusicItems = new ObservableCollection<MusicItemModel>(musicItems.Where(item => item != null).ToList()!);
    }

    private static List<string> GetAllFilePaths(List<IStorageItem> items)
    {
        var allFilePaths = new List<string>();
        foreach (string path in items.Select(item => item.Path.LocalPath))
        {
            if (Directory.Exists(path))
            {
                try
                {
                    // 递归获取所有文件
                    string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                    allFilePaths.AddRange(files);
                }
                catch
                {
                    // 处理无法访问的目录
                    Console.WriteLine($"无法访问的路径: {path}");
                }
            }
            else if (File.Exists(path))
            {
                allFilePaths.Add(path);
            }
        }
        return allFilePaths;
    }
}
