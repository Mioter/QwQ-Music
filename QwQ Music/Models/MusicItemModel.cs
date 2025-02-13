using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.Models;

public partial class MusicItemModel(
    string? title = null,
    string? singer = null,
    string? filePath = null,
    string? fileSize = null,
    string? totalDuration = "00:00",
    string? album = null,
    string? genre = null,
    string? albumImageIndex = null,
    string? currentDuration = "00:00",
    string? year = null,
    string? comment = null,
    string? composer = null,
    string? copyright = null,
    string? discNumber = null,
    string? trackNumber = null,
    string? samplingRate = null,
    string? bitrate = null,
    string? encodingFormat = null)
    : ObservableObject, IEquatable<MusicItemModel>
{

    [ObservableProperty] private string? _album = string.IsNullOrWhiteSpace(album) ? "未知专辑" : album;

    [ObservableProperty] private string? _albumImageIndex = albumImageIndex;

    [ObservableProperty] private string? _bitrate = bitrate;

    [ObservableProperty] private string? _comment = comment;

    [ObservableProperty] private string? _composer = composer;

    [ObservableProperty] private string? _copyright = copyright;

    [ObservableProperty] private string? _currentDuration = currentDuration ?? "00:00";

    [ObservableProperty] private string? _discNumber = discNumber;

    [ObservableProperty] private string? _encodingFormat = encodingFormat;

    [ObservableProperty] private string? _filePath = filePath;

    [ObservableProperty] private string? _fileSize = fileSize;

    [ObservableProperty] private string? _genre = genre;

    [ObservableProperty] private string? _remarks;

    [ObservableProperty] private float[]? _replayGain;

    [ObservableProperty] private string? _samplingRate = samplingRate;

    [ObservableProperty] private string? _singer = string.IsNullOrWhiteSpace(singer) ? "未知歌手" : singer;

    [ObservableProperty] private string? _title = string.IsNullOrWhiteSpace(title) ? "未知标题" : title;

    [ObservableProperty] private string? _totalDuration = totalDuration ?? "00:00";

    [ObservableProperty] private string? _trackNumber = trackNumber;

    [ObservableProperty] private string? _year = year;

    public bool Equals(MusicItemModel? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Singer, other.Singer, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as MusicItemModel);
    }

    public override int GetHashCode()
    {
        unchecked // Allow arithmetic overflow, just wrap around
        {
            int hash = 17;
            hash = hash * 23 + (FilePath?.ToLowerInvariant().GetHashCode() ?? 0);
            hash = hash * 23 + (Title?.ToLowerInvariant().GetHashCode() ?? 0);
            hash = hash * 23 + (Singer?.ToLowerInvariant().GetHashCode() ?? 0);
            return hash;
        }
    }
}
