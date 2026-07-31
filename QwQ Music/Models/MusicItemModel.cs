using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ATL;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Common.Helpers;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Common.Services.MusicTagExtractors;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Common.Utilities.StringUtilities;
using QwQ_Music.Models.Enums;
using SystemMediaInterop;

namespace QwQ_Music.Models;

public partial class MusicItemModel : ObservableObject, IMediaItem {
    public static readonly MusicItemModel Default = new() {
        Title = "听你想听", Artists = "YOU", FilePath = string.Empty, Lyrics = LyricsData.Default
    };

    public string Extension;

    public bool IsTemporary {
        get;
        set {
            if (!SetProperty(ref field, value))
                return;
            _tempThumbnail = null;
            _ = UpdateMetaDataAsync().ConfigureAwait(false);
        }
    }

    public bool IsCurrent {
        get;
        private set {
            if (SetProperty(ref field, value))
                OnPropertyChanged();
        }
    }

    public Stream Data => File.OpenRead(FilePath);

    [ObservableProperty]
    public partial string Title { get; set; } = "未知标题";

    [ObservableProperty]
    public partial string Artists { get; set; } = "未知歌手";
    //TODO SEPARATE ARTISTS

    [ObservableProperty]
    public partial string? Composer { get; set; }

    [ObservableProperty]
    public partial string Album { get; set; } = "未知专辑";

    [ObservableProperty]
    public partial string AlbumArtists { get; set; } = "未知专辑艺术家";

    [ObservableProperty]
    public partial TimeSpan Record { get; set; } = TimeSpan.Zero;

    public TimeSpan Duration { get; set; }

    public required string FilePath {
        get;
        [MemberNotNull(nameof(Extension))]
        set {
            field = value;
            Extension = Path.GetExtension(FilePath).ToUpper();
        }
    }

    public string FileSize { get; set; } = "未知";

    public double Gain { get; set; }

    public string EncodingFormat { get; set; } = "未知";

    public (string Name, string Artists) AlbumId { get; set; } = (string.Empty, string.Empty);

    public bool HasCover => !AlbumId.Name.StartsWith('\u0002');

    public string[]? CoverColors { get; set; }

    public AudioQualityLevel AudioQualityLevel { get; set; }

    public string Comment { get; set; } = "";

    private Bitmap? _tempThumbnail;

    public Bitmap Thumbnail =>
        IsTemporary ?
            _tempThumbnail ?? CacheManager.Loading :
            CacheManager.TryLoadThumbnail(
                AlbumId,
                "音频",
                "封面",
                AlbumThumbnailRepository.Instance,
                () => OnPropertyChanged(),
                Title);

    public Stream ThumbnailStream {
        get {
            MemoryStream thumbnailStream = new();
            Thumbnail.Save(thumbnailStream);
            return thumbnailStream;
        }
    }

    [ObservableProperty]
    public partial string Remarks { get; set; } = "";

    public double LyricOffset { get; set; }

    public DateTime InsertTime { get; set; }

    public DateTime ModificationTime { get; set; }

    public int Channels { get; set; }
    public double SampleRate { get; set; }

