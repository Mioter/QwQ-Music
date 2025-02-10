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
    : ObservableObject
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

    [ObservableProperty] private string? _samplingRate = samplingRate;

    [ObservableProperty] private string? _singer = string.IsNullOrWhiteSpace(singer) ? "未知歌手" : singer;

    [ObservableProperty] private string? _title = string.IsNullOrWhiteSpace(title) ? "未知标题" : title;

    [ObservableProperty] private string? _totalDuration = totalDuration ?? "00:00";

    [ObservableProperty] private string? _trackNumber = trackNumber;

    [ObservableProperty] private string? _year = year;
}
