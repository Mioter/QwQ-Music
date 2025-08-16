using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Panels;

public partial class MusicListDetailsPanelViewModel : DataGridViewModelBase
{
    private readonly AvaloniaList<MusicItemModel> _filterSource = [];

    [ObservableProperty]
    public partial MusicListModel MusicListModel { get; set; } = new()
    {
        Name = "Error",
        Description = "#警告！你已进入未知空域，请立即离开此处（",
        IdStr = "$Default",
    };

    public Bitmap CoverImage { get; set; } = CacheManager.Default;

    protected override void OnSearchTextChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            MusicItems = MusicListsManager.CurrentMusicList.MusicItems;

            return;
        }

        var source = string.IsNullOrEmpty(value)
            ? MusicListsManager.CurrentMusicList.MusicItems
            : MusicListsManager.CurrentMusicList.MusicItems.Where(MatchesSearchCriteria);

        _filterSource.Clear();
        _filterSource.AddRange(source);
        MusicItems = _filterSource;

        return;

        bool MatchesSearchCriteria(MusicItemModel item)
        {
            return item.Title.Contains(value, StringComparison.OrdinalIgnoreCase)
             || item.Artists.Contains(value, StringComparison.OrdinalIgnoreCase)
             || item.Album.Contains(value, StringComparison.OrdinalIgnoreCase);
        }
    }

    public async void UpdateMusicListModel(MusicListModel musicListModel)
    {
        try
        {
            MusicListModel = musicListModel;
            UpdateCoverImage(musicListModel);

            await using var musicListMapRepository = new MusicListItemRepository(musicListModel.IdStr, StaticConfig.DatabasePath);
            var paths = await musicListMapRepository.GetAllAsync();

    
            MusicListsManager.CurrentMusicList.IdStr = musicListModel.IdStr;

            if (paths.Count > 0)
            {
                // 使用 LINQ 简化过滤逻辑
                MusicListsManager.CurrentMusicList.MusicItems.Clear();
                MusicListsManager.CurrentMusicList.MusicItems.AddRange(MusicItemManager.Default.MusicItems.Where(item => paths.Contains(item.FilePath)));
            }

            MusicItems = MusicListsManager.CurrentMusicList.MusicItems;
        }
        catch (Exception e)
        {
            await LoggerService.ErrorAsync($"更新歌单信息时发生错误！\n{e.Message}\n{e.StackTrace}");
            NotificationService.Error($"更新歌单信息时发生错误！\n{e.Message}");
        }
    }
    
    private async void UpdateCoverImage(MusicListModel musicList)
    {
        try
        {
            if (musicList.CoverId == null)
                return;

            var bitmap = await MusicExtractor.LoadBitmapFromFileAsync(MusicExtractor.GetMusicListCoverFullPath(musicList.CoverId));

            if (bitmap == null)
                return;

            CoverImage = bitmap;
            OnPropertyChanged(nameof(CoverImage));
        }
        catch (Exception e)
        {
            NotificationService.Error("加载大专辑封面时出错！");
            await LoggerService.ErrorAsync($"更新专辑详情页封面时出错 : \n{e.Message}\n{e.StackTrace}");
        }
    }

    [RelayCommand]
    private async Task PlayMusicList()
    {
        if (MusicItems.Count <= 0)
            return;

        await MusicPlayList.Toggle(MusicItems);

        await MusicPlayerViewModel.PlayThisMusic(MusicItems.First());
    }

    [RelayCommand]
    private static void BackAllAlMusicList()
    {
        NavigateService.NavigateTo("全部歌单");
    }
}
