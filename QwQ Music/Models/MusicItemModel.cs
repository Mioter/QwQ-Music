using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Services;
using QwQ_Music.Utilities;
using Log = QwQ_Music.Services.LoggerService;

namespace QwQ_Music.Models;

public record struct LyricsModel(IReadOnlyList<uint> indexes, IReadOnlyList<IReadOnlyList<string>> lyrics)
    : IEnumerable<KeyValuePair<uint, IReadOnlyList<string>>> {
    private int _prev = 0;

    public IReadOnlyList<string> GetCurrentLyrics(uint t) {
        while (t < indexes[_prev]) _prev--;
        while (t > indexes[_prev]) _prev++;
        return lyrics[_prev];
    }

    public static readonly LyricsModel Empty = new([], []);

    public static async Task<LyricsModel> ParseAsync(string lyrics) {
        return await Task.Run(
                () => {
                    if (string.IsNullOrEmpty(lyrics)) return Empty;
                    List<uint> indexes = [];
                    List<List<string>> result = [];
                    var data = lyrics.Split("\n");
                    try {
                        foreach (var line in data) {
                            if (!TimeSpan.TryParse(line[1..9], out var time)) continue;
                            var t = (uint)(time.TotalSeconds * 100);

                            if (indexes[^1] == t)
                                result[^1].Add(line[9..].Trim());
                            else {
                                indexes.Add(t);
                                result.Add([line[9..].Trim()]);
                            }
                        }
                    } catch (Exception ex) {
                        Log.Warning("Cannot parse lyrics:" + ex.Message);
                        indexes = [0];
                        result = [[lyrics]];
                    }

                    return new LyricsModel(indexes, result);
                })
            .ConfigureAwait(false);
    }

    public void Reset() => _prev = 0;

    public IEnumerator<KeyValuePair<uint, IReadOnlyList<string>>> GetEnumerator() {
        foreach (var (index, lyric) in indexes.Zip(lyrics, Tuple.Create)) {
            yield return new KeyValuePair<uint, IReadOnlyList<string>>(index, lyric);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
}

public partial class MusicItemModel(
    string? title = null,
    string[]? artists = null,
    string? album = null,
    string? coverPath = null,
    string filePath = "",
    string fileSize = "",
    double gain = 0f,
    TimeSpan? current = null,
    TimeSpan duration = default,
    string encodingFormat = "",
    string? comment = null) : ObservableObject, IEquatable<MusicItemModel>, IModelBase<MusicItemModel> {
    public bool IsInitialized { get; private set; }
    public bool IsError { get; private set; }
    public string Title = string.IsNullOrWhiteSpace(title) ? "未知标题" : title;
    public string TitleProperty => Title;
    public string[] Artists = artists ?? ["未知歌手"];
    public string ArtistsProperty => string.Join(',', Artists);
    public string Album = string.IsNullOrWhiteSpace(album) ? "未知专辑" : album;
    public string AlbumProperty => Album;
    public string? CoverPath = coverPath;
    public string? CoverPathProperty => CoverPath;
    [ObservableProperty] private TimeSpan _current = current ?? TimeSpan.Zero;
    public TimeSpan Duration = duration;
    public TimeSpan DurationProperty => Duration;
    public string? FilePath = filePath;
    public string? FilePathProperty => FilePath;
    public string FileSize = fileSize;
    public string FileSizeProperty => FileSize;
    public double Gain = gain;
    public double GainProperty => Gain;
    public string EncodingFormat = encodingFormat;
    public string EncodingFormatProperty => EncodingFormat;
    public string? Comment = comment;
    public string? CommentProperty => Comment;
    [ObservableProperty] private string? _remarks;

    public readonly Lazy<MusicTagExtensions> Extensions = new(
        () => MusicExtractor.ExtractMusicInfoExtensionsAsync(filePath).ConfigureAwait(false).GetAwaiter().GetResult());

    public MusicTagExtensions ExtensionsProperty => Extensions.Value;

    public readonly Lazy<Task<LyricsModel>> Lyrics = new(() => MusicExtractor.ExtractMusicLyricsAsync(filePath));

    public bool Equals(MusicItemModel? other) => other is not null && string.Equals(FilePath, other.FilePath);
    public override bool Equals(object? obj) => obj is MusicItemModel other && Equals(other);

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => (Title + string.Join(',', Artists)).GetHashCode();
    // ReSharper restore NonReadonlyMemberInGetHashCode

    public static MusicItemModel Parse(in SqliteDataReader config) {
#pragma warning disable MVVMTK0034
        MusicItemModel result = new();
        result.IsError = ConfigIO.TryParse(config, nameof(Title), ref result.Title) |
                         ConfigIO.TryParse(
                             config,
                             nameof(Artists),
                             ref result.Artists,
                             (string data) => data.Split("\n")) |
                         ConfigIO.TryParse(config, nameof(Album), ref result.Album) |
                         ConfigIO.TryParse(config, nameof(CoverPath), ref result.CoverPath) |
                         ConfigIO.TryParse(config, nameof(Current), ref result._current) |
                         ConfigIO.TryParse(config, nameof(Duration), ref result.Duration) |
                         ConfigIO.TryParse(config, nameof(FilePath), ref result.FilePath) |
                         ConfigIO.TryParse(config, nameof(FileSize), ref result.FileSize) |
                         ConfigIO.TryParse(config, nameof(Gain), ref result.Gain) |
                         ConfigIO.TryParse(config, nameof(EncodingFormat), ref result.EncodingFormat) |
                         ConfigIO.TryParse(config, nameof(Comment), ref result.Comment) |
                         ConfigIO.TryParse(config, nameof(Remarks), ref result._remarks);
#pragma warning restore MVVMTK0034
        result.IsInitialized = true;
        return result;
    }

    public Dictionary<string, string> Dump() =>
        new() {
            [nameof(ConfigInfoModel.Version)] = ConfigInfoModel.Version,
            [nameof(Title)] = Title,
            [nameof(Artists)] = string.Join("\n", Artists),
            [nameof(Album)] = Album,
            [nameof(CoverPath)] = CoverPath ?? "",
            [nameof(Current)] = Current.ToString(),
            [nameof(Duration)] = Duration.ToString(),
            [nameof(FilePath)] = FilePath ?? "",
            [nameof(FileSize)] = FileSize,
            [nameof(Gain)] = Gain.ToString("G"),
            [nameof(EncodingFormat)] = EncodingFormat,
            [nameof(Comment)] = Comment ?? "",
            [nameof(Remarks)] = Remarks ?? ""
        };
}

public readonly record struct MusicTagExtensions(
    string Genre,
    uint Year,
    string[] Composers,
    string Copyright,
    uint Disc,
    uint Track,
    int SamplingRate,
    int Bitrate);