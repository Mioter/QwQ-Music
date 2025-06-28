using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.UserControls;

public partial class AlbumDetailsPanelViewModel : DataGridViewModelBase
{
    [ObservableProperty]
    public partial AlbumItemModel AlbumItemModel { get; private set; } =
        new("Error", "#警告！你已进入未知空域，请立即离开此处（");

    [ObservableProperty]
    public partial Bitmap CoverImage { get; set; } = MusicExtractor.DefaultCover;

    public AlbumDetailsPanelViewModel UpdateAlbumItemModel(AlbumItemModel albumItemModel)
    {
        AlbumItemModel = albumItemModel;
        MusicItems = new ObservableCollection<MusicItemModel>(SearchMusicItems(albumItemModel));

        if (MusicItems.Count == 0)
        {
            NotificationService.ShowLight("警告", "当前专辑内容为空，可能是音乐已经被删除！", NotificationType.Warning);
        }

        _ = UpdateCoverImage(MusicItems.First()).ConfigureAwait(false);

        if (AlbumItemModel.Description != null)
        {
            return this;
        }

        if (AlbumItemModel.Name == "未知专辑")
        {
            AlbumItemModel.Description = "咱不知道它专辑，获取专辑信息这事做不到呜呜！";
            return this;
        }

        AlbumItemModel.Description = "专辑信息等待获取中...";

        _ = GetAlbumDetailByNameAsync(albumItemModel).ConfigureAwait(false);

        return this;
    }

    private async Task UpdateCoverImage(MusicItemModel musicItem)
    {
        string? currentCoverPath = musicItem.CoverPath;
        bool shouldRetry;

        do
        {
            shouldRetry = false;

            if (currentCoverPath != null)
            {
                var bitmap = await MusicExtractor.LoadOriginalBitmap(currentCoverPath).ConfigureAwait(false);
                if (bitmap != null)
                {
                    CoverImage = bitmap;
                    return;
                }
            }

            // 尝试从音频文件中提取封面
            string? newCoverPath = await MusicExtractor
                .ExtractAndSaveCoverFromAudioAsync(musicItem.FilePath)
                .ConfigureAwait(false);
            if (newCoverPath != null)
            {
                currentCoverPath = newCoverPath;
                musicItem.CoverPath = Path.GetFileName(currentCoverPath); // 更新模型中的路径
                shouldRetry = true; // 重试加载新路径的封面
            }
            else
            {
                CoverImage = MusicExtractor.DefaultCover;
            }
        } while (shouldRetry);
    }

    private static List<MusicItemModel> SearchMusicItems(AlbumItemModel albumItem)
    {
        // 找到该专辑对应的所有音乐项
        var albumMusicItems = MusicPlayerViewModel
            .Instance.MusicItems.Where(music => music.Album == albumItem.Name && music.Artists == albumItem.Artist)
            .ToList();

        return albumMusicItems;
    }

    private async Task GetAlbumDetailByNameAsync(AlbumItemModel album)
    {
        try
        {
            using var crawler = new NetEaseAlbumCrawler();
            var albumDetail = await crawler.GetAlbumDetailByNameAsync(album.Name, album.Artist);

            AlbumItemModel.Description = albumDetail.Description;
            AlbumItemModel.PublishTime = albumDetail.PublishTime;
            AlbumItemModel.Company = albumDetail.Company;
        }
        catch (NetEaseAlbumCrawlerException ex)
        {
            await LoggerService.ErrorAsync($"爬虫异常: {ex.Message}");
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"其他异常: {ex.Message}");
        }
    }
}
