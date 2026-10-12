using ATL;
using Avalonia.Collections;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class AudioDetailedInfoViewModel : ViewModelBase {
    private readonly MusicItemModel _musicItem;
    private readonly Track? _track;

    public AudioDetailedInfoViewModel(MusicItemModel musicItem, Track? track) {
        _musicItem = musicItem;
        _track = track ?? musicItem.GetTrackAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        MusicInfoGroups = [];

        InitializeMusicInfo();
    }

    // 使用 MusicInfoGroup 集合替代原来的单个列表
    public AvaloniaList<MusicInfoGroup> MusicInfoGroups { get; }

    public string? SelectedText { get; set; }

    private void InitializeMusicInfo() {
        AddFileInfoGroup();
        AddBasicMetadataGroup();
        AddPersonsAndRightsGroup();
        AddClassificationAndSeriesGroup();
        AddDatesGroup();
        AddNumbersAndRatingsGroup();
        AddIdentifiersGroup();
        AddSortingGroup();
        AddLyricsAndChaptersGroup();
        AddAdditionalFieldsGroup();
        AddAudioTechnicalInfoGroup();
        AddMetadataFormatsGroup();
    }

    // --- 辅助方法 ---

    private static void AddInfo(AvaloniaList<MusicInfoKeyValuePair> targetList, string key, string? value) {
        // 只添加非空且有意义的信息
        if (!string.IsNullOrWhiteSpace(value) && value != "0" && value != "0001-01-01 00:00:00")
            targetList.Add(new MusicInfoKeyValuePair(key, value));
    }

    private static string FormatValue(object? value, string? unit = null) {
        if (value == null)
            return "未知";

        string? valueStr = value.ToString();

        if (string.IsNullOrEmpty(valueStr) || valueStr == "0")
            return "未知";

        return string.IsNullOrEmpty(unit) ? valueStr : $"{valueStr} {unit}";
    }

    private static string GetCodecFamilyDescription(int codecFamily) {
        return codecFamily switch {
            0 => "流式 有损",
            1 => "流式 无损",
            2 => "序列化 嵌入式音源库",
            3 => "序列化 编解码器/硬件相关音源库",
            _ => "未知"
        };
    }

    // --- 分组添加方法 ---

    private void AddFileInfoGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();
        AddInfo(items, "标题", _musicItem.Title);
        AddInfo(items, "文件路径", _musicItem.FilePath);
        AddInfo(items, "文件大小", _musicItem.FileSize);
        AddInfo(items, "添加时间", _musicItem.InsertTime.ToString("yyyy-MM-dd HH:mm:ss"));
        AddInfo(items, "修改时间", _musicItem.ModificationTime.ToString("yyyy-MM-dd HH:mm:ss"));
        AddInfo(items, "编码格式", _track?.AudioFormat.ShortName ?? "未知");

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("文件信息", items));
    }

    private void AddBasicMetadataGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();
        AddInfo(items, "标题", _track?.Title);
        AddInfo(items, "艺术家", _track?.Artist ?? _musicItem.Artists);
        AddInfo(items, "专辑", _track?.Album ?? _musicItem.Album);
        AddInfo(items, "专辑艺术家", _track?.AlbumArtist ?? _musicItem.AlbumArtists);
        AddInfo(items, "作曲", _track?.Composer ?? _musicItem.Composer ?? "未知");
        AddInfo(items, "指挥", _track?.Conductor);
        AddInfo(items, "词作者", _track?.Lyricist);
        AddInfo(items, "描述", _track?.Description);
        AddInfo(items, "长描述", _track?.LongDescription);
        AddInfo(items, "注释", _track?.Comment ?? _musicItem.Comment);
        AddInfo(items, "语言", _track?.Language);

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("基本元数据", items));
    }

    private void AddPersonsAndRightsGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();
        AddInfo(items, "相关人员", _track?.InvolvedPeople);
        AddInfo(items, "版权", _track?.Copyright);
        AddInfo(items, "发行商", _track?.Publisher);
        AddInfo(items, "编码者", _track?.EncodedBy);
        AddInfo(items, "编码器信息", _track?.Encoder);
        AddInfo(items, "音频源 URL", _track?.AudioSourceUrl);

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("相关人员与权利", items));
    }

    private void AddClassificationAndSeriesGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();
        AddInfo(items, "流派", _track?.Genre);
        AddInfo(items, "内容组描述", _track?.Group);
        AddInfo(items, "系列标题/乐章名称", _track?.SeriesTitle);
        AddInfo(items, "系列部分/乐章索引", _track?.SeriesPart);

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("分类与系列", items));
    }

    private void AddDatesGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();

        if (_track?.Year is > 0)
            AddInfo(items, "录制年份", _track.Year.ToString());

        if (_track?.Date?.ToString("MM/dd HH:mm:ss") is { } date and not "01/01 00:00:00" &&
            _track.Date > DateTime.MinValue)
            AddInfo(items, "录制日期", date);

        if (_track?.OriginalReleaseDate is not null && _track.OriginalReleaseDate > DateTime.MinValue)
            AddInfo(items, "原始发行日期", _track.OriginalReleaseDate?.ToString("yyyy-MM-dd HH:mm:ss"));

        if (_track?.OriginalReleaseYear is > 0)
            AddInfo(items, "原始发行年份", _track.OriginalReleaseYear.ToString());

        if (_track?.PublishingDate != null && _track.PublishingDate > DateTime.MinValue)
            AddInfo(items, "发布日期", _track.PublishingDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("日期信息", items));
    }

    private void AddNumbersAndRatingsGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();

        AddInfo(items, "音轨编号", _track?.TrackNumber?.ToString() ?? _track?.TrackNumberStr);
        AddInfo(items, "音轨总数", _track?.TrackTotal?.ToString());
        AddInfo(items, "光盘编号", _track?.DiscNumber?.ToString());
        AddInfo(items, "光盘总数", _track?.DiscTotal?.ToString());

        if (_track?.Popularity is > 0)
            AddInfo(items, "流行度", $"{_track.Popularity:F1}%");

        if (_track?.BPM is > 0)
            AddInfo(items, "每分钟节拍数 (BPM)", _track.BPM.ToString());

        AddInfo(items, "回放增益", _musicItem.Gain > 0 ? $"{_musicItem.Gain:F2} dB" : null);

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("编号与评分", items));
    }

    private void AddIdentifiersGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();
        AddInfo(items, "产品 ID", _track?.ProductId);
        AddInfo(items, "ISRC", _track?.ISRC);
        AddInfo(items, "目录号", _track?.CatalogNumber);

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("标识符", items));
    }

    private void AddSortingGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();
        AddInfo(items, "标题排序", _track?.SortTitle);
        AddInfo(items, "艺术家排序", _track?.SortArtist);
        AddInfo(items, "专辑排序", _track?.SortAlbum);
        AddInfo(items, "专辑艺术家排序", _track?.SortAlbumArtist);

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("排序信息", items));
    }

    private void AddLyricsAndChaptersGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();

        // 简化处理，只显示是否存在
        if (_track?.Lyrics is { Count: > 0 })
            AddInfo(items, "歌词", "存在");

        if (_track?.Chapters is { Count: > 0 }) {
            AddInfo(items, "章节", "存在");
            AddInfo(items, "章节表描述", _track.ChaptersTableDescription);
        }

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("歌词与章节", items));
    }

    private void AddAdditionalFieldsGroup() {
        if (_track?.AdditionalFields is { Count: > 0 }) {
            var items = new AvaloniaList<MusicInfoKeyValuePair>();

            foreach (KeyValuePair<string, string> field in _track.AdditionalFields)
                AddInfo(items, field.Key, field.Value);

            if (items.Count > 0)
                MusicInfoGroups.Add(new MusicInfoGroup("附加字段", items));
        }
    }

    private void AddAudioTechnicalInfoGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();

        AddInfo(items, "音频格式", _track?.AudioFormat?.Name ?? "未知");
        AddInfo(items, "时长", $@"{_musicItem.Duration:hh\:mm\:ss} ({FormatValue(_track?.DurationMs, "ms")})");
        AddInfo(items, "比特率", FormatValue(_track?.Bitrate, "kbps"));
        AddInfo(items, "可变比特率", _track?.IsVBR == true ? "是" : "否");
        AddInfo(items, "采样率", FormatValue(_track?.SampleRate, "Hz"));
        AddInfo(
            items,
            "位深度",
            _track?.BitDepth == -1 ? I18NService.Lang.Translation["Not Applicable"] : FormatValue(_track?.BitDepth, "bit"));

        AddInfo(
            items,
            "声道数",
            $"{_track?.ChannelsArrangement?.NbChannels} ({_track?.ChannelsArrangement?.Description})");
        AddInfo(items, "编解码器族", GetCodecFamilyDescription(_track?.CodecFamily ?? -1));

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("音频技术信息", items));
    }

    private void AddMetadataFormatsGroup() {
        var items = new AvaloniaList<MusicInfoKeyValuePair>();

        if (_track?.MetadataFormats is { Count: > 0 }) {
            string[] formatNames = _track.MetadataFormats.Select(f => f.Name).ToArray();
            AddInfo(items, "存在的标签格式", string.Join(", ", formatNames));
        }

        if (_track?.SupportedMetadataFormats is { Count: > 0 }) {
            string[] supportedFormatNames = _track.SupportedMetadataFormats.Select(f => f.Name).ToArray();
            AddInfo(items, "支持的标签格式", string.Join(", ", supportedFormatNames));
        }

        if (items.Count > 0)
            MusicInfoGroups.Add(new MusicInfoGroup("元数据格式", items));
    }

    [RelayCommand]
    private void CopyText(MusicInfoKeyValuePair keyValuePair) {
        IClipboard? clipboard = App.TopLevel?.Clipboard;

        if (clipboard == null) {
            NotificationService.Error("复制失败！\n无法找到剪贴板！〒▽〒");

            return;
        }

        string textToCopy = SelectedText ?? $"{keyValuePair.Key} : {keyValuePair.Value}";
        clipboard.SetTextAsync(textToCopy).ContinueWith(LoggerService.HandleException).ConfigureAwait(false);
    }
}

public record struct MusicInfoKeyValuePair(string Key, string Value);

// 添加新的记录类型用于分组
public record MusicInfoGroup(string Header, AvaloniaList<MusicInfoKeyValuePair> Items);