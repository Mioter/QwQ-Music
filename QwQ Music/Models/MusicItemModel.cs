using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Data.Sqlite;
using QwQ_Music.Models.ModelBase;
using QwQ_Music.Services;
using QwQ_Music.Services.ConfigIO;

namespace QwQ_Music.Models;

public partial class MusicItemModel(
    string? title = null,
    string? artists = null,  
    string? composer = null,
    string? album = null,
    string? coverPath = null,
    string filePath = "",
    string fileSize = "",
    TimeSpan? current = null,
    TimeSpan duration = default,
    string encodingFormat = "",
    string? comment = null,
    double gain = -1.0f,
    string[]? coverColor = null
) : ObservableObject, IEquatable<MusicItemModel>, IModelBase<MusicItemModel>
{
    public bool IsInitialized { get; private set; }
    public bool IsError { get; private set; }

    public string Title = string.IsNullOrWhiteSpace(title) ? "未知标题" : title;


    public string TitleProperty => Title;


    public string Artists = artists ?? "未知歌手";
    
    public string Composer = composer ?? string.Empty;
    public string ComposerProperty => Composer;
    
    public string ArtistsProperty => string.Join(',', Artists);

    public string Album = string.IsNullOrWhiteSpace(album) ? "未知专辑" : album;
    public string AlbumProperty => Album;
    public string? CoverPath = coverPath;

    public string? CoverPathProperty => CoverPath;

    public string[]? CoverColors = coverColor;

    [ObservableProperty]
    private TimeSpan _current = current ?? TimeSpan.Zero;
    public TimeSpan Duration = duration;
    public TimeSpan DurationProperty => Duration;
    public string FilePath = filePath;
    public string FilePathProperty => FilePath;
    public string FileSize = fileSize;
    public string FileSizeProperty => FileSize;
    public double Gain = gain;

    public string EncodingFormat = encodingFormat;
    public string EncodingFormatProperty => EncodingFormat;
    public string? Comment = comment;
    public string? CommentProperty => Comment;

    [ObservableProperty]
    private string? _remarks;
    
    public async Task<MusicTagExtensions> GetExtensionsInfo() => await MusicExtractor.ExtractExtensionsInfoAsync(FilePath);

    public readonly Lazy<Task<LyricsModel>> Lyrics = new(() => MusicExtractor.ExtractMusicLyricsAsync(filePath));

    public bool Equals(MusicItemModel? other) => other is not null && string.Equals(FilePath, other.FilePath);

    public override bool Equals(object? obj) => obj is MusicItemModel other && Equals(other);

    public override int GetHashCode() => (Title + string.Join(',', Artists)).GetHashCode();

    public static MusicItemModel Parse(in SqliteDataReader config)
    {
        MusicItemModel result = new();
        result.IsError =
            DataBaseService.TryParse(config, nameof(Title), ref result.Title)
            | DataBaseService.TryParse(config, nameof(Artists), ref result.Artists)
            | DataBaseService.TryParse(config, nameof(Album), ref result.Album)
            | DataBaseService.TryParse(config, nameof(Composer), ref result.Composer)
            | DataBaseService.TryParse(config, nameof(CoverPath), ref result.CoverPath)
            | DataBaseService.TryParse(config, nameof(Current), ref result._current)
            | DataBaseService.TryParse(config, nameof(Duration), ref result.Duration)
            | DataBaseService.TryParse(config, nameof(FilePath), ref result.FilePath)
            | DataBaseService.TryParse(config, nameof(FileSize), ref result.FileSize)
            | DataBaseService.TryParse(config, nameof(Gain), ref result.Gain)
            | DataBaseService.TryParse(
                config,
                nameof(CoverColors),
                ref result.CoverColors,
                (string data) => data.Split("\n")
            )
            | DataBaseService.TryParse(config, nameof(Comment), ref result.Comment)
            | DataBaseService.TryParse(config, nameof(EncodingFormat), ref result.EncodingFormat)
            | DataBaseService.TryParse(config, nameof(Comment), ref result.Comment)
            | DataBaseService.TryParse(config, nameof(Remarks), ref result._remarks);

        result.IsInitialized = true;
        return result;
    }

    public Dictionary<string, string> Dump() =>
        new()
        {
            [nameof(ConfigInfoModel.Version)] = ConfigInfoModel.Version,
            [nameof(Title)] = Title,
            [nameof(Artists)] = Artists,
            [nameof(Album)] = Album,
            [nameof(CoverPath)] = CoverPath ?? "",
            [nameof(Current)] = Current.ToString(),
            [nameof(Duration)] = Duration.ToString(),
            [nameof(FilePath)] = FilePath,
            [nameof(FileSize)] = FileSize,
            ["BASICINFO"] = $"{Title}\n{string.Join("\n", Artists)}\n{Album}",
            [nameof(Gain)] = Gain.ToString("G"),
            [nameof(CoverColors)] = string.Join("\n", CoverColors ?? [""]),
            [nameof(EncodingFormat)] = EncodingFormat,
            [nameof(Comment)] = Comment ?? "",
            [nameof(Remarks)] = Remarks ?? "",
        };
}

public readonly record struct MusicTagExtensions(
    string Genre,
    int? Year,
    string[] Composers,
    string Copyright,
    uint Disc,
    uint Track,
    int SamplingRate,
    int Channels,
    int Bitrate,
    int BitsPerSample,
    // 添加更多基本信息
    string OriginalAlbum,
    string OriginalArtist,
    string AlbumArtist,
    string Publisher,
    string Description,
    string Language,
    // 添加技术信息
    bool IsVbr,
    string AudioFormat,
    string EncoderInfo
);

// 添加扩展结构体用于获取更多详细信息
public readonly record struct MusicDetailedInfo(
    // 发布信息
    DateTime? ReleaseDate,
    DateTime? OriginalReleaseDate,
    DateTime? PublishingDate,
    // 专业信息
    string Isrc,
    string CatalogNumber,
    string ProductId,
    // 其他信息
    float? Bpm,
    float? Popularity,
    string SeriesTitle,
    string SeriesPart,
    string LongDescription,
    string Group,
    // 技术信息
    long AudioDataOffset,
    long AudioDataSize
);

