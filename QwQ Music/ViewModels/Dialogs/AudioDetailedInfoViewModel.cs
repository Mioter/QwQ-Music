using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class AudioDetailedInfoViewModel(MusicItemModel musicItem, MusicTagExtensions musicTagExtensions)
    : ViewModelBase
{
    public AvaloniaList<MusicInfoKeyValuePair> MusicInfoKeyValuePairs { get; } =
    [
        new("标题", musicItem.Title),
        new("艺术家", musicItem.Artists),
        new("专辑", musicItem.Album),
        new("作曲", musicItem.Composer ?? "未知作曲"),
        new("时长", musicItem.Duration.ToString(@"hh\:mm\:ss")),
        new("文件路径", musicItem.FilePath),
        new("文件大小", musicItem.FileSize ?? "未知"),
        new("编码格式", musicItem.EncodingFormat ?? "未知"),
        new("采样率", $"{musicTagExtensions.SamplingRate} Hz"),
        new("声道数", musicTagExtensions.Channels.ToString()),
        new("比特率", $"{musicTagExtensions.Bitrate} kbps"),
        new("位深度", $"{musicTagExtensions.BitsPerSample} bit"),
        new("可变比特率", musicTagExtensions.IsVbr ? "是" : "否"),
    ];

    public string? SelectedText { get; set; }

    public AudioDetailedInfoViewModel MoreDetailedInfor()
    {
        // 添加额外的信息项
        if (!string.IsNullOrEmpty(musicItem.Comment))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("注释", musicItem.Comment));
        }

        if (musicItem.Gain > -1.0)
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("回放增益", $"{musicItem.Gain:F2} dB"));
        }

        // 从MusicTagExtensions加载扩展信息
        if (!string.IsNullOrEmpty(musicTagExtensions.Genre))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("流派", musicTagExtensions.Genre));
        }

        if (musicTagExtensions.Year.HasValue)
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("年份", musicTagExtensions.Year.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(musicTagExtensions.Copyright))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("版权", musicTagExtensions.Copyright));
        }

        if (musicTagExtensions.Disc > 0)
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("光盘编号", musicTagExtensions.Disc.ToString()));
        }

        if (musicTagExtensions.Track > 0)
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("音轨编号", musicTagExtensions.Track.ToString()));
        }

        if (!string.IsNullOrEmpty(musicTagExtensions.OriginalAlbum))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("原始专辑", musicTagExtensions.OriginalAlbum));
        }

        if (!string.IsNullOrEmpty(musicTagExtensions.OriginalArtist))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("原始艺术家", musicTagExtensions.OriginalArtist));
        }

        if (!string.IsNullOrEmpty(musicTagExtensions.AlbumArtist))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("专辑艺术家", musicTagExtensions.AlbumArtist));
        }

        if (!string.IsNullOrEmpty(musicTagExtensions.Publisher))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("发行商", musicTagExtensions.Publisher));
        }

        if (!string.IsNullOrEmpty(musicTagExtensions.Description))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("描述", musicTagExtensions.Description));
        }

        if (!string.IsNullOrEmpty(musicTagExtensions.Language))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("语言", musicTagExtensions.Language));
        }

        if (!string.IsNullOrEmpty(musicTagExtensions.AudioFormat))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("音频格式", musicTagExtensions.AudioFormat));
        }

        if (!string.IsNullOrEmpty(musicTagExtensions.EncoderInfo))
        {
            MusicInfoKeyValuePairs.Add(new MusicInfoKeyValuePair("编码器信息", musicTagExtensions.EncoderInfo));
        }

        return this;
    }

    [RelayCommand]
    private async Task CopyText(MusicInfoKeyValuePair keyValuePair)
    {
        // 使用topLevel进行操作
        var clipboard = App.TopLevel?.Clipboard;

        if (clipboard == null)
        {
            NotificationService.Error("复制失败！\n无法找到剪贴板！〒▽〒");

            return;
        }

        await clipboard.SetTextAsync(SelectedText ?? $"{keyValuePair.Key} : {keyValuePair.Value}");
    }
}

public record struct MusicInfoKeyValuePair(string Key, string Value);