    public void RemoveCover() {
        if (AlbumId.Name.StartsWith('\u0002'))
            CacheManager.DeleteImage(AlbumId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearRecord() { Record = TimeSpan.Zero; }

    public async Task<Track?> GetTrackAsync() {
        if (Extension == AudioFileValidator.AudioFormatsExtendToNameMap[AudioFileValidator.ExtendAudioFormats.Ncm])
            return await new NcmMusicTagExtractor(FilePath).GetTrackAsync().ConfigureAwait(false);

        return new Track(FilePath);
    }

    public async Task<bool> UpdateMetaDataAsync(bool forceRefresh = false) {
        if (!File.Exists(FilePath)) {
            NotificationService.Error($"未找到{Title} - {Artists}: 文件不存在，源路径{FilePath}");
            await LoggerService.ErrorAsync($"未找到音乐文件: {FilePath}").ConfigureAwait(false);
            return false;
        }

        var fileInfo = new FileInfo(FilePath);

        // 如果修改时间没有变动，不需要更新
        if (fileInfo.LastWriteTimeUtc == ModificationTime && !forceRefresh)
            return false;


        if (await GetTrackAsync().ConfigureAwait(false) is not { } track) {
            await LoggerService.WarningAsync($"无法获取《{Title}》的音轨信息").ConfigureAwait(false);
            return false;
        }

        await SetMusicMetadata(track, fileInfo, forceRefresh).ConfigureAwait(false);
        return true;
    }

    private async Task SetMusicMetadata(Track track, FileInfo fileInfo, bool forceRefresh) {
        Title = string.IsNullOrEmpty(track.Title) ?
            Path.GetFileNameWithoutExtension(FilePath).Trim() :
            track.Title.Trim();

        if (!string.IsNullOrEmpty(track.Artist))
            Artists = track.Artist.Trim();

        if (!string.IsNullOrEmpty(track.Album))
            Album = track.Album.Trim();

        AlbumArtists = string.IsNullOrEmpty(track.AlbumArtist) ? Artists : track.AlbumArtist.Trim();

        Composer = track.Composer;
        Duration = TimeSpan.FromMilliseconds(track.DurationMs);
        EncodingFormat = track.AudioFormat.ShortName;
        Comment = track.Comment;
        AudioQualityLevel = AudioQualityDetector.DetermineQualityLevel(track);
        Channels = track.ChannelsArrangement.NbChannels;
        SampleRate = track.SampleRate;
        ModificationTime = DateTime.Now;
        FileSize = StringFormatter.FormatFileSize(fileInfo.Length);
        try {
            if (track.EmbeddedPictures.Count == 0) {
                await LoggerService.InfoAsync($"{Title}的封面不存在。").ConfigureAwait(false);
            } else {
                byte[]? coverData = track.EmbeddedPictures[0].PictureData;

                AlbumId = (string.IsNullOrWhiteSpace(Album) ? $"\u0002{Guid.NewGuid()}" : Album, AlbumArtists);
                if (!IsTemporary)
                    await AlbumRepository.Instance.AddOrUpdateAlbumItemAsync(this).ConfigureAwait(false);
                Bitmap? thumbnail = await ImageHelper
                                          .LoadFromMemoryAsync(new MemoryStream(coverData), AlbumId.ToString(), 128)
                                          .ConfigureAwait(false);
                if (thumbnail is null) {
                    await LoggerService.ErrorAsync("制作缩略图失败").ConfigureAwait(false);
                    return;
                }

                await LoggerService.DebugAsync("制作缩略图成功").ConfigureAwait(false);
                CacheManager.SetImage(AlbumId, "音频", thumbnail);
                if (IsTemporary)
                    _tempThumbnail = thumbnail;
                else
                    _ = AlbumCoverRepository.Instance
                                            .InsertAsync(
                                                this,
                                                coverData,
                                                forceRefresh ? InsertExist.REPLACE : InsertExist.IGNORE)
                                            .ContinueWith(LoggerService.HandleException)
                                            .ConfigureAwait(false);
                OnPropertyChanged(nameof(Thumbnail));
            }
        } catch (Exception e) {
            await LoggerService.ErrorAsync($"解码{Title}的封面图像失败", e).ConfigureAwait(false);
        }

        if (!IsTemporary)
            await MusicItemRepository.Instance.UpdateAsync(this).ConfigureAwait(false);
    }

    private async Task<LyricsData> LoadLyricsAsync(Track track) {
        var result = LyricsData.Create(Title, Artists, Album, AlbumArtists);
        IList<LyricsInfo>? lyricsList = track.Lyrics;

        // 查找同步歌词
        LyricsInfo? syncLyricsInfo = lyricsList.FirstOrDefault(l => l.SynchronizedLyrics.Count > 0);

        if (syncLyricsInfo != null) {
            IList<LyricsInfo.LyricsPhrase>? syncLyrics = syncLyricsInfo.SynchronizedLyrics;

            // 按时间点分组
            IOrderedEnumerable<IGrouping<int, LyricsInfo.LyricsPhrase>> grouped =
                syncLyrics.GroupBy(p => p.TimestampStart).OrderBy(g => g.Key);

            foreach (IGrouping<int, LyricsInfo.LyricsPhrase> group in grouped) {
                List<LyricsInfo.LyricsPhrase> phrases = group.ToList();

                string? primary, translation = null;

                // 方案二：尝试分隔符
                string[] split = phrases[0].Text.Split(["//", "|", "\n", " "], StringSplitOptions.RemoveEmptyEntries);

                if (split.Length == 2) {
                    primary = split[0].Trim();
                    translation = split[1].Trim();
                } else {
                    primary = phrases[0].Text.Trim();

                    // 方案一：同一时间点有多条
                    if (phrases.Count > 1)
                        translation = phrases[1].Text.Trim();
                }

                result.Data.Add(new LyricLine(group.Key / 1000.0, primary, translation));
            }

            return result;
        }

        // 查找非同步歌词
        LyricsInfo? unsyncedLyricsInfo = lyricsList.FirstOrDefault(l => !string.IsNullOrEmpty(l.UnsynchronizedLyrics));

        if (unsyncedLyricsInfo != null) {
            string? lyric = unsyncedLyricsInfo.UnsynchronizedLyrics;

            if (!string.IsNullOrEmpty(lyric)) {
                (double Offset, IEnumerable<LyricLine> Lyrics) embedded =
                    await Task.Run(() => LyricsService.ParseLrcFile(lyric)).ConfigureAwait(false);
                result.Offset += embedded.Offset;
                result.Data.AddRange(embedded.Lyrics);
                return result;
            }
        }

        // 获取目录路径
        string? directoryPath = Path.GetDirectoryName(FilePath);

        // 获取文件名（不含扩展名）
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(FilePath);

        // 拼接完整路径（不含扩展名）
        if (directoryPath == null || fileNameWithoutExtension == "")
            return result;

        string fullPathWithoutExtension = Path.Combine(directoryPath, fileNameWithoutExtension);
        string lyricPath = fullPathWithoutExtension + ".lrc";

        if (!Path.Exists(lyricPath))
            return result;

        string lyricText = await File.ReadAllTextAsync(lyricPath).ConfigureAwait(false);

        (double Offset, IEnumerable<LyricLine> Lyrics) outer =
            await Task.Run(() => LyricsService.ParseLrcFile(lyricText)).ConfigureAwait(false);
        result.Offset += outer.Offset;
        result.Data.AddRange(outer.Lyrics);
        return result;
    }

    public async Task LoadCurrentAsync() {
        if (IsCurrent) {
            await LoggerService.WarningAsync("多余的音频封面与歌词加载请求，已忽略").ConfigureAwait(false);
            return;
        }

        await LoggerService.DebugAsync($"正在异步加载音频《{Title}》的原始封面与歌词...").ConfigureAwait(false);
        IsCurrent = true;
        Cover = CacheManager.Loading;
        if (string.IsNullOrWhiteSpace(FilePath)) {
            await LoggerService.ErrorAsync($"{Title}的文件路径为空。").ConfigureAwait(false);
            return;
        }

        if (await GetTrackAsync().ConfigureAwait(false) is not { } track) {
            await LoggerService.ErrorAsync($"无法获取{Title}的音频流。").ConfigureAwait(false);
            return;
        }

        Track = track;
        Bitmap cover = track.EmbeddedPictures.Count > 0 ?
            new Bitmap(new MemoryStream(track.EmbeddedPictures[0].PictureData)) :
            CacheManager.NotExist;
        Dispatcher.UIThread.Post(() => Cover = cover);

        Lyrics = await LoadLyricsAsync(track).ConfigureAwait(false);
        await LoggerService.DebugAsync($"音频《{Title}》的原始封面与歌词加载完毕。").ConfigureAwait(false);
    }

    public void DisposeCurrent() {
        if (!IsCurrent) {
            LoggerService.Warning("意外的音频封面与歌词释放请求，已忽略。");
            return;
        }

        IsCurrent = false;
        Track = null;
        Cover = CacheManager.Loading;
        Lyrics = LyricsData.Loading;
        LoggerService.Debug($"已释放音频《{Title}》的歌词。");
    }

    #region 播放歌曲前加载的属性

    public Bitmap Cover {
        get {
            try {
                _ = field.PixelSize;
                return field;
            } catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) {
                Cover = Track?.EmbeddedPictures.Count > 0 ?
                    new Bitmap(new MemoryStream(Track.EmbeddedPictures[0].PictureData)) :
                    CacheManager.NotExist;
                LoggerService.Warning($"歌曲《{Title}》的原始封面意外失效。已重新加载。");
                return Cover;
            }
        }
        private set {
            try {
                _ = value.PixelSize;
            } catch (Exception ex) when (ex is NullReferenceException or ObjectDisposedException) {
                field = CacheManager.Damaged;
                OnPropertyChanged();
                return;
            }

            if (!ConfigManager.UiConfig.VisualConfig.AllowNonSquareCover && Math.Abs(value.Size.AspectRatio - 1) > 1e-5)
                BitmapCropper.Crop(ref value, 1.0);
            field = value;
            OnPropertyChanged();
        }
    } = CacheManager.NotExist;

    public LyricsData Lyrics { get; private set; } = LyricsData.Loading;

    public Track? Track { get; private set; }

    #endregion 播放歌曲前加载的属性

    public override string ToString() { return $"{Title}_{Artists}"; }
}

public readonly record struct PlaylistItemModel : IMediaItemWrapper {
    public static readonly PlaylistItemModel RefDefault = new(MusicItemModel.Default, 0);
    public readonly ulong Id = 0L;

    private PlaylistItemModel(MusicItemModel model, ulong id) {
        Model = model;
        Id = id;
    }

    public PlaylistItemModel(MusicItemModel model) {
        Model = model;
        Id = IdAllocator;
    }

    private static ulong IdAllocator {
        get => field++;
        set;
    } = 1;

    public MusicItemModel Model { get; } = MusicItemModel.Default;

    public static void Reset() { IdAllocator = 1; }
    public IMediaItem MediaItem => Model;
    public override string ToString() { return $"{Model.Title}_{Model.Artists}({Id})"; }
}