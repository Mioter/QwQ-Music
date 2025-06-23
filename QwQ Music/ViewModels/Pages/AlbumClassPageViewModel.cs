using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ.Avalonia.Utilities.MessageBus;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.ViewModels.Pages;

public partial class AlbumClassPageViewModel : ViewModelBase
{
    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public ObservableCollection<AlbumItemModel> AlbumItems { get; set; } = [];

    public AlbumClassPageViewModel()
    {
        MessageBus
            .ReceiveMessage<OperateCompletedMessage>(this)
            .WithHandler(LoadCompletedMessageHandler)
            .AsWeakReference()
            .Subscribe();
    }

    private void LoadCompletedMessageHandler(OperateCompletedMessage message, object sender)
    {
        InitializeAlbumItems();
    }
    
    private void InitializeAlbumItems()
    {
        // 检查音乐库是否已初始化
        if (MusicPlayerViewModel.MusicItems.Count == 0)
        {
            AlbumItems.Clear();
            return;
        }

        // 清理现有专辑列表
        AlbumItems.Clear();

        // 按专辑名和歌手分组，专辑名与歌手相同视为一个专辑项
        var albumGroups = MusicPlayerViewModel
            .MusicItems.Where(music =>
                !string.IsNullOrWhiteSpace(music.Album) && !string.IsNullOrWhiteSpace(music.Artists)
            )
            .GroupBy(music => new { music.Album, music.Artists })
            .ToList();

        foreach (
            var albumItem in albumGroups
                .Select(group => new { group, firstMusic = group.First() })
                .Select(t => new AlbumItemModel(t.group.Key.Album, t.group.Key.Artists, t.firstMusic.CoverPath))
        )
        {
            AlbumItems.Add(albumItem);
        }
    }

    [RelayCommand]
    private async Task PlayAlbumMusic(AlbumItemModel albumItem)
    {
        try
        {
            // 找到该专辑对应的所有音乐项
            var albumMusicItems = MusicPlayerViewModel
                .MusicItems.Where(music => music.Album == albumItem.Title && music.Artists == albumItem.Artist)
                .ToList();

            if (albumMusicItems.Count == 0)
            {
                // 可以在这里添加通知，提示用户没有找到该专辑的音乐
                return;
            }

            // 创建专辑音乐列表并播放
            var albumMusicCollection = new ObservableCollection<MusicItemModel>(albumMusicItems);
            await MusicPlayerViewModel.TogglePlaylist(albumMusicCollection);
        }
        catch (Exception ex)
        {
            // 可以在这里添加错误日志记录
            NotificationService.ShowLight(
                new Notification("错误", $"播放专辑时出错: {ex.Message}"),
                NotificationType.Error
            );
        }
    }
}
