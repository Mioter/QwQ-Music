using System.Collections;
using System.Collections.Concurrent;
using Avalonia.Collections;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.ViewModels.Bases;

public class DataList<T> : List<T> {
    
}

public abstract partial class ItemsViewModelBase<T>(string viewModelName) : NavigableViewModel(viewModelName) {
    [ObservableProperty]
    public partial double DataGridHorizontalScrollValue { get; set; }

    protected string MusicListName { get; private set; } = "Unknown";

    protected List<T> AllItemsList { get; private set; } = [];

    [ObservableProperty]
    public partial AvaloniaList<T> FilteredList { get; set; } = [];

    public string SearchText {
        get;
        set {
            if (SetProperty(ref field, value))
                OnSearchTextChanged(field);
        }
    } = "";

    public List<T> SelectedItems { get; set; } = [];

    protected void SetCurrentList(string name, params List<T> items) {
        MusicListName = name;
        AllItemsList = items;
        OnSearchTextChanged(SearchText);
    }

    public void ChangeAllItems(IEnumerable<T>? oldItems, IEnumerable<T>? newItems) {
        T[]? olds = oldItems?.ToArray();
        T[]? news = newItems?.ToArray();
        if (olds is { Length: > 0 }) {
            AllItemsList.RemoveAll(olds.Contains);
            FilteredList.RemoveAll(olds.Where(item => CustomFilter(SearchText, item)));
        }

        if (news is { Length: > 0 }) {
            AllItemsList.AddRange(news);
            FilteredList.AddRange(news.Where(item => CustomFilter(SearchText, item)));
        }
    }

    protected void OnSearchTextChanged(string value) {
        if (string.IsNullOrEmpty(value)) {
            if (FilteredList.Count != AllItemsList.Count) {
                FilteredList.Clear();
                FilteredList.AddRange(AllItemsList);
            }

            return;
        }

        FilteredList = new AvaloniaList<T>(AllItemsList.AsParallel().Where(item => CustomFilter(value, item)));
    }

    protected abstract bool CustomFilter(in string value, in T item);

    [RelayCommand]
    private void SelectedItemsChanged(IList items) { SelectedItems = items.Cast<T>().ToList(); }

    [RelayCommand]
    private void ScrollToTop(DataGrid dataGrid) {
        // 滚动到第一行（第一行数据）
        dataGrid.ScrollIntoView(dataGrid.CollectionView.Cast<T>().FirstOrDefault(), null);
    }
}

public partial class MusicItemsViewModelBase(string viewModelName) : ItemsViewModelBase<MusicItemModel>(viewModelName) {
    public static MusicListsManager MusicListsManager => MusicListsManager.Instance;
    public static MusicItemsManager MusicItemsManager => MusicItemsManager.All;

    [RelayCommand]
    private static void JumpToTop(DataGrid dataGrid) {
        // 滚动到第一行（第一行数据）
        dataGrid.ScrollIntoView(dataGrid.CollectionView.Cast<object>().First(), null);
    }

    [RelayCommand]
    private void SetMusicList(MusicItemModel? model) {
        string name;
        IList<MusicItemModel> items;
        int index;
        if (model is not null) {
            name = MusicListName;
            items = AllItemsList;
            index = AllItemsList.IndexOf(model);
        } else {
            name = MusicItemsManager.All.Name;
            items = MusicItemsManager.All.MusicItems.Values;
            index = 0;
        }


        (ConfigManager.PlayerConfig.AddMusicBehavior switch {
                AddMusicBehavior.AddToNext => Task.FromResult(PlaylistManager.Instance.InsertToNext(SelectedItems)),
                AddMusicBehavior.SetToList => PlaylistManager.Instance.ReplaceAsync(
                    PlaylistManager.Custom,
                    SelectedItems,
                    0,
                    true),
                AddMusicBehavior.ReplaceList => PlaylistManager.Instance.ReplaceAsync(name, items, index, true,true),
                _ => throw new IndexOutOfRangeException(
                    $"{ConfigManager.PlayerConfig.AddMusicBehavior} is not a valid state of {
                        nameof(ConfigManager.PlayerConfig.AddMusicBehavior)}")
            }).ContinueWith(LoggerService.HandleException)
              .ConfigureAwait(false);
    }

    [RelayCommand]
    private void SelectCurrentMusicItem() { SelectedItems = [AudioPlayManager.Instance.CurrentMusicItem.Model]; }

    [RelayCommand]
    private static void AddToPlaylistNext(IList items) {
        PlaylistManager.Instance.Insert(PlaylistManager.Instance.CurrentItem, items.Cast<MusicItemModel>());
    }

    [RelayCommand]
    private void AddSelectedTo(MusicListModel? musicListModel) {
        if (SelectedItems is { Count: > 0 } && musicListModel is not null)
            MusicListsManager.Instance.AddToMusicListAsync(SelectedItems, musicListModel)
                             .ContinueWith(LoggerService.HandleException)
                             .ConfigureAwait(false);
    }

