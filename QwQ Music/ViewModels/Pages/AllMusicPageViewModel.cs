using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.Pages;

public partial class AllMusicPageViewModel() : DataGridViewModelBase(MusicPlayerViewModel.Instance.MusicItems)
{
    protected override void OnSearchTextChanged(string? value)
    {
        var source = string.IsNullOrEmpty(value)
            ? MusicPlayerViewModel.MusicItems
            : MusicPlayerViewModel.MusicItems.Where(MatchesSearchCriteria);

        MusicItems = new ObservableCollection<MusicItemModel>(source);
        return;

        bool MatchesSearchCriteria(MusicItemModel item) =>
            item.Title.Contains(value, StringComparison.OrdinalIgnoreCase)
            || item.Artists.Contains(value, StringComparison.OrdinalIgnoreCase)
            || item.Album.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    [RelayCommand]
    private static async Task OpenFileAsync()
    {
        if (App.TopLevel == null)
            return ;
        
        var items = await App.TopLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions { Title = "选择音乐文件", AllowMultiple = true }
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
