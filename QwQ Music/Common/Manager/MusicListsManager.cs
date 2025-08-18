using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Dialogs;
using QwQ_Music.Views;
using QwQ_Music.Views.Dialogs;
using Ursa.Controls;

namespace QwQ_Music.Common.Manager;

public partial class MusicListsManager : ObservableObject
{
    private MusicListsManager()
    {
        Initialize();
    }

    public static MusicListsManager Default { get; } = new();

    public AvaloniaList<MusicListModel> MusicLists { get; set; } = [];

    public CurrentMusicList CurrentMusicList { get; set; } = new();

    private async void Initialize()
    {
        try
        {
            using var musicListMapRepository = new MusicListMapRepository(StaticConfig.DatabasePath);

            MusicLists.AddRange(await Task.Run(() => musicListMapRepository.GetAll()));
        }
        catch (Exception e)
        {
            NotificationService.Error("歌单信息加载失败！\n" +
                $"{e.Message}");

            await LoggerService.ErrorAsync("歌单信息加载失败！\n" +
                $"{e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     添加歌单信息
    /// </summary>
    /// <param name="model">歌单模型</param>
    private void AddMusicList(MusicListModel model)
    {
        using var musicListMapRepository = new MusicListMapRepository(StaticConfig.DatabasePath);
        musicListMapRepository.Insert(model);

        MusicLists.Add(model);
        NotificationService.Success("成功", $"歌单《{model.Name}》创建成功！");
    }

    [RelayCommand]
    private async Task CreatePlaylistWithMusicItem(IList items)
    {
        var list = await CreateMusicList();

        if (list?.IdStr != null)
        {
            await AddToMusicList(items.Cast<MusicItemModel>().ToList(), list);
        }
    }

    [RelayCommand]
    private async Task<MusicListModel?> CreateMusicList()
    {
        var options = new OverlayDialogOptions
        {
            Title = "新建歌单",
            Mode = DialogMode.Info,
            CanDragMove = true,
            CanResize = false,
        };

        var model = await OverlayDialog.ShowCustomModal<CreateMusicList, CreateMusicListViewModel, MusicListModel>(
            new CreateMusicListViewModel(options.Title), options: options);

        if (model == null)
            return null;

        try
        {
            AddMusicList(model);

            return model;
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"创建《{model.Name}》歌单失败！\n" +
                $"{ex.Message}\n{ex.StackTrace}");

            NotificationService.Error($"创建歌单《{model.Name}》失败！\n" +
                $"{ex.Message}");

            return null;
        }
    }

    /// <summary>
    ///     批量添加音乐项到指定名称歌单
    /// </summary>
    /// <param name="musicItems">音乐项列表</param>
    /// <param name="musicList">歌单项</param>
    public async Task AddToMusicList(List<MusicItemModel> musicItems, MusicListModel musicList)
    {
        if (musicItems.Count == 0)
            return;

        using var repo = new MusicListItemRepository(musicList.IdStr, StaticConfig.DatabasePath);

        // 过滤掉已存在的音乐项
        var newItems = musicItems.Where(item => !repo.Contains(item.FilePath)).ToList();

        var existingItems = musicItems.Except(newItems).ToList();

        // 如果有已存在的音乐项，显示提示
        if (existingItems.Count > 0)
        {
            string existingTitles = string.Join("、", existingItems.Select(item => $"《{item.Title}》"));
            NotificationService.Info("提示", $"歌曲{existingTitles}已存在于歌单 {musicList.Name} 中！");
        }

        var failedItems = new List<MusicItemModel>();

        await Task.Run(() =>
        {
            foreach (var item in newItems)
            {
                try
                {
                    repo.Add(item.FilePath);
                }
                catch (Exception)
                {
                    failedItems.Add(item);
                }
            }
        });

        var successItems = newItems.Except(failedItems).ToList();

        // 显示添加结果通知
        if (successItems.Count > 0)
        {
            string successTitles = string.Join("、", successItems.Select(item => $"《{item.Title}》"));
            NotificationService.Success($"已将歌曲{successTitles}添加到歌单：{musicList.Name}！");

            if (musicList.IdStr == CurrentMusicList.IdStr)
            {
                CurrentMusicList.MusicItems.AddRange(successItems);
            }
        }

        if (failedItems.Count > 0)
        {
            string failedTitles = string.Join("、", failedItems.Select(item => $"《{item.Title}》"));
            NotificationService.Error($"添加歌曲{failedTitles}到歌单失败！");
        }
    }

    /// <summary>
    ///     批量从指定名称歌单中移除音乐项
    /// </summary>
    /// <param name="musicItems">音乐项列表</param>
    /// <param name="musicList">歌单项</param>
    public async Task RemoveToMusicList(List<MusicItemModel> musicItems, MusicListModel musicList)
    {
        if (musicItems.Count == 0)
            return;

        var failedItems = new List<MusicItemModel>();

        await Task.Run(() =>
        {
            using var repo = new MusicListItemRepository(musicList.IdStr, StaticConfig.DatabasePath);

            foreach (var item in musicItems)
            {
                try
                {
                    repo.Remove(item.FilePath);
                }
                catch (Exception)
                {
                    failedItems.Add(item);
                }
            }
        });

        var successItems = musicItems.Except(failedItems).ToList();

        // 显示移除结果通知
        if (successItems.Count > 0)
        {
            string successTitles = string.Join("、", successItems.Select(item => $"《{item.Title}》"));

            NotificationService.Success($"已将歌曲{successTitles}从歌单 {musicList.Name} 中移除！");

            if (musicList.IdStr == CurrentMusicList.IdStr)
            {
                CurrentMusicList.MusicItems.RemoveAll(successItems);
            }
        }

        if (failedItems.Count > 0)
        {
            string failedTitles = string.Join("、", failedItems.Select(item => $"《{item.Title}》"));
            NotificationService.Error($"从歌单移除歌曲{failedTitles}失败！");
        }
    }

    [RelayCommand]
    private async Task DeleteMusicList(MusicListModel musicList)
    {
        var result = await MessageBox.ShowOverlayAsync(
            $"你真的要删除歌单《{musicList.Name}》吗?",
            "警告",
            icon: MessageBoxIcon.Warning,
            button: MessageBoxButton.YesNo
        );

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            await Task.Run(() =>
            {
                using var repo = new MusicListMapRepository(StaticConfig.DatabasePath);
                repo.Delete(musicList.IdStr);

                // 删除封面图片文件
                if (!string.IsNullOrEmpty(musicList.CoverId))
                {
                    string coverFullPath = MusicExtractor.GetMusicListCoverFullPath(musicList.CoverId);

                    if (File.Exists(coverFullPath))
                    {
                        File.Delete(MusicExtractor.GetMusicListCoverFullPath(musicList.CoverId));
                    }
                }

                // 从图片缓存中移除
                if (!string.IsNullOrEmpty(musicList.CoverId))
                {
                    CacheManager.ImageCache.Remove(musicList.CoverId);
                }
            });

            MusicLists.Remove(musicList);

            NotificationService.Success($"歌单《{musicList.Name}》删除成功！");
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"删除歌单失败:\n{ex.Message}\n{ex.StackTrace}");

            NotificationService.Error($"删除歌单《{musicList.Name}》失败！\n{ex.Message}");

            throw;
        }
    }

