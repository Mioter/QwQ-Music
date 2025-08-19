using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.ViewModels.Panels;

public partial class AllAlbumsPanelViewModel : ObservableObject
{
    private readonly AvaloniaList<AlbumItemModel> _allAlbumList = [];

    public AllAlbumsPanelViewModel()
    {
        RebuildAllAlbumItems();
        OnSearchTextChanged(SearchText);
        MusicItemManager.Default.MusicItems.CollectionChanged += MusicItemsOnCollectionChanged;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_OnProcessExit;
    }

    public StyleConfig StyleConfig { get; } = ConfigManager.UiConfig.StyleConfig;

    [ObservableProperty] public partial AvaloniaList<AlbumItemModel> AlbumItems { get; set; } = [];

    [ObservableProperty] public partial AlbumItemModel? SelectedAlbumItem { get; set; }

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

    private void CurrentDomain_OnProcessExit(object? sender, EventArgs e)
    {
        MusicItemManager.Default.MusicItems.CollectionChanged -= MusicItemsOnCollectionChanged;
        AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_OnProcessExit;
    }

    private void MusicItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                    foreach (MusicItemModel newItem in e.NewItems)
                    {
                        AddOrUpdateAlbumItem(newItem);
                    }

                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                    foreach (MusicItemModel oldItem in e.OldItems)
                    {
                        RemoveAlbumItemIfNecessary(oldItem);
                    }

                break;

            case NotifyCollectionChangedAction.Replace:
                if (e.OldItems != null)
                    foreach (MusicItemModel oldItem in e.OldItems)
                    {
                        RemoveAlbumItemIfNecessary(oldItem);
                    }

                if (e.NewItems != null)
                    foreach (MusicItemModel newItem in e.NewItems)
                    {
                        AddOrUpdateAlbumItem(newItem);
                    }

                break;

            case NotifyCollectionChangedAction.Reset:
                RebuildAllAlbumItems(); // 全部重置，重建整个专辑列表

                break;
            case NotifyCollectionChangedAction.Move:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e),"未知的集合变更操作！");
        }

        // 更新过滤后的结果（考虑搜索框）
        OnSearchTextChanged(SearchText);
    }

    // 添加或更新专辑项
    private void AddOrUpdateAlbumItem(MusicItemModel musicItem)
    {
        if (string.IsNullOrWhiteSpace(musicItem.Album) || string.IsNullOrWhiteSpace(musicItem.Artists))
            return;

        var existingItem = _allAlbumList.FirstOrDefault(a =>
            a.Name == musicItem.Album && a.Artist == musicItem.Artists);

        if (existingItem != null) return;

        // 新增专辑项
        var newItem = new AlbumItemModel(musicItem.Album, musicItem.Artists, musicItem.CoverId);
        _allAlbumList.Add(newItem);
    }

    // 如果该音乐是某专辑的最后一首，则移除该专辑
    private void RemoveAlbumItemIfNecessary(MusicItemModel musicItem)
    {
        if (string.IsNullOrWhiteSpace(musicItem.Album) || string.IsNullOrWhiteSpace(musicItem.Artists))
            return;

        bool hasOtherMusicsInSameAlbum = MusicItemManager.Default.MusicItems.Any(m =>
            m.Album == musicItem.Album &&
            m.Artists == musicItem.Artists);

        if (hasOtherMusicsInSameAlbum) return;

        var albumToRemove = _allAlbumList.FirstOrDefault(a =>
            a.Name == musicItem.Album && a.Artist == musicItem.Artists);

        if (albumToRemove != null)
        {
            _allAlbumList.Remove(albumToRemove);
        }
    }

    // 重建整个专辑列表
    private void RebuildAllAlbumItems()
    {
        _allAlbumList.Clear();

        var albumGroups = MusicItemManager.Default
            .MusicItems
            .Where(music => !string.IsNullOrWhiteSpace(music.Album) && !string.IsNullOrWhiteSpace(music.Artists))
            .GroupBy(music => new
            {
                music.Album,
                music.Artists,
            })
            .ToList();

        foreach (var albumItem
                 in from @group
                     in albumGroups
                 let firstMusic = @group.First()
                 select new AlbumItemModel(
                     @group.Key.Album,
                     @group.Key.Artists,
                     firstMusic.CoverId))
        {
            _allAlbumList.Add(albumItem);
        }
    }

    private void OnSearchTextChanged(string? value)
    {
        var source = string.IsNullOrEmpty(value)
            ? _allAlbumList
            : _allAlbumList.Where(MatchesSearchCriteria);

        AlbumItems.Clear();
        AlbumItems.AddRange(source);

        return;

        bool MatchesSearchCriteria(AlbumItemModel item)
        {
            return item.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
             || item.Artist.Contains(value, StringComparison.OrdinalIgnoreCase);
        }
    }

    [RelayCommand]
    private static async Task PlayAlbumMusic(AlbumItemModel? albumItem)
    {
        if (albumItem == null)
            return;

        try
        {
            var musicItems = SearchMusicItems(albumItem);

            if (musicItems.Count < 0)
                return;

            MusicPlayListManager.Default.Toggle(musicItems);

            await MusicPlayerViewModel.Default.PlayThisMusic(MusicPlayListManager.Default.First());
        }
        catch (Exception ex)
        {
            // 可以在这里添加错误日志记录
            NotificationService.Error("错误", $"播放专辑时出错: {ex.Message}");
        }
    }

    private static List<MusicItemModel> SearchMusicItems(AlbumItemModel albumItem)
    {
        // 找到该专辑对应的所有音乐项
        var albumMusicItems = MusicItemManager.Default
            .MusicItems.Where(music => music.Album == albumItem.Name && music.Artists == albumItem.Artist)
            .ToList();

        return albumMusicItems;
    }
}
