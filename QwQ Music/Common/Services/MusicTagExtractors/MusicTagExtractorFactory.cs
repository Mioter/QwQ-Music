using ATL;
using QwQ_Music.Models;

namespace QwQ_Music.Common.Services.MusicTagExtractors;

public static class MusicTagExtractorFactory {
    public static MusicDetailedInfo? ExtractDetailedInfo(Track track) {
        return new MusicDetailedInfo(
            // 发布信息
            track.Date,
            track.OriginalReleaseDate,
            track.PublishingDate,

            // 专业信息
            track.ISRC,
            track.CatalogNumber,
            track.ProductId,

            // 其他信息
            track.BPM,
            track.Popularity,
            track.SeriesTitle,
            track.SeriesPart,
            track.LongDescription,
            track.Group,

            // 技术信息
            track.TechnicalInformation.AudioDataOffset,
            track.TechnicalInformation.AudioDataSize);
    }

    public static Dictionary<string, string>? ExtractAdditionalFields(Track track) {
        return track.AdditionalFields != null ? new Dictionary<string, string>(track.AdditionalFields) : [];
    }
}