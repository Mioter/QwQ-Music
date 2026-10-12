using System.Collections;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Dialogs;
using QwQ_Music.Views.Dialogs;
using Ursa.Controls;

namespace QwQ_Music.Common.Managers;

public sealed partial class MusicListsManager : ObservableObject, IDisposable {
    private MusicListsManager() { InitializeAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false); }
    public static MusicListsManager Instance { get; } = new();

    public List<MusicListModel> MusicLists { get; set; } = [];
    public event EventHandler? CollectionChanged;

    private void InvokeIfChanged() { CollectionChanged?.Invoke(this, EventArgs.Empty); }

    private async Task InitializeAsync() {
        try {
            MusicLists.AddRange(await MusicListRepository.Instance.GetAsync().ConfigureAwait(true));
            InvokeIfChanged();
        } catch (Exception e) {
            NotificationService.Error("歌单信息加载失败！\n" + $"{e.Message}");

            await LoggerService.ErrorAsync("歌单信息加载失败！\n" + $"{e.Message}\n{e.StackTrace}").ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     添加歌单信息
    /// </summary>
    /// <param name="model">歌单模型</param>
    private async Task AddMusicListAsync(MusicListModel model) {
        await MusicListRepository.Instance.InsertAsync(model).ConfigureAwait(false);

        MusicLists.Add(model);
        InvokeIfChanged();
        NotificationService.Success("成功", $"歌单《{model.Name}》创建成功！");
    }

    [RelayCommand]
    private void CreatePlaylistWithMusicItem(IList items) {
        if (items[0] is not MusicItemModel)
            throw new InvalidCastException();
        CreateMusicListAsync()
            .ContinueWith(task => {
                if (task is { IsCompletedSuccessfully: true, Result: { } list })
                    AddToMusicListAsync(items.Cast<MusicItemModel>().ToArray(), list)
                        .ContinueWith(LoggerService.HandleException)
                        .ConfigureAwait(false);
            })
            .ContinueWith(LoggerService.HandleException)
            .ConfigureAwait(false);
    }

    [RelayCommand]
    public void CreateMusicList() {
        CreateMusicListAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    private async Task<MusicListModel?> CreateMusicListAsync() {
        var options = new OverlayDialogOptions { Title = "新建歌单" };
        MusicListModel? model = await Dispatcher.UIThread
                                                .InvokeAsync(async Task<MusicListModel?> () =>
                                                                 await OverlayDialog
                                                                       .ShowCustomAsync<CreateMusicList,
                                                                           CreateMusicListViewModel, MusicListModel>(
                                                                           new CreateMusicListViewModel(""),
                                                                           options: options)
                                                                       .ConfigureAwait(true))
                                                .ConfigureAwait(false);

        if (model == null)
            return null;

        try {
            await AddMusicListAsync(model).ConfigureAwait(false);

            if (model.Thumbnail == CacheManager.NotExist)
                return model;

            await MusicListCoverRepository.Instance.InsertAsync(model).ConfigureAwait(false);
            return model;
        } catch (SqliteException sqlEx) when (sqlEx.SqliteErrorCode == 19) {
            await LoggerService.InfoAsync($"尝试创建歌单{model.Name}失败，因为该歌单已存在。").ConfigureAwait(false);
            NotificationService.Error($"创建歌单\"{model.Name}\"失败！该歌单已存在");
            return await CreateMusicListAsync().ConfigureAwait(false);
        } catch (Exception ex) {
            await LoggerService.ErrorAsync($"创建歌单{model.Name}失败", ex).ConfigureAwait(false);

            NotificationService.Error($"创建歌单《{model.Name}》失败！\n" + $"{ex.Message}");

            return null;
        }
    }

    /// <summary>
    ///     批量添加音乐项到指定名称歌单
    /// </summary>
    /// <param name="musicItems">音乐项列表</param>
    /// <param name="musicList">歌单项</param>
    public async Task AddToMusicListAsync(ICollection<MusicItemModel> musicItems, MusicListModel musicList) {
        if (musicItems.Count == 0)
            return;

        var repo = MusicListItemsRepository.Instance;

        List<MusicItemModel> newItems = [];
        foreach (MusicItemModel item in musicItems) {
            if (!await repo.ContainsAsync((musicList.Name, musicList.Creator), item.FilePath).ConfigureAwait(false)) {
                newItems.Add(item);
            }
        }

        // 如果有已存在的音乐项，显示提示
        if (newItems.Count != musicItems.Count) {
            MusicItemModel[] existingItems = musicItems.Except(newItems).ToArray();
            string existingTitles = string.Join("、", existingItems.Select(item => $"《{item.Title}》"));
            NotificationService.Info("提示", $"歌曲{existingTitles}已存在于歌单 {musicList.Name} 中！");
        }

        try {
            await repo.InsertAsync(musicList, newItems).ConfigureAwait(false); // 显示添加结果通知
            string titles = string.Join(' ', newItems.Select(item => $"《{item.Title}》"));
            NotificationService.Success($"已将歌曲{titles}添加到歌单：{musicList.Name}！");
            await LoggerService.DebugAsync($"已将{titles}").ConfigureAwait(false);
        } catch (SqliteException ex) {
            NotificationService.Error("添加歌曲失败");
            await LoggerService.ErrorAsync("添加歌曲失败:", ex).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     批量从指定名称歌单中移除音乐项
    /// </summary>
    /// <param name="musicItems">音乐项列表</param>
    /// <param name="musicList">歌单项</param>
    public async Task RemoveFromMusicList(ICollection<MusicItemModel> musicItems, MusicListModel musicList) {
        if (musicItems.Count == 0)
            return;

        var failedItems = new List<MusicItemModel>();

        var repo = MusicListItemsRepository.Instance;

        foreach (MusicItemModel item in musicItems)
            try {
                await repo.RemoveAsync(musicList, item).ConfigureAwait(false);
            } catch (Exception) {
                failedItems.Add(item);
            }

        ICollection<MusicItemModel> successItems;
        if (failedItems.Count == 0) {
            successItems = musicItems;
        } else {
            successItems = musicItems.Except(failedItems).ToArray();
            string failedTitles = string.Join("、", failedItems.Select(item => $"《{item.Title}》"));
            NotificationService.Error($"从歌单移除歌曲{failedTitles}失败！");
        }

        // 显示移除结果通知
        if (successItems.Count > 0) {
            string successTitles = string.Join("、", successItems.Select(item => $"《{item.Title}》"));

            NotificationService.Success($"已将歌曲{successTitles}从歌单 {musicList.Name} 中移除！");
        }
    }

    [RelayCommand]
    private void DeleteMusicList(MusicListModel musicList) {
        MessageBox.ShowAsync(
                      $"你真的要删除歌单《{musicList.Name}》吗?",
                      "警告",
                      icon: MessageBoxIcon.Warning,
                      button: MessageBoxButton.YesNo)
                  .ContinueWith(task => {
                      if (task is not { IsCompletedSuccessfully: true, Result: MessageBoxResult.Yes })
                          return;
                      try {
                          MusicListRepository.Instance.DeleteAsync((musicList.Name, musicList.Creator))
                                             .ContinueWith(LoggerService.HandleException)
                                             .ConfigureAwait(false);

                          // 从图片缓存中移除
                          if (!musicList.IsCoverExist)
                              CacheManager.ImageCache.Remove(musicList.Name);

                          MusicLists.Remove(musicList);
                          InvokeIfChanged();
                          NotificationService.Success($"歌单《{musicList.Name}》删除成功！");
                      } catch (Exception ex) {
                          LoggerService.Error($"删除歌单失败:\n{ex.Message}\n{ex.StackTrace}");
                          NotificationService.Error($"删除歌单《{musicList.Name}》失败！\n{ex.Message}");
                          throw;
                      }
                  })
                  .ContinueWith(LoggerService.HandleException)
                  .ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task AddToMusicListAsync((MusicItemModel musicItem, MusicListModel musicList) argument) {
        await AddToMusicListAsync([argument.musicItem], argument.musicList).ConfigureAwait(false);
    }

    [RelayCommand]
    private static async Task EditMusicListName(MusicListModel musicList) {
        var options = new OverlayDialogOptions { Title = "修改名称" };

        string? result = await OverlayDialog.ShowCustomAsync<EditText, EditTextViewModel, string>(
                                                new EditTextViewModel(musicList.Name, options.Title, 64),
                                                options: options)
                                            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(result))
            return;

        try {
            await MusicListRepository.Instance.UpdateAsync(
                                         (musicList.Name, musicList.Creator),
                                         new Dictionary<string, object?> { [nameof(musicList.Name)] = musicList.Name })
                                     .ConfigureAwait(false);
            musicList.Name = result;

            NotificationService.Success($"修改歌单 {musicList.Name}的名称成功了");
        } catch (Exception e) {
            await LoggerService.ErrorAsync($"修改歌单 {musicList.Name} 的名称失败了:\n{e.Message}\n{e.StackTrace}")
                               .ConfigureAwait(false);

            NotificationService.Error($"修改歌单 {musicList.Name} 的名称失败了");
        }
    }

    [RelayCommand]
    private static async Task EditMusicListDescription(MusicListModel musicList) {
        var options = new OverlayDialogOptions { Title = "修改描述", Mode = DialogMode.Info };

        string? result = await OverlayDialog.ShowCustomAsync<EditText, EditTextViewModel, string>(
                                                new EditTextViewModel(musicList.Description, options.Title),
                                                options: options)
                                            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(result))
            return;

        try {
            await MusicListRepository.Instance.UpdateAsync(
                                         (musicList.Name, musicList.Creator),
                                         new Dictionary<string, object?> {
                                             [nameof(musicList.Description)] = musicList.Description
                                         })
                                     .ConfigureAwait(false);

            musicList.Name = result;

            NotificationService.Success($"修改歌单 {musicList.Name}的描述成功了");
        } catch (Exception e) {
            await LoggerService.ErrorAsync($"修改歌单 {musicList.Name} 的描述失败了:\n{e.Message}\n{e.StackTrace}")
                               .ConfigureAwait(false);

            NotificationService.Error($"修改歌单 {musicList.Name} 的描述失败了");
        }
    }

    [RelayCommand]
    private static async Task EditMusicListCover(MusicListModel musicList) {
        if (App.TopLevel == null)
            return;

        var options = new OverlayDialogOptions { Title = "图片裁剪" };

        if (await FileOperationService.OpenImageFile(App.TopLevel).ConfigureAwait(false) is not { } bitmap ||
            await OverlayDialog.ShowCustomAsync<ImageCropping, ImageCroppingViewModel, Bitmap>(
                                   new ImageCroppingViewModel(bitmap),
                                   options: options)
                               .ConfigureAwait(false) is not { } newCover)
            return;

        musicList.Thumbnail = newCover;

        await MusicListCoverRepository.Instance.InsertAsync(musicList).ConfigureAwait(false);
    }

    public void Dispose() {
        CollectionChanged = null;
        MusicLists.Clear();
        GC.SuppressFinalize(this);
    }

    ~MusicListsManager() { Dispose(); }
}