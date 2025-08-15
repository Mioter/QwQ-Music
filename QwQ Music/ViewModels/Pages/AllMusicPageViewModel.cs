using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public partial class AllMusicPageViewModel() : DataGridViewModelBase(MusicItemManager.Default.MusicItems)
{
    private readonly AvaloniaList<MusicItemModel> _filterSource = [];

    protected override void OnSearchTextChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            MusicItems = MusicItemManager.Default.MusicItems;

            return;
        }

        var source = string.IsNullOrEmpty(value)
            ? MusicItemManager.Default.MusicItems
            : MusicItemManager.Default.MusicItems.Where(MatchesSearchCriteria);

        _filterSource.Clear();
        _filterSource.AddRange(source);
        MusicItems = _filterSource;

        return;

        bool MatchesSearchCriteria(MusicItemModel item)
        {
            return item.Title.Contains(value, StringComparison.OrdinalIgnoreCase)
             || item.Artists.Contains(value, StringComparison.OrdinalIgnoreCase)
             || item.Album.Contains(value, StringComparison.OrdinalIgnoreCase);
        }
    }

    [RelayCommand]
    private static async Task OpenFileAsync()
    {
        if (App.TopLevel == null)
            return;

        var items = await App.TopLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "选择音乐文件",
                AllowMultiple = true,
            }
        );

        if (items.Count == 0)
            return;

        await AudioFileManager.ProcessStorageItemsAsync(items);
    }

    [RelayCommand]
    private static async Task DropFilesAsync(DragEventArgs? e)
    {
        if (e?.Data.Contains(DataFormats.Files) != true)
            return;

        var items = e.Data.GetFiles()?.ToList();

        if (items == null || items.Count == 0)
            return;

        await AudioFileManager.ProcessStorageItemsAsync(items);
    }
}
