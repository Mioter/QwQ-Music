using System.Diagnostics.CodeAnalysis;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services.Databases;

namespace QwQ_Music.Models;

public enum SortMode {
    Custom, AddTimeAscending, AddTimeDescending, NameAscending, NameDescending
}

public enum MusicListType {
    All, Playlist, Custom, Album
}

public partial class MusicListModel : ObservableObject {
    [MemberNotNullWhen(true, nameof(Musics))]
    public bool IsLoaded { get; private set; }

    [ObservableProperty]
    public required partial string Name { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; } = "暂无简介";

    public SortMode SortMode { get; set; } = SortMode.Custom;
    public bool IsCoverExist => Thumbnail != CacheManager.NotExist;

    public Bitmap Thumbnail {
        get =>
            CacheManager.TryLoadThumbnail(
                            (Name, Creator),
                            "音乐列表",
                            "封面",
                            MusicListThumbnailRepository.Instance,
                            () => OnPropertyChanged());
        set {
            CacheManager.SetImage(Name, "音乐列表", value);
            OnPropertyChanged();
        }
    }

    public required string Creator { get; init; } = "未知";

    public DateTime? CreateTime { get; init; }
    public DateTime ModifyTime { get; set; }

    public List<MusicItemModel>? Musics { get; private set; }

    public Bitmap Cover { get; private set; } = CacheManager.Loading;

    public static MusicListModel Create(string name, Bitmap cover) {
        // TODO USERNAME
        return new MusicListModel {
            Name = name, Creator = "_QWQ_LOCAL_USER", Cover = cover, CreateTime = DateTime.Now
        };
    }

    public async Task AddAsync(params ICollection<MusicItemModel> musics) {
        if (!IsLoaded) {
            await MusicListItemsRepository.Instance.InsertAsync(this, musics).ConfigureAwait(false);
            return;
        }

        List<MusicItemModel> items = musics.ToList();
        await MusicListItemsRepository.Instance.InsertAsync(this, items).ConfigureAwait(false);
        Musics.InsertRange(0, items);
    }

    public async Task RemoveAsync(params ICollection<MusicItemModel> musics) {
        if (!IsLoaded) {
            await MusicListItemsRepository.Instance.RemoveAsync(this, musics).ConfigureAwait(false);
            return;
        }

        List<MusicItemModel> items = musics.ToList();
        await MusicListItemsRepository.Instance.RemoveAsync(this, items).ConfigureAwait(false);
        items.ForEach(item => Musics.Remove(item));
    }

    public async Task LoadCurrentAsync() {
        Musics = (await MusicListItemsRepository.Instance.GetAllAsync((Name, Creator)).ConfigureAwait(false)).Paths
            .Select(path => MusicItemsManager.All.MusicItems[path])
            .ToList();
        Cover = await MusicListCoverRepository.Instance.SingleAsync((Name, Creator)).ConfigureAwait(false) ??
                CacheManager.NotExist;
        IsLoaded = true;
    }

    public void DisposeCurrent() {
        IsLoaded = false;
        Cover = CacheManager.Loading;
        Musics = null;
    }
}