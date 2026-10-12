using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Common.Utilities.StringUtilities;

namespace QwQ_Music.Models;

public partial class AlbumModel : ObservableObject {
    private bool _isUpdating;
    public bool IsLoaded { get; private set; }
    public required string Name { get; init; }
    public required string Artists { get; init; }

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial DateTime? PublishTime { get; set; }

    [ObservableProperty]
    public partial string? Company { get; set; }

    public Bitmap Thumbnail =>
        CacheManager.TryLoadThumbnail(
                        (Name, Artists),
                        "专辑",
                        "封面",
                        AlbumThumbnailRepository.Instance,
                        () => OnPropertyChanged());

    public List<MusicItemModel>? Musics { get; private set; }
    public Bitmap Cover { get; private set; } = CacheManager.Loading;

    public async Task LoadCurrentAsync() {
        Musics = MusicItemsManager.All.MusicItems.Values
                                  .Where(item => item.Album == Name && item.AlbumArtists == Artists)
                                  .ToList();
        Cover = await AlbumCoverRepository.Instance.SingleAsync((Name, Artists)).ConfigureAwait(false) ??
                CacheManager.NotExist;
        IsLoaded = true;
    }

    public void DisposeCurrent() {
        IsLoaded = false;
        Cover = CacheManager.Loading;
        Musics = null;
    }

    public async Task UpdateAsync() {
        if (!Interlocked.CompareExchange(ref _isUpdating, true, false)) {
            await LoggerService.DebugAsync($"{Name} - {Artists}的更新正在进行。忽略额外的请求。 ").ConfigureAwait(false);
            return;
        }

        try {
            using var crawler = new NetEaseAlbumCrawler();
            AlbumDetail detail = await crawler.GetAlbumDetailByNameAsync(Name, Artists).ConfigureAwait(false);
            Description = StringCleaner.ToPlainText(detail.Description);
            PublishTime = detail.PublishTime;
            Company = detail.Company;
        } catch (NetEaseAlbumCrawlerException ex) {
            await LoggerService.ErrorAsync("爬虫异常", ex).ConfigureAwait(false);
        } catch (Exception ex) {
            await LoggerService.ErrorAsync("其他异常", ex).ConfigureAwait(false);
        } finally {
            _isUpdating = false;
        }
    }
}