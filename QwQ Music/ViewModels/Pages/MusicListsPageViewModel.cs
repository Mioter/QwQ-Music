using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.UserControls;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.Pages;
using QwQ_Music.Views.UserControls;
using QwQ.Avalonia.Utilities.MessageBus;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.ViewModels.Pages;

public partial class MusicListsPageViewModel : ViewModelBase
{
    public ObservableCollection<MusicListModel> PlayListItems { get; set; } = [];

    public MusicListsPageViewModel()
    {
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        try
        {
            await LoadPlayListsAsync();
        }
        catch (Exception e)
        {
            await LoggerService.ErrorAsync($"初始化歌单模型出错 : {e.Message}");
        }
    }

    [RelayCommand]
    private static async Task OpenMusicLists(MusicListModel model)
    {
        if (!model.IsInitialized)
            await model.LoadAsync();

        await MessageBus
            .CreateMessage(
                new ViewChangeMessage(
                    model.Id,
                    model.Name,
                    model.CoverImage,
                    new ViewMusicListPage { DataContext = new ViewMusicListPageViewModel(model) }
                )
            )
            .AddReceivers<MainWindowViewModel>()
            .PublishAsync();
    }

    /// <summary>
    /// 添加歌单模型到歌单集合
    /// </summary>
    /// <param name="model">歌单模型</param>
    private async Task AddMusicListModel(MusicListModel model)
    {
        try
        {
            PlayListItems.Add(model);
            await DataBaseService.InsertDataAsync(model.Dump(), DataBaseService.Table.LISTINFO);
            NotificationService.ShowLight(
                new Notification("成功", $"歌单《{model.Name}》创建成功！"),
                NotificationType.Success
            );
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"添加《{model.Name}》歌单失败: {ex.Message}");
            PlayListItems.Remove(model);
            NotificationService.ShowLight(
                new Notification("错误", $"创建歌单《{model.Name}》失败！\n{ex.Message}"),
                NotificationType.Error
            );
            throw;
        }
    }

    [RelayCommand]
    private async Task CreateAndAddToMusicList(MusicItemModel musicItem)
    {
        var list = await CreateMusicList();
        if (list == null || string.IsNullOrEmpty(list.Name))
            return;

        await AddToMusicList(musicItem, list.Name);
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

        if (model is not { IsOk: true, Name: not null })
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
                new Notification("错误", $"创建歌单《{model.Name}》失败！\n{ex.Message}"),
                NotificationType.Error
            );
            return null;
        }
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

        if (model is not { IsOk: true, Name: not null })
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
                await FileOperation.SaveImageAsync(model.Cover, newCoverPath);
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
            await EditMusicListModelInDataBase(name, musicListItem);

            MainWindowViewModel.UpdateIconItems(
                musicListItem.Id,
                musicListItem.Name,
                new BitmapIconSource(musicListItem.CoverImage)
            );

            NotificationService.ShowLight(
                new Notification("成功", $"编辑歌单《{name}》成功！"),
                NotificationType.Success
            );
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"编辑歌单失败: {ex.Message}");
            // 恢复原始状态
            musicListItem.Name = name;
            musicListItem.Description = description;
            musicListItem.CoverPath = coverPath;
            NotificationService.ShowLight(
                new Notification("错误", $"编辑歌单《{model.Name}》失败！\n{ex.Message}"),
                NotificationType.Error
            );
        }
    }

    /// <summary>
    /// 编辑歌单模型
    /// </summary>
    /// <param name="oldName">原歌单名称</param>
    /// <param name="newModel">新的歌单模型</param>
    private static async Task EditMusicListModelInDataBase(string oldName, MusicListModel newModel)
    {
        try
        {
            // 如果歌单名称发生变化，需要更新数据库中的记录
            if (oldName != newModel.Name)
            {
                // 更新歌单信息表中的记录
                await DataBaseService.UpdateDataAsync(
                    newModel.Dump(),
                    DataBaseService.Table.LISTINFO,
                    nameof(MusicListModel.Name),
                    oldName
                );

                // 更新歌单音乐关联表中的记录
                await DataBaseService.UpdateDataAsync(
                    new Dictionary<string, string?> { [nameof(MusicListModel.Name)] = newModel.Name },
                    DataBaseService.Table.MUSICLISTS,
                    nameof(MusicListModel.Name),
                    oldName
                );
            }
            else
            {
                // 如果歌单名称没有变化，只更新歌单信息
                await DataBaseService.UpdateDataAsync(
                    newModel.Dump(),
                    DataBaseService.Table.LISTINFO,
                    nameof(MusicListModel.Name),
                    oldName
                );
            }
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"更新数据库失败: {ex.Message}");
            throw;
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

            await MessageBus
                .CreateMessage(new ViewChangeMessage(model.Id, model.Name, model.CoverImage, null, true))
                .AddReceivers<MainWindowViewModel>()
                .PublishAsync();

            NotificationService.ShowLight(
                new Notification("成功", $"歌单《{model.Name}》删除成功！"),
                NotificationType.Success
            );
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"删除歌单失败: {ex.Message}");
            NotificationService.ShowLight(
                new Notification("错误", $"删除歌单《{model.Name}》失败！\n{ex.Message}"),
                NotificationType.Error
            );
            throw;
        }
    }

    [RelayCommand]
    private async Task AddToMusicList((MusicItemModel musicItem, string listName) argument)
    {
        await AddToMusicList(argument.musicItem, argument.listName);
    }

    /// <summary>
    /// 添加音乐项到指定名称歌单
    /// </summary>
    /// <param name="musicItem">音乐项</param>
    /// <param name="listName">歌单名称</param>
    public async Task AddToMusicList(MusicItemModel musicItem, string listName)
    {
        try
        {
            var model = PlayListItems.FirstOrDefault(x => x.Name == listName);

            if (model == null || model.MusicItems.Contains(musicItem))
                return;

            model.MusicItems.Add(musicItem);
            var playlistData = new Dictionary<string, string?>
            {
                [nameof(MusicListModel.Name)] = listName,
                [nameof(MusicItemModel.FilePath)] = musicItem.FilePath,
            };
            await DataBaseService.InsertDataAsync(playlistData, DataBaseService.Table.MUSICLISTS);

            NotificationService.ShowLight(
                new Notification("成功", $"已将歌曲《{musicItem.Title}》添加到歌单 : {listName}！"),
                NotificationType.Success
            );
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"添加音乐《{musicItem.Title}》到歌单失败: {ex.Message}");
            NotificationService.ShowLight(
                new Notification("错误", $"添加音乐《{musicItem.Title}》到歌单失败！\n{ex.Message}"),
                NotificationType.Error
            );
            throw;
        }
    }

    [RelayCommand]
    private async Task RemoveToMusicList((MusicItemModel musicItem, string listName) argument)
    {
        await RemoveToMusicList(argument.musicItem, argument.listName);
    }

    /// <summary>
    /// 从指定名称歌单中移除音乐项
    /// </summary>
    /// <param name="musicItem">音乐项</param>
    /// <param name="listName">歌单名称</param>
    public async Task RemoveToMusicList(MusicItemModel musicItem, string listName)
    {
        try
        {
            var model = PlayListItems.FirstOrDefault(x => x.Name == listName);

            if (model == null || !model.MusicItems.Contains(musicItem))
                return;

            model.MusicItems.Remove(musicItem);
            await DataBaseService.DeleteDataAsync(
                DataBaseService.Table.MUSICLISTS,
                nameof(MusicItemModel.FilePath),
                musicItem.FilePath
            );

            NotificationService.ShowLight(
                new Notification("成功", $"已将歌曲《{musicItem.Title}》从歌单 {listName} 中移除！"),
                NotificationType.Success
            );
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"从歌单移除音乐失败: {ex.Message}");
            NotificationService.ShowLight(
                new Notification("错误", $"从歌单《{musicItem.Title}》移除音乐失败！\n{ex.Message}"),
                NotificationType.Error
            );
            throw;
        }
    }

    private async Task LoadPlayListsAsync()
    {
        try
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

            foreach (var info in playlistInfos.Where(info => !string.IsNullOrEmpty(info.Name)))
            {
                // 添加到列表中
                PlayListItems.Add(
                    new MusicListModel(info.Name, info.Description, info.LatestPlayedMusic, info.CoverPath)
                );
            }
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"加载歌单列表失败: {ex.Message}");
            NotificationService.ShowLight(
                new Notification("错误", $"加载歌单列表失败！\n{ex.Message}"),
                NotificationType.Error
            );
            throw;
        }
    }
}
