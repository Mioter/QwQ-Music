namespace QwQ_Music.Models;

public sealed class MusicMetadata {
    public string Title { get; set; } = string.Empty;

    public string Artists { get; set; } = string.Empty;

    public string Album { get; set; } = string.Empty;

    public string AlbumArtist { get; set; } = string.Empty;

    public string Composer { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public string EncodingFormat { get; set; } = string.Empty;

    public TimeSpan Duration { get; set; }

    public byte[]? CoverImageData { get; set; }
}

public record MusicTagExtensions(
    string Genre,
    int? Year,
    string Copyright,
    uint Disc,
    uint Track,
    int SampleRate,
    int Channels,
    int Bitrate,
    int BitsPerSample,

    // 添加更多基本信息
    string OriginalAlbum,
    string OriginalArtist,
    string Publisher,
    string Description,
    string Language,

    // 添加技术信息
    bool IsVbr,
    string AudioFormat,
    string EncoderInfo);

// 添加扩展结构体用于获取更多详细信息
public record MusicDetailedInfo(
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
    long AudioDataSize);