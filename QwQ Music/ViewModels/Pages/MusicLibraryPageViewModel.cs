using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.ViewModels.Bases;
using MusicItemsManager = QwQ_Music.Common.Managers.MusicItemsManager;

namespace QwQ_Music.ViewModels.Pages;

public partial class MusicLibraryPageViewModel : MusicItemsViewModelBase {
    public MusicLibraryPageViewModel() : base(nameof(MusicLibraryPageViewModel)) {
        SetCurrentList(MusicItemsManager.All.Name, MusicItemsManager.All.MusicItems.Values.ToList());
        MusicItemsManager.All.MusicItemsChanged += OnMusicsChanged;
    }

    private void OnMusicsChanged(object? sender, MusicItemsChangedEventArgs e) {
        Dispatcher.UIThread.Post(() => ChangeAllItems(e.OldItems, e.NewItems), DispatcherPriority.Background);
    }

    [RelayCommand]
    private static void OpenFile() {
        App.TopLevel?.StorageProvider.OpenFilePickerAsync(
               new FilePickerOpenOptions { Title = "选择音乐文件", AllowMultiple = true })
           .ContinueWith(task => {
               if (task is { IsCompletedSuccessfully: true, Result: { Count: > 0 } items })
                   AudioFileService.ProcessStorageItemsAsync(items).ConfigureAwait(false);
           })
           .ContinueWith(LoggerService.HandleException)
           .ConfigureAwait(false);
    }

    [RelayCommand]
    private static void OpenFolder() {
        App.TopLevel?.StorageProvider.OpenFolderPickerAsync(
               new FolderPickerOpenOptions { Title = "选择包含音乐的文件夹", AllowMultiple = true })
           .ContinueWith(task => {
               if (task is { IsCompletedSuccessfully: true, Result: { Count: > 0 } items })
                   AudioFileService.ProcessStorageItemsAsync(items).ConfigureAwait(false);
           })
           .ContinueWith(LoggerService.HandleException)
           .ConfigureAwait(false);
    }

    [RelayCommand]
    private static void DropFiles(DragEventArgs? e) {
        if (e?.DataTransfer.Contains(DataFormat.File) != true)
            return;

        IStorageItem[]? items = e.DataTransfer.TryGetFiles();

        if (items == null || items.Length == 0)
            return;

        AudioFileService.ProcessStorageItemsAsync(items)
                        .ContinueWith(LoggerService.HandleException)
                        .ConfigureAwait(false);
    }

    ~MusicLibraryPageViewModel() { MusicItemsManager.All.MusicItemsChanged -= OnMusicsChanged; }
}