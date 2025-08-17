using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Panels;

public partial class AllMusicListPanelViewModel : ViewModelBase
{
    private readonly AvaloniaList<MusicListModel> _filterSource = [];

    public static MusicPlayerViewModel MusicPlayerViewModel => MusicPlayerViewModel.Default;

    public static MusicListsManager MusicListsManager => MusicListsManager.Default;

    [ObservableProperty] public partial AvaloniaList<MusicListModel> MusicLists { get; set; } = MusicListsManager.MusicLists;

    public string? SearchText
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            OnSearchTextChanged(value);
        }
    }

    private void OnSearchTextChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            MusicLists = MusicListsManager.MusicLists;

            return;
        }

        var source = string.IsNullOrEmpty(value)
            ? MusicListsManager.MusicLists
            : MusicListsManager.MusicLists.Where(MatchesSearchCriteria);

        _filterSource.Clear();
        _filterSource.AddRange(source);
        MusicLists = _filterSource;

        return;

        bool MatchesSearchCriteria(MusicListModel item)
        {
            return item.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
             || item.Description.Contains(value, StringComparison.OrdinalIgnoreCase);
        }
    }

    [RelayCommand]
    private static async Task TogglePlaylist(MusicListModel? musicList)
    {
        if (string.IsNullOrEmpty(musicList?.IdStr))
            return;

        using var musicListMapRepository = new MusicListItemRepository(musicList.IdStr, StaticConfig.DatabasePath);
        var paths = await Task.Run(() => musicListMapRepository.GetAll());

        if (paths.Count <= 0)
            return;

        // 使用 LINQ 简化过滤逻辑
        var musicItems = new AvaloniaList<MusicItemModel>(
            MusicItemManager.Default.MusicItems.Where(item => paths.Contains(item.FilePath))
        );

        MusicPlayListManager.Default.Toggle(musicItems);

        await MusicPlayerViewModel.PlayThisMusic(musicItems.First());
    }
}