    [RelayCommand]
    private async Task AddToMusicList((MusicItemModel musicItem, MusicListModel musicList) argument)
    {
        await AddToMusicList([argument.musicItem], argument.musicList);
    }

    [RelayCommand]
    private static async Task EditMusicListName(MusicListModel musicList)
    {
        var options = new OverlayDialogOptions
        {
            Title = "修改名称",
            Buttons = DialogButton.OKCancel,
            Mode = DialogMode.Info,
            CanDragMove = true,
        };

        string? result = await OverlayDialog.ShowCustomModal<EditText, EditTextViewModel, string>(new EditTextViewModel(musicList.Name, options.Title, 64), options: options);

        if (string.IsNullOrEmpty(result))
            return;

        try
        {
            using (var repo = new MusicListMapRepository(StaticConfig.DatabasePath))
            {
                repo.Update(musicList.IdStr, [nameof(musicList.Name)], [musicList.Name]);
            }

            musicList.Name = result;

            NotificationService.Success($"修改{musicList.Name}的名称成功了");
        }
        catch (Exception e)
        {
            await LoggerService.ErrorAsync($"修改 {musicList.Name} 的名称失败了:\n{e.Message}\n{e.StackTrace}");

            NotificationService.Error($"修改 {musicList.Name} 的名称失败了");
        }
    }

    [RelayCommand]
    private static async Task EditMusicListDescription(MusicListModel musicList)
    {
        var options = new OverlayDialogOptions
        {
            Title = "修改描述",
            Buttons = DialogButton.OKCancel,
            Mode = DialogMode.Info,
            CanDragMove = true,
        };

        string? result = await OverlayDialog.ShowCustomModal<EditText, EditTextViewModel, string>(new EditTextViewModel(musicList.Description, options.Title), options: options);

        if (string.IsNullOrEmpty(result))
            return;

        try
        {
            using (var repo = new MusicListMapRepository(StaticConfig.DatabasePath))
            {
                repo.Update(musicList.IdStr, [nameof(musicList.Description)], [musicList.Description]);
            }

            musicList.Name = result;

            NotificationService.Success($"修改{musicList.Name}的名称成功了");
        }
        catch (Exception e)
        {
            await LoggerService.ErrorAsync($"修改 {musicList.Name} 的名称失败了:\n{e.Message}\n{e.StackTrace}");

            NotificationService.Error($"修改 {musicList.Name} 的名称失败了");
        }
    }

    [RelayCommand]
    private static async Task EditMusicListCover(MusicListModel musicList)
    {
        if (App.TopLevel == null)
            return;

        var options = new ShowWindowOptions
        {
            Title = "裁剪图片",
            IsRestoreButtonVisible = false,
            IsFullScreenButtonVisible = false,
        };

        var bitmap = await FileOperationService.OpenImageFile(App.TopLevel);

        if (bitmap == null)
            return;

        var newCover = await WindowBox.ShowDialog<ImageCropping, Bitmap>(new ImageCroppingViewModel(bitmap), options, App.TopLevel);

        if (newCover == null)
            return;

        musicList.CoverImage = newCover;

        if (musicList.CoverId == null)
            return;

        if (await FileOperationService.SaveImageAsync(newCover, MusicExtractor.GetMusicListCoverFullPath(musicList.CoverId)))
        {
            NotificationService.Success($"修改歌词 {musicList.Name} 的图标成功啦~");
        }
        else
        {
            NotificationService.Error($"修改歌词 {musicList.Name} 的图标失败啦~");
        }
    }
}
