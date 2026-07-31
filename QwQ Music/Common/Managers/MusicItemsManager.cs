using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Dialogs;
using QwQ_Music.Views.Dialogs;
using Ursa.Controls;

namespace QwQ_Music.Common.Managers;

public class MusicItemsChangedEventArgs : EventArgs {
    public required List<MusicItemModel>? OldItems { get; init; }
    public required List<MusicItemModel>? NewItems { get; init; }
}

public sealed partial class MusicItemsManager : ObservableObject, IDisposable {
    private readonly SemaphoreSlim _addSem = new(1, 1);
    private MusicItemsManager() { InitializeAsync().ContinueWith(LoggerService.HandleException).ConfigureAwait(false); }
    public static MusicItemsManager All { get; } = new() { Name = "QWQ_MUSIC_LIST_ALL_MUSIC_LIST" };

    public required string Name { get; init; }

    public OrderedDictionary<string, MusicItemModel> MusicItems { get; private set; } = new(); //Initialized in .ctor
    public MusicItemModel this[string key] => MusicItems[key];
    public int Count => MusicItems.Count;

    public event EventHandler<MusicItemsChangedEventArgs>? MusicItemsChanged;

    private async Task InitializeAsync() {
        try {
            MusicItems = new OrderedDictionary<string, MusicItemModel>(
                (await MusicItemRepository.Instance.GetAsync().ConfigureAwait(false)).Select(item =>
                    KeyValuePair.Create(item.FilePath, item)));
            if (MusicItems.Count != 0)
                return;

            NotificationService.Info("好像...一首歌都没有（ \n " + "Tips : 可以点击右上角加号从文件中添加音乐哦！");
        } catch (Exception ex) {
            await LoggerService.ErrorAsync($"初始化音乐项出错: \n{ex.Message}\n{ex.StackTrace}").ConfigureAwait(false);
            NotificationService.Error($"初始化音乐项出错: {ex.Message}");
        }
    }

    public async Task AddAsync(IAsyncEnumerable<MusicItemModel> musicItems) {
        List<string> successItems = [];
        await using StringWriter failedItems = new();
        var repo = MusicItemRepository.Instance;

        await foreach (MusicItemModel musicItem in musicItems.ConfigureAwait(false))
            try {
                musicItem.InsertTime = DateTime.UtcNow;
                await repo.InsertAsync(musicItem).ConfigureAwait(false);

                successItems.Add($"《{musicItem.Title}》");
                await _addSem.WaitAsync().ConfigureAwait(false);
                MusicItems.Add(musicItem.FilePath, musicItem);
                OnPropertyChanged(nameof(Count));
                MusicItemsChanged?.Invoke(
                    this,
                    new MusicItemsChangedEventArgs { OldItems = null, NewItems = [musicItem] });
                _addSem.Release();
            } catch (Exception e) {
                await LoggerService.ErrorAsync($"歌曲《{musicItem.Title}》保存到数据库失败！\n{e.Message}\n{e.StackTrace}")
                                   .ConfigureAwait(false);
                await failedItems.WriteAsync($"《{musicItem.Title}》").ConfigureAwait(false);
            }

        if (failedItems.ToString() is { Length: > 0 } failedTitles)
            NotificationService.Error($"歌曲 {failedTitles} 添加失败了！");

        if (successItems.Count > 0)
            NotificationService.Success(
                successItems.Count > 10 ?
                    $"有{successItems.Count}首歌曲添加成功啦~" :
                    $"歌曲{string.Join("", successItems)}添加成功啦~");
    }

    public static async Task UpdateAsync(MusicItemModel musicItem) {
        try {
            await MusicItemRepository.Instance.UpdateAsync(musicItem).ConfigureAwait(false);
        } catch (Exception e) {
            await LoggerService.ErrorAsync($"更新歌曲{musicItem.Title}到数据库失败！\n{e.Message}\n{e.StackTrace}")
                               .ConfigureAwait(false);
            NotificationService.Error($"更新歌曲{musicItem.Title}到数据库失败！\n{e.Message}");
        }
    }

    public static async Task UpdateAsync(IEnumerable<MusicItemModel> musicItems) {
        var successItems = new StringWriter();
        var failedItems = new StringWriter();

        var repo = MusicItemRepository.Instance;
        foreach (MusicItemModel musicItem in musicItems)
            try {
                await repo.UpdateAsync(musicItem).ConfigureAwait(false);
                await successItems.WriteAsync($"《{musicItem.Title}》").ConfigureAwait(false);
            } catch (Exception e) {
                await failedItems.WriteAsync($"《{musicItem.Title}》").ConfigureAwait(false);
                await LoggerService.ErrorAsync($"更新歌曲{musicItem.Title}到数据库失败！\n{e.Message}\n{e.StackTrace}")
                                   .ConfigureAwait(false);
            }

        // 显示删除结果通知
        if (successItems.ToString() is { Length: > 0 } successTitles)
            NotificationService.Success($"{successTitles}更新成功了！");

        if (failedItems.ToString() is { Length: > 0 } failedTitles)
            NotificationService.Error($"更新{failedTitles}失败了！");

        successItems.Close();
        failedItems.Close();
        await successItems.DisposeAsync().ConfigureAwait(false);
        await failedItems.DisposeAsync().ConfigureAwait(false);
    }

    public static async Task UpdateAsync(MusicItemModel musicItem, Dictionary<string, object?> fields) {
        try {
            await MusicItemRepository.Instance.UpdateAsync(musicItem.FilePath, fields).ConfigureAwait(false);
        } catch (Exception e) {
            await LoggerService.ErrorAsync($"更新歌曲《{musicItem.Title}》信息到数据库时发生错误 : \n{e}").ConfigureAwait(false);
            NotificationService.Error($"更新歌曲《{musicItem.Title}》信息到数据库时发生错误 : \n{e.Message}");
        }
    }

    public static async Task UpdatePlayProgressAsync(MusicItemModel musicItem, TimeSpan current) {
        try {
            await MusicItemRepository.Instance.UpdateAsync(
                                         musicItem.FilePath,
                                         new Dictionary<string, object?> {
                                             [nameof(MusicItemModel.Record)] = current.ToString()
                                         })
                                     .ConfigureAwait(false);
        } catch (Exception e) {
            await LoggerService.ErrorAsync($"保存歌曲《{musicItem.Title}》的播放进度到数据库时发生错误 : \n{e}").ConfigureAwait(false);
            NotificationService.Error($"保存歌曲《{musicItem.Title}》的播放进度到数据库时发生错误 : \n{e.Message}");
        }
    }

    public async Task<IEnumerable<MusicItemModel>?> RemoveAsync(IEnumerable<MusicItemModel> musicItems) {
        MusicItemModel[] items = musicItems.ToArray();
        if (items.Length == 0)
            return null;
        // 构建确认提示信息
        string titles = string.Join("、", items.Select(item => $"《{item.Title}》"));

        MessageBoxResult result = await MessageBox.ShowAsync(
                                                      $"你真的要删除以下音乐吗？\n{titles}",
                                                      "警告",
                                                      icon: MessageBoxIcon.Warning,
                                                      button: MessageBoxButton.YesNo)
                                                  .ConfigureAwait(true);
        if (result != MessageBoxResult.Yes)
            return null;
        var successItems = new List<MusicItemModel>();
        await Task.Run(async Task? () => {
                      var repo = MusicItemRepository.Instance;

                      await foreach (MusicItemModel musicItem in items.ToAsyncEnumerable().ConfigureAwait(false))
                          try {
                              await repo.DeleteAsync(musicItem.FilePath).ConfigureAwait(false);
                              musicItem.RemoveCover();

                              successItems.Add(musicItem);
                              MusicItems.Remove(musicItem.FilePath);
                              OnPropertyChanged(nameof(Count));
                              MusicItemsChanged?.Invoke(
                                  this,
                                  new MusicItemsChangedEventArgs { OldItems = [musicItem], NewItems = null });
                          } catch (Exception e) {
                              await LoggerService
                                    .ErrorAsync($"从数据库中删除歌曲{musicItem.Title}失败！\n{e.Message}\n{e.StackTrace}")
                                    .ConfigureAwait(false);

                              NotificationService.Error($"歌曲{musicItem.Title}删除失败！\n{e.Message}");
                          }

                      // 显示删除结果通知
                      if (successItems.Count > 0) {
                          string successTitles = string.Join("", successItems.Select(item => $"《{item.Title}》"));
                          NotificationService.Success($"{successTitles}已经从音乐库中移除了！");
                      }

                      string failedTitles = string.Join(
                          "、",
                          items.Except(successItems).Select(item => $"《{item.Title}》"));
                      if (!string.IsNullOrEmpty(failedTitles))
                          NotificationService.Error($"删除{failedTitles}失败了！");

                      PlaylistManager.Instance.RemoveAllOf(successItems);
                  })
                  .ConfigureAwait(false);
        return successItems;
    }

    [RelayCommand]
    public static void ClearRecords(IList items) {
        if (items.Count == 0)
            return;
        IEnumerable<MusicItemModel> models = items[0] switch {
            MusicItemModel    => items.Cast<MusicItemModel>(),
            PlaylistItemModel => items.Cast<PlaylistItemModel>().Select(item => item.Model),
            _                 => throw new ArgumentException($"未知的音频项类型集合{items.GetType()}", nameof(items))
        };
        foreach (MusicItemModel model in models)
            model.ClearRecord();
    }

    [RelayCommand]
    public static void ShowDetailedInfo(MusicItemModel musicItems) {
        var options = new OverlayDialogOptions { Title = "详细信息", CanLightDismiss = true, Mode = DialogMode.Info };

        OverlayDialog.ShowCustomAsync<AudioDetailedInfo, AudioDetailedInfoViewModel, DialogResult>(
                         new AudioDetailedInfoViewModel(musicItems, musicItems.Track),
                         options: options)
                     .ContinueWith(LoggerService.HandleException)
                     .ConfigureAwait(false);
    }

    [RelayCommand]
    public static void OpenInExplorer(MusicItemModel musicItem) {
        if (string.IsNullOrEmpty(musicItem.FilePath) || !File.Exists(musicItem.FilePath)) {
            NotificationService.Error($"无法打开《{musicItem.Title}》文件位置：{musicItem.FilePath}文件不存在");
            return;
        }

        try {
            FileOperationService.OpenInFileManager(musicItem.FilePath);
        } catch (Exception e) {
            LoggerService.Error($"打开文件位置失败: {e.Message}");
            NotificationService.Error($"打开《{musicItem.Title}》文件位置时报错：{e.Message}");
        }
    }


    [RelayCommand]
    public static void RemoveItems(IList items) {
        All.RemoveAsync(items.Cast<MusicItemModel>()).ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }

    public void Dispose() {
        MusicItemsChanged = null;
        _addSem.Dispose();
        MusicItems.Clear();
        GC.SuppressFinalize(this);
    }

    ~MusicItemsManager() { Dispose(); }
}