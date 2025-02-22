using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Services;
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
    string? comment = null) : ObservableObject, IEquatable<MusicItemModel>, IConfigBase<MusicItemModel> {
    public string FileName =>
        Title.Length > 20 ?
            Title[..20] :
            Title +
            (string.Join(";", Artists).Length > 20 ?
                string.Join(";", Artists)[..20] :
                string.Join(";", Artists)); //TODO

    public bool IsInitialized { get; private set; }
    public bool IsError { get; private set; }
    public string Title { get; private set; } = string.IsNullOrWhiteSpace(title) ? "未知标题" : title;
    public string[] Artists { get; private set; } = artists ?? ["未知歌手"];
    public string Album { get; private set; } = string.IsNullOrWhiteSpace(album) ? "未知专辑" : album;
    public string? CoverPath { get; private set; } = coverPath;

    [ObservableProperty] private TimeSpan _current = current ?? TimeSpan.Zero;
    public TimeSpan Duration { get; private set; } = duration;
    public string? FilePath { get; private set; } = filePath;
    public string FileSize { get; private set; } = fileSize;
    public double Gain { get; private set; } = gain;
    public string EncodingFormat { get; private set; } = encodingFormat;
    public string? Comment { get; private set; } = comment;

    [ObservableProperty] private string? _remarks;

    public readonly Lazy<Task<MusicTagExtensions>> Extensions = new(
        () => MusicExtractor.ExtractMusicInfoExtensionsAsync(filePath));

    public readonly Lazy<Task<LyricsModel>> Lyrics = new(() => MusicExtractor.ExtractMusicLyricsAsync(filePath));

    public bool Equals(MusicItemModel? other) => other is not null && string.Equals(FilePath, other.FilePath);
    public override bool Equals(object? obj) => obj is MusicItemModel other && Equals(other);
    public override int GetHashCode() => FileName.GetHashCode();

    public static MusicItemModel Parse(SqliteDataReader config) {
        MusicItemModel result = new();
        try {
            result.Title = config.GetString(config.GetOrdinal(nameof(Title)));
            result.Artists = config.GetFieldValue<string[]>(config.GetOrdinal(nameof(Artists)));
            result.Album = config.GetString(config.GetOrdinal(nameof(Album)));
            result.CoverPath = config.GetString(config.GetOrdinal(nameof(CoverPath)));
            result.Current = config.GetTimeSpan(config.GetOrdinal(nameof(Title)));
            result.Duration = config.GetTimeSpan(config.GetOrdinal(nameof(Duration)));
            result.FilePath = config.GetString(config.GetOrdinal(nameof(FilePath)));
            result.FileSize = config.GetString(config.GetOrdinal(nameof(FileSize)));
            result.Gain = config.GetDouble(config.GetOrdinal(nameof(Gain)));
            result.EncodingFormat = config.GetString(config.GetOrdinal(nameof(EncodingFormat)));
            result.Comment = config.GetString(config.GetOrdinal(nameof(Comment)));
            result.Remarks = config.GetString(config.GetOrdinal(nameof(Remarks)));
            result.IsInitialized = true;
            result.IsError = false;
            return result;
        } catch (NullReferenceException) {
            Log.Error(
                $"Cannot Load {nameof(MusicItemModel)}. Config file broken or version inconsistent? (file version {
                    config.GetString(config.GetOrdinal(nameof(ConfigInfoModel.Version)))}, app version {
                        ConfigInfoModel.Version})");
            result.IsInitialized = true;
            result.IsError = true;
            return result;
        }
    }

    public string Dump() =>
        $"""
         ({nameof(ConfigInfoModel.Version)},
         {nameof(Title)},
         {nameof(Artists)},
         {nameof(Album)},
         {nameof(CoverPath)},
         {nameof(Current)},
         {nameof(Duration)},
         {nameof(FilePath)},
         {nameof(FileSize)},
         {nameof(Gain)},
         {nameof(EncodingFormat)},
         {nameof(Comment)},
         {nameof(Remarks)}) 
         VALUES(
         {ConfigInfoModel.Version},
         {Title},
         {Artists},
         {Album},
         {CoverPath},
         {Current},
         {Duration},
         {FilePath},
         {FileSize},
         {Gain},
         {EncodingFormat},
         {Comment},
         {Remarks})
         """;
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