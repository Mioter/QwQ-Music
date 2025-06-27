using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Services;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.UserControls;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.Pages;
using QwQ_Music.Views.UserControls;
using Ursa.Controls;

namespace QwQ_Music.ViewModels.Pages;

public partial class MusicListsPageViewModel : ViewModelBase
{
    public static IBrush RandomColor => ColorGenerator.GeneratePastelColor();

    public static MusicPlayerViewModel MusicPlayerViewModel { get; set; } = null!;

    public ObservableCollection<MusicListModel> PlayListItems { get; set; } = [];

    public async Task InitializeAsync(ObservableCollection<MusicItemModel> allMusicItems)
    {
        // 检查 LISTINFO 表是否存在记录
        int count = await DataBaseService.GetRecordCountAsync(DataBaseService.Table.LISTINFO).ConfigureAwait(false);
        if (count == 0)
        {
            await LoggerService.DebugAsync("歌单列表为空，跳过加载").ConfigureAwait(false);
            return;
        }

        await LoadPlayListsAsync(allMusicItems).ConfigureAwait(false);
    }

    [RelayCommand]
    private static async Task OpenMusicLists(MusicListModel musicList)
    {
        if (!musicList.IsInitialized)
        {
            await musicList.LoadAsync(MusicPlayerViewModel.MusicItems);
        }

        MainWindowViewModel.Instance.AddTabPage(
            musicList.Id,
            musicList.Name,
            musicList.CoverImage,
            new ViewMusicListPage { DataContext = new ViewMusicListPageViewModel(musicList) }
        );
    }

    [RelayCommand]
    private static async Task TogglePlaylist(MusicListModel musicList)
    {
        if (musicList.MusicItems.Count <= 0)
            return;

        if (!musicList.IsInitialized)
        {
            await musicList.LoadAsync(MusicPlayerViewModel.MusicItems);
        }

        MusicItemModel? selectedMusic = null;

        // 如果有最近播放记录，尝试找到对应歌曲
        if (musicList.LatestPlayedMusic != null)
        {
            selectedMusic = musicList.MusicItems.FirstOrDefault(x => x.FilePath == musicList.LatestPlayedMusic);
        }

        await MusicPlayerViewModel.TogglePlaylist(musicList.MusicItems, selectedMusic);
    }

    /// <summary>
    /// 添加歌单模型到歌单集合
    /// </summary>
    /// <param name="model">歌单模型</param>
    private async Task AddMusicListModel(MusicListModel model)
    {
        PlayListItems.Add(model);

        var result = await DataBaseService.InsertDataAsync(model.Dump(), DataBaseService.Table.LISTINFO);

        if (result == DataBaseService.OperationResult.Success)
        {
            NotificationService.ShowLight("成功", $"歌单《{model.Name}》创建成功！", NotificationType.Success);
            return;
        }

        await LoggerService.ErrorAsync($"添加《{model.Name}》歌单失败");
        PlayListItems.Remove(model);
        NotificationService.ShowLight("错误", $"创建歌单《{model.Name}》失败！", NotificationType.Error);
    }

    [RelayCommand]
    private async Task CreateAndAddToMusicList(IList items)
    {
        var list = await CreateMusicList();
        if (list == null || string.IsNullOrEmpty(list.Name))
            return;

        await AddToMusicList(items.Cast<MusicItemModel>().ToList(), list.Name);
    }

    [RelayCommand]
    private async Task<CreateMusicListViewModel?> CreateMusicList()
    {
        var options = new OverlayDialogOptions
        {
            Title = "新建歌单",
            Mode = DialogMode.Info,
            CanDragMove = true,
            CanResize = false,
        };

        var model = new CreateMusicListViewModel(options);

        await OverlayDialog.ShowCustomModal<CreateMusicList, CreateMusicListViewModel, object>(model, options: options);

        if (model.IsCancel || model.Name == null)
            return null;

        try
        {
            string coverFilePath = Path.Combine(MainConfig.PlaylistCoverSavePath, $"{model.Name}.png");
            var musicListItem = new MusicListModel(model.Name, model.Description, "", coverFilePath, model.Cover);

            await AddMusicListModel(musicListItem);

            if (model.Cover != null)
            {
                await FileOperation.SaveImageAsync(model.Cover, coverFilePath);
            }

            return model;
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"创建歌单《{model.Name}》失败: {ex.Message}");
            NotificationService.ShowLight(
                "错误",
                $"创建歌单《{model.Name}》失败！\n{ex.Message}",
                NotificationType.Error
            );
            return null;
        }
    }

    /// <summary>
    /// 批量添加音乐项到指定名称歌单
    /// </summary>
    /// <param name="musicItems">音乐项列表</param>
    /// <param name="listName">歌单名称</param>
    public async Task AddToMusicList(List<MusicItemModel> musicItems, string listName)
    {
        if (musicItems.Count == 0)
            return;

        var model = PlayListItems.FirstOrDefault(x => x.Name == listName);
        if (model == null)
            return;

        // 过滤掉已存在的音乐项
        var newItems = musicItems.Where(item => !model.MusicItems.Contains(item)).ToList();
        var existingItems = musicItems.Except(newItems).ToList();

        // 如果有已存在的音乐项，显示提示
        if (existingItems.Count > 0)
        {
            string existingTitles = string.Join("、", existingItems.Select(item => $"《{item.Title}》"));
            NotificationService.ShowLight(
                "提示",
                $"歌曲{existingTitles}已存在于歌单 {listName} 中！",
                NotificationType.Information
            );
        }

        if (newItems.Count == 0)
            return;

        // 在后台线程中并行处理所有添加操作
        var successItems = new List<MusicItemModel>();

        var addTasks = newItems.Select(async item =>
        {
            var playlistData = new Dictionary<string, string?>
            {
                [nameof(MusicListModel.Name)] = listName,
                [nameof(MusicItemModel.FilePath)] = item.FilePath,
            };

            var result = await DataBaseService.InsertDataAsync(playlistData, DataBaseService.Table.MUSICLISTS);
            if (result == DataBaseService.OperationResult.Success)
            {
                successItems.Add(item);
            }
        });

        await Task.WhenAll(addTasks);

        // 在UI线程中更新集合
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var item in successItems)
            {
                model.MusicItems.Add(item);
            }
        });

        var failedItems = newItems.Except(successItems).ToList();

        // 显示添加结果通知
        if (successItems.Count > 0)
        {
            string successTitles = string.Join("、", successItems.Select(item => $"《{item.Title}》"));
            NotificationService.ShowLight(
                "成功",
                $"已将歌曲{successTitles}添加到歌单：{listName}！",
                NotificationType.Success
            );
        }

        if (failedItems.Count > 0)
        {
            string failedTitles = string.Join("、", failedItems.Select(item => $"《{item.Title}》"));
            NotificationService.ShowLight("错误", $"添加歌曲{failedTitles}到歌单失败！", NotificationType.Error);
        }
    }

    /// <summary>
    /// 批量从指定名称歌单中移除音乐项
    /// </summary>
    /// <param name="musicItems">音乐项列表</param>
    /// <param name="listName">歌单名称</param>
    public async Task RemoveToMusicList(List<MusicItemModel> musicItems, string listName)
    {
        if (musicItems.Count == 0)
            return;

        var model = PlayListItems.FirstOrDefault(x => x.Name == listName);
        if (model == null)
            return;

        // 过滤出实际存在于歌单中的音乐项
        var existingItems = musicItems.Where(item => model.MusicItems.Contains(item)).ToList();
        if (existingItems.Count == 0)
            return;

        // 在后台线程中并行处理所有删除操作
        var successItems = new List<MusicItemModel>();

        var removeTasks = existingItems.Select(async item =>
        {
            var result = await DataBaseService.DeleteDataAsync(
                DataBaseService.Table.MUSICLISTS,
                nameof(MusicItemModel.FilePath),
                item.FilePath
            );

            if (result == DataBaseService.OperationResult.Success)
            {
                successItems.Add(item);
            }
        });

        await Task.WhenAll(removeTasks);

        // 在UI线程中更新集合
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var item in successItems)
            {
                model.MusicItems.Remove(item);
            }
        });

        var failedItems = existingItems.Except(successItems).ToList();

        // 显示移除结果通知
        if (successItems.Count > 0)
        {
            string successTitles = string.Join("、", successItems.Select(item => $"《{item.Title}》"));
            NotificationService.ShowLight(
                "成功",
                $"已将歌曲{successTitles}从歌单 {listName} 中移除！",
                NotificationType.Success
            );
        }

        if (failedItems.Count > 0)
        {
            string failedTitles = string.Join("、", failedItems.Select(item => $"《{item.Title}》"));
            NotificationService.ShowLight("错误", $"从歌单移除歌曲{failedTitles}失败！", NotificationType.Error);
        }
    }

    /// <summary>
    /// 编辑歌单模型
    /// </summary>
    /// <param name="oldName">原歌单名称</param>
    /// <param name="newModel">新的歌单模型</param>
    private static async Task<bool> EditMusicListModelInDataBase(string oldName, MusicListModel newModel)
    {
        // 如果歌单名称发生变化，需要更新数据库中的记录
        if (oldName != newModel.Name)
        {
            // 更新歌单信息表中的记录
            var result1 = await DataBaseService.UpdateDataAsync(
                newModel.Dump(),
                DataBaseService.Table.LISTINFO,
                nameof(MusicListModel.Name),
                oldName
            );

            // 更新歌单音乐关联表中的记录
            var result2 = await DataBaseService.UpdateDataAsync(
                new Dictionary<string, string?> { [nameof(MusicListModel.Name)] = newModel.Name },
                DataBaseService.Table.MUSICLISTS,
                nameof(MusicListModel.Name),
                oldName
            );

            return result1 == DataBaseService.OperationResult.Success
                && result2 == DataBaseService.OperationResult.Success;
        }

        // 如果歌单名称没有变化，只更新歌单信息
        var result = await DataBaseService.UpdateDataAsync(
            newModel.Dump(),
            DataBaseService.Table.LISTINFO,
            nameof(MusicListModel.Name),
            oldName
        );

        return result == DataBaseService.OperationResult.Success;
    }

    [RelayCommand]
    private static async Task EditMusicList(MusicListModel musicListItem)
    {
        var options = new OverlayDialogOptions
        {
            Title = "编辑歌单",
            Mode = DialogMode.Info,
            CanDragMove = true,
            CanResize = false,
        };

        (string name, string description, string? coverPath, var coverImage) = (
            musicListItem.Name,
            musicListItem.Description,
            musicListItem.CoverPath,
            musicListItem.CoverImage
        );

        var originalBitmap = await MusicExtractor.LoadOriginalBitmap(
            Path.Combine(MainConfig.PlaylistCoverSavePath, $"{musicListItem.Name}.png")
        );

        var model = new CreateMusicListViewModel(options, oldName: musicListItem.Name)
        {
            Name = musicListItem.Name,
            Description = musicListItem.Description,
            Cover = originalBitmap,
        };

        await OverlayDialog.ShowCustomModal<CreateMusicList, CreateMusicListViewModel, object>(model, options: options);

        if (model.IsCancel || model.Name == null)
            return;

        try
        {
            string newCoverPath = Path.Combine(MainConfig.PlaylistCoverSavePath, $"{model.Name}.png");

            // 更新歌单信息
            musicListItem.Name = model.Name;
            musicListItem.Description = model.Description;
            musicListItem.CoverPath = newCoverPath;

            // 如果封面图片发生变化，保存新图片
            if (model.Cover != coverImage && model.Cover != null)
            {
                musicListItem.CoverImage = model.Cover;
                await FileOperation.SaveImageAsync(model.Cover, newCoverPath, true);
            }
            // 如果名称变化但图片没变，重命名图片文件
            else if (name != model.Name && !string.IsNullOrEmpty(coverPath) && File.Exists(coverPath))
            {
                File.Move(coverPath, newCoverPath);
                // 更新图片缓存
                MusicExtractor.ImageCache.Remove($"歌单-{name}");
                MusicExtractor.ImageCache.Add($"歌单-{model.Name}", musicListItem.CoverImage);
            }

            // 更新数据库
            if (await EditMusicListModelInDataBase(name, musicListItem))
            {
                MainWindowViewModel.Instance.UpdateIconItems(
                    musicListItem.Id,
                    musicListItem.Name,
                    new BitmapIconSource(musicListItem.CoverImage)
                );

                NotificationService.ShowLight("成功", $"编辑歌单《{name}》成功！", NotificationType.Success);
            }
            else
            {
                throw new Exception("更新数据库失败");
            }
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"编辑歌单失败: {ex.Message}");
            // 恢复原始状态
            musicListItem.Name = name;
            musicListItem.Description = description;
            musicListItem.CoverPath = coverPath;
            NotificationService.ShowLight(
                "错误",
                $"编辑歌单《{model.Name}》失败！\n{ex.Message}",
                NotificationType.Error
            );
        }
    }

    [RelayCommand]
    private async Task DeleteMusicListModel(MusicListModel model)
    {
        var result = await MessageBox.ShowOverlayAsync(
            $"你真的要删除歌单《{model.Name}》吗?",
            "警告",
            icon: MessageBoxIcon.Warning,
            button: MessageBoxButton.YesNo
        );
        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            // 删除封面图片文件
            if (!string.IsNullOrEmpty(model.CoverPath) && File.Exists(model.CoverPath))
            {
                File.Delete(model.CoverPath);
            }

            // 从图片缓存中移除
            if (!string.IsNullOrEmpty(model.CoverPath))
            {
                MusicExtractor.ImageCache.Remove(model.CoverPath);
            }

            PlayListItems.Remove(model);

            await DataBaseService.DeleteDataAsync(
                DataBaseService.Table.LISTINFO,
                nameof(MusicListModel.Name),
                model.Name
            );
            await DataBaseService.DeleteDataAsync(
                DataBaseService.Table.MUSICLISTS,
                nameof(MusicListModel.Name),
                model.Name
            );

            MainWindowViewModel.Instance.RemoveTabPage(model.Id);

            NotificationService.ShowLight("成功", $"歌单《{model.Name}》删除成功！", NotificationType.Success);
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"删除歌单失败: {ex.Message}");
            NotificationService.ShowLight(
                "错误",
                $"删除歌单《{model.Name}》失败！\n{ex.Message}",
                NotificationType.Error
            );
            throw;
        }
    }

    [RelayCommand]
    private async Task AddToMusicList((MusicItemModel musicItem, string listName) argument)
    {
        await AddToMusicList([argument.musicItem], argument.listName);
    }

    private async Task LoadPlayListsAsync(ObservableCollection<MusicItemModel> allMusicItems)
    {
        // 从数据库加载所有歌单名称和描述
        var playlistInfos = await DataBaseService.LoadSpecifyFieldsAsync(
            DataBaseService.Table.LISTINFO,
            [
                nameof(MusicListModel.Name),
                nameof(MusicListModel.Description),
                nameof(MusicListModel.CoverPath),
                nameof(MusicListModel.LatestPlayedMusic),
            ],
            dict => new
            {
                Name = dict.TryGetValue(nameof(MusicListModel.Name), out object? name)
                    ? name.ToString() ?? string.Empty
                    : string.Empty,
                Description = dict.TryGetValue(nameof(MusicListModel.Description), out object? desc)
                    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    ? desc?.ToString()
                    : null,
                CoverPath = dict.TryGetValue(nameof(MusicListModel.CoverPath), out object? coverPath)
                    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    ? coverPath?.ToString()
                    : null,
                LatestPlayedMusic = dict.TryGetValue(nameof(MusicListModel.LatestPlayedMusic), out object? latest)
                    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    ? latest?.ToString()
                    : null,
            }
        );

        if (playlistInfos is null)
        {
            await LoggerService.ErrorAsync("加载歌单列表失败: 结果为 null");
            NotificationService.ShowLight("错误", "加载歌单列表失败！结果为 null", NotificationType.Error);

            return;
        }

        foreach (var info in playlistInfos.Where(info => !string.IsNullOrEmpty(info.Name)))
        {
            var musicListModel = new MusicListModel(
                info.Name,
                info.Description,
                info.LatestPlayedMusic,
                info.CoverPath
            );
            // 添加到列表中
            PlayListItems.Add(musicListModel);
        }
    }
}
