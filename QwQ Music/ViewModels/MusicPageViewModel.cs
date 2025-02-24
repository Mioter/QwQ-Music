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
using QwQ_Music.Models;
using QwQ_Music.Services;

namespace QwQ_Music.ViewModels;

public partial class MusicPageViewModel : ViewModelBase, IDisposable {
    [ObservableProperty] private ObservableCollection<MusicItemModel> _allMusicItems;

    [ObservableProperty] private string? _searchText;

    [ObservableProperty] private MusicItemModel? _selectedItem;
    
    public MusicPageViewModel()
    {
        AllMusicItems = MusicPlayerViewModel.MusicItems;
        MusicPlayerViewModel.MusicItemsChanged += OnMusicPlayerViewModelOnMusicItemsChanged;
    }

    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private void OnMusicPlayerViewModelOnMusicItemsChanged(object? _, ObservableCollection<MusicItemModel> musicItems)
    {
        AllMusicItems = musicItems;
    }

    partial void OnSearchTextChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            AllMusicItems = MusicPlayerViewModel.MusicItems;
        }
        else
        {
            // 使用 Where 进行过滤，并将结果转换为 ObservableCollection。
            AllMusicItems = new ObservableCollection<MusicItemModel>(
                MusicPlayerViewModel.MusicItems.Where(
                    musicItem => musicItem.Title.Contains(value) ||
                                 musicItem.Artists.Contains(value) ||
                                 musicItem.Album.Contains(value)));
        }
    }

    [RelayCommand]
    private void ToggleMusic() {
        if (SelectedItem == null) return;

        MusicPlayerViewModel.IsPlaying = false;
        MusicPlayerViewModel.SetCurrentMusicItem(SelectedItem);
    }

    [RelayCommand]
    private void SelectedCurrentMusicItem() {
        SelectedItem = null;
        SelectedItem = MusicPlayerViewModel.CurrentMusicItem;
    }

    [RelayCommand]
    private async Task DropFilesAsync(DragEventArgs? e) {
        if (e?.Data.Contains(DataFormats.Files) != true) return;

        var items = e.Data.GetFiles()?.ToList();

        if (items == null) return;

        var allFilePaths = GetAllFilePaths(items);
        var audioFilePaths = AudioFileValidator.FilterAudioFiles(allFilePaths);

        if (audioFilePaths == null || audioFilePaths.Count == 0) return;

        foreach (var item in await Task.WhenAll(audioFilePaths.Select(MusicExtractor.ExtractMusicInfoAsync))) {
            if (item != null && !MusicPlayerViewModel.MusicItems.Any(x => x.Equals(item))) {
                MusicPlayerViewModel.MusicItems.Add(item);
            }
        }
    }

    private static List<string> GetAllFilePaths(List<IStorageItem> items) {
        var allFilePaths = new List<string>();
        foreach (var uri in items.Select(item => item.Path)) {
            if (!uri.IsAbsoluteUri) {
                Console.WriteLine($"跳过非绝对路径的项: {uri}");
                continue; // 跳过无效项
            }

            string path;
            try { path = uri.LocalPath; } catch (InvalidOperationException ex) {
                Console.WriteLine($"无法解析路径: {uri} (错误: {ex.Message})");
                continue;
            }

            if (Directory.Exists(path)) {
                try {
                    string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                    allFilePaths.AddRange(files);
                } catch (Exception ex) { Console.WriteLine($"无法访问目录 {path}: {ex.Message}"); }
            } else if (File.Exists(path)) { allFilePaths.Add(path); } else { Console.WriteLine($"路径不存在: {path}"); }
        }

        return allFilePaths;
    }
    
    ~MusicPageViewModel() => Dispose(false);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            MusicPlayerViewModel.MusicItemsChanged -= OnMusicPlayerViewModelOnMusicItemsChanged;
    }
}