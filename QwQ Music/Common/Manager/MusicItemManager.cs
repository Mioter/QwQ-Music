using System;
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

public partial class MusicItemManager : ObservableObject
{
    public static MusicItemManager Default { get; } = new();

    public AvaloniaList<MusicItemModel> MusicItems { get; set; } = [];

    public int Count => MusicItems.Count;

    public async Task Initialize()
    {
        try
        {
            using var musicItemRepository = new MusicItemRepository(StaticConfig.DatabasePath);

            MusicItems.AddRange(await Task.Run(() => musicItemRepository.GetAll()));

            if (MusicItems.Count != 0)
                return;

            NotificationService.Info("好像...一首歌都没有（ \n " +
                "Tips : 可以点击右上角加号从文件中添加音乐哦！");
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"初始化音乐项出错: \n{ex.Message}\n{ex.StackTrace}");
            NotificationService.Error($"初始化音乐项出错: {ex.Message}");
        }
    }

    public async Task AddAsync(IList<MusicItemModel> musicItems)
    {
        var successItems = new List<MusicItemModel>();

        await Task.Run(() =>
        {
            using var repo = new MusicItemRepository(StaticConfig.DatabasePath);

            foreach (var musicItem in musicItems)
            {
                try
                {
                    musicItem.InsertTime = DateTime.UtcNow;
                    repo.Insert(musicItem);

                    successItems.Add(musicItem);
                }
                catch (Exception e)
                {
                    LoggerService.Error($"歌曲{musicItem.Title}保存到数据库失败！\n{e.Message}\n{e.StackTrace}");

                    NotificationService.Error($"歌曲{musicItem.Title}保存到数据库失败！\n{e.Message}");
                }
            }
        });

        // 批量添加到UI集合
        MusicItems.InsertRange(0, successItems);

        var failedItems = musicItems.Except(successItems).ToList();

        if (musicItems.Count > 0)
        {
            string existingTitles = string.Join("、", musicItems.Select(items => $"《{items.Title}》")
            );

            NotificationService.Success($"歌曲 {existingTitles} 添加成功啦~");
        }

        if (failedItems.Count > 0)
        {
            string failedTitles = string.Join("、", failedItems.Select(item => $"《{item.Title}》"));
            NotificationService.Error($"删除 {failedTitles} 添加失败了！");
        }
    }

    public static void Update(MusicItemModel musicItem)
    {
        using var repo = new MusicItemRepository(StaticConfig.DatabasePath);

        try
        {
            repo.Update(musicItem);
        }
        catch (Exception e)
        {
            LoggerService.Error($"更新歌曲{musicItem.Title}到数据库失败！\n{e.Message}\n{e.StackTrace}");

            NotificationService.Error($"更新歌曲{musicItem.Title}到数据库失败！\n{e.Message}");
        }
    }

    public static void UpdatePlayProgress(string filePath, TimeSpan current)
    {
        using var repo = new MusicItemRepository(StaticConfig.DatabasePath);

        repo.Update(filePath, new Dictionary<string, object?>
        {
            [nameof(MusicItemModel.Current)] = current.ToString(),
        });
    }

    public static void UpdateCoverColors(string filePath, string[] colorList)
    {
        using var musicItemRepository = new MusicItemRepository(StaticConfig.DatabasePath);

        musicItemRepository.Update(filePath, new Dictionary<string, object?>
        {
            [nameof(MusicItemModel.CoverColors)] = string.Join("、", colorList),
        });
    }

    public async Task<List<MusicItemModel>?> Delete(IList<MusicItemModel> musicItems)
    {
        if (musicItems.Count == 0)
            return null;

        // 构建确认提示信息
        string titles = string.Join("、", musicItems.Select(item => $"《{item.Title}》"));

        var result = await MessageBox.ShowOverlayAsync(
            $"你真的要删除以下音乐吗？\n{titles}",
            "警告",
            icon: MessageBoxIcon.Warning,
            button: MessageBoxButton.YesNo
        );

        if (result != MessageBoxResult.Yes)
            return null;

        var successItems = new List<MusicItemModel>();

        await Task.Run(() =>
        {
            using var repo = new MusicItemRepository(StaticConfig.DatabasePath);

            foreach (var musicItem in musicItems)
            {
                try
                {
                    repo.Delete(musicItem.FilePath);
                    successItems.Add(musicItem);
                }
                catch (Exception e)
                {
                    LoggerService.Error($"从数据库中删除歌曲{musicItem.Title}失败！\n{e.Message}\n{e.StackTrace}");

                    NotificationService.Error($"歌曲{musicItem.Title}删除失败！\n{e.Message}");
                }
            }
        });

        MusicItems.RemoveAll(successItems);

        var failedItems = musicItems.Except(successItems).ToList();

        // 显示删除结果通知
        if (successItems.Count > 0)
        {
            string successTitles = string.Join("、", successItems.Select(item => $"《{item.Title}》"));
            NotificationService.Success($"{successTitles}已经从音乐库中移除了！");
        }

        if (failedItems.Count > 0)
        {
            string failedTitles = string.Join("、", failedItems.Select(item => $"《{item.Title}》"));
            NotificationService.Error($"删除{failedTitles}失败了！");
        }

        return successItems;
    }

    [RelayCommand]
    public static async Task ShowDetailedInfo(MusicItemModel musicItem)
    {
        var options = new OverlayDialogOptions
        {
            Title = "详细信息",
            Buttons = DialogButton.None,
            CanLightDismiss = true,
            Mode = DialogMode.Info,
            CanDragMove = true,
            CanResize = false,
        };
        var tagExtensions = await Task.Run(() =>MusicExtractor.ExtractExtensionsInfo(musicItem.FilePath));
        await OverlayDialog.ShowModal<AudioDetailedInfo, AudioDetailedInfoViewModel>(
            new AudioDetailedInfoViewModel(
                musicItem,
                tagExtensions
            ).MoreDetailedInfor(),
            options: options
        );
    }

    [RelayCommand]
    public static void OpenInExplorer(MusicItemModel musicItem)
    {
        if (string.IsNullOrEmpty(musicItem.FilePath) || !File.Exists(musicItem.FilePath))
        {
            NotificationService.Error($"无法打开《{musicItem.Title}》文件位置：{musicItem.FilePath}文件不存在");

            return;
        }

        try
        {
            FileOperationService.OpenInFileManager(musicItem.FilePath);
        }
        catch (Exception e)
        {
            LoggerService.Error($"打开文件位置失败: {e.Message}");
            NotificationService.Error($"打开《{musicItem.Title}》文件位置时报错：{e.Message}");
        }
    }
}