    [RelayCommand]
    private void RemoveSelectedFrom(MusicListModel? musicListModel) {
        if (SelectedItems is { Count: > 0 } && musicListModel?.Name != null)
            MusicListsManager.Instance.RemoveFromMusicList(SelectedItems, musicListModel)
                             .ContinueWith(LoggerService.HandleException)
                             .ConfigureAwait(false);
    }

    [RelayCommand]
    private void PlayMusicList() {
        if (FilteredList.Count == 0)
            return;

        PlaylistManager.Instance.ReplaceAsync(PlaylistManager.Custom, FilteredList, 0, true)
                       .ContinueWith(LoggerService.HandleException)
                       .ConfigureAwait(false);
    }

    protected override bool CustomFilter(in string value, in MusicItemModel item) {
        //TODO TAGS
        return item.Title.Contains(value, StringComparison.OrdinalIgnoreCase) ||
               item.Artists.Contains(value, StringComparison.OrdinalIgnoreCase) ||
               item.Album.Contains(value, StringComparison.OrdinalIgnoreCase);
    }


    [RelayCommand]
    private void ForceRefreshMusicInfo() {
        RefreshMusicItemsAsync(true).ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
        CacheManager.ImageCache.Clear();
    }

    [RelayCommand]
    private void RefreshMusicInfo() {
        RefreshMusicItemsAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    private async Task RefreshMusicItemsAsync(bool forceRefresh = false) {
        NotificationService.Info("正在刷新音乐信息...");

        var itemsToRemove = new ConcurrentBag<MusicItemModel>();
        var itemsToUpdate = new ConcurrentBag<MusicItemModel>();


        foreach (MusicItemModel item in MusicItemsManager.All.MusicItems.Values) {
            if (!File.Exists(item.FilePath)) {
                itemsToRemove.Add(item);

                return;
            }

            await item.UpdateMetaDataAsync(forceRefresh)
                      .ContinueWith(task => {
                          if (!task.IsCompletedSuccessfully)
                              return;
                          if (task.Result) {
                              itemsToUpdate.Add(item);
                          } else {
                              LoggerService.Error($"刷新《{item.Title}》的信息失败。");
                              itemsToRemove.Add(item);
                          }
                      })
                      .ConfigureAwait(false);
        }

        await HandleBatchOperationsAsync(itemsToRemove, itemsToUpdate).ConfigureAwait(false);

        ShowRefreshSummary(itemsToRemove.Count, itemsToUpdate.Count);
    }

    private async Task HandleBatchOperationsAsync(
        ConcurrentBag<MusicItemModel> itemsToRemove,
        ConcurrentBag<MusicItemModel> itemsToUpdate) {
        try {
            if (!itemsToRemove.IsEmpty)
                await DeleteMusicItemsAsync(itemsToRemove).ConfigureAwait(false);

            if (!itemsToUpdate.IsEmpty)
                await MusicItemsManager.UpdateAsync(itemsToUpdate).ConfigureAwait(false);
        } catch (Exception ex) {
            await LoggerService.ErrorAsync($"更新音乐信息到数据库时发生错误: {ex.Message}\n{ex.StackTrace}").ConfigureAwait(false);
            NotificationService.Error($"更新音乐信息到数据库失败: {ex.Message}");
        }
    }

    private static void ShowRefreshSummary(int removedCount, int updatedCount) {
        if (removedCount == 0 && updatedCount == 0) {
            NotificationService.Success("所有音乐文件信息都是最新的！");

            return;
        }

        string message = "刷新完成！";
        if (removedCount > 0)
            message += $"\n删除了 {removedCount} 个不存在的音乐文件";

        if (updatedCount > 0)
            message += $"\n更新了 {updatedCount} 个音乐文件的信息";

        NotificationService.Success(message);
    }

    private async Task DeleteMusicItemsAsync(IEnumerable items) {
        MusicItemModel[] musicItems = (items switch {
            IEnumerable<MusicItemModel> itemModels            => itemModels,
            IEnumerable<PlaylistItemModel> playlistItemModels => playlistItemModels.Select(item => item.Model),
            _ => throw new ArgumentOutOfRangeException(
                nameof(items),
                $"{nameof(items)} must be IEnumerable of types of MusicItemModel or PlaylistItemModel.")
        }).ToArray();
        if (musicItems.Length == 0) {
            NotificationService.Info("提示", "请先选择音乐项哦~");

            return;
        }

        IEnumerable<MusicItemModel>? successEnumerable =
            await MusicItemsManager.All.RemoveAsync(musicItems).ConfigureAwait(false);

        if (successEnumerable?.ToArray() is not { Length: > 0 } successItems)
            return;

        AudioPlayManager.Instance.CheckForRemovedItems(successItems);
        PlaylistManager.Instance.RemoveAllOf(successItems);
        FilteredList.RemoveAll(successItems);
    }
}