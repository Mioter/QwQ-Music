using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Dialogs;
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

    private void Initialize()
    {
        LoadMusicListAsync();
    }

    private async void LoadMusicListAsync()
    {
        try
        {
            await using var musicListMapRepository = new MusicListMapRepository(StaticConfig.DatabasePath);

            MusicLists.AddRange(await musicListMapRepository.GetAllAsync());
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
    private async Task AddMusicList(MusicListModel model)
    {
        try
        {
            await using var musicListMapRepository = new MusicListMapRepository(StaticConfig.DatabasePath);
            await musicListMapRepository.InsertAsync(model);

            MusicLists.Add(model);
            NotificationService.Success("成功", $"歌单《{model.Name}》创建成功！");
        }
        catch (Exception e)
        {
            await LoggerService.ErrorAsync($"创建《{model.Name}》歌单失败！\n" +
                $"{e.Message}");

            NotificationService.Error($"创建歌单《{model.Name}》失败！\n" +
                $"{e.Message}\n{e.StackTrace}");
        }
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

        var musicListModel = await OverlayDialog.ShowCustomModal<CreateMusicList, CreateMusicListViewModel, MusicListModel>(
            new CreateMusicListViewModel(options.Title), options: options);

        if (musicListModel == null)
            return null;

        try
        {
            if (musicListModel is { CoverId: not null, CoverImage: not null })
            {
                string coverFilePath = Path.Combine(StaticConfig.MusicListCoverSavePath, $"{musicListModel.CoverId}.png");
                await FileOperationService.SaveImageAsync(musicListModel.CoverImage, coverFilePath);
            }

            await AddMusicList(musicListModel);

            return musicListModel;
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"创建歌单《{musicListModel.Name}》失败:\n {ex.Message}");

            NotificationService.Error($"创建歌单《{musicListModel.Name}》失败！\n{ex.Message}");

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

        await using var musicListMapRepository = new MusicListItemRepository(musicList.IdStr, StaticConfig.DatabasePath);

        // 过滤掉已存在的音乐项
        List<MusicItemModel> newItems = [];

        foreach (var item in musicItems)
        {
            if (!await musicListMapRepository.ContainsAsync(item.FilePath))
            {
                newItems.Add(item);
            }
        }

        var existingItems = musicItems.Except(newItems).ToList();

        // 如果有已存在的音乐项，显示提示
        if (existingItems.Count > 0)
        {
            string existingTitles = string.Join("、", existingItems.Select(item => $"《{item.Title}》"));
            NotificationService.Info("提示", $"歌曲{existingTitles}已存在于歌单 {musicList.Name} 中！");
        }

        var failedItems = new List<MusicItemModel>();

        foreach (var item in newItems)
        {
            try
            {
                await musicListMapRepository.AddAsync(item.FilePath);
            }
            catch (Exception)
            {
                failedItems.Add(item);
            }
        }

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

        await using var musicListMapRepository = new MusicListItemRepository(musicList.IdStr, StaticConfig.DatabasePath);

        var failedItems = new List<MusicItemModel>();

        foreach (var item in musicItems)
        {
            try
            {
                await musicListMapRepository.RemoveAsync(item.FilePath);
            }
            catch (Exception)
            {
                failedItems.Add(item);
            }
        }

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
    private async Task DeleteMusicList(MusicListModel model)
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
            if (!string.IsNullOrEmpty(model.CoverId) && File.Exists(model.CoverId))
            {
                File.Delete(model.CoverId);
            }

            // 从图片缓存中移除
            if (!string.IsNullOrEmpty(model.CoverId))
            {
                CacheManager.ImageCache.Remove(model.CoverId);
            }

            MusicLists.Remove(model);

            NotificationService.Success($"歌单《{model.Name}》删除成功！");
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"删除歌单失败: {ex.Message}");

            NotificationService.Error($"删除歌单《{model.Name}》失败！\n{ex.Message}");

            throw;
        }
    }

    [RelayCommand]
    private async Task AddToMusicList((MusicItemModel musicItem, MusicListModel musicList) argument)
    {
        await AddToMusicList([argument.musicItem], argument.musicList);
    }
}
