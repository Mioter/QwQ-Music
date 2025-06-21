using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Services.Shader;
using QwQ_Music.ViewModels.UserControls;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.Pages;
using QwQ.Avalonia.Utilities.MessageBus;
using CoverConfig = QwQ_Music.Models.ConfigModels.CoverConfig;
using RolledLyricConfig = QwQ_Music.Models.ConfigModels.RolledLyricConfig;

namespace QwQ_Music.ViewModels.Pages;

public partial class MusicCoverPageViewModel : NavigationViewModel
{
    public static string OffsetName => LanguageModel.Lang[nameof(OffsetName)];

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public static RolledLyricConfig RolledLyric { get; } = ConfigManager.LyricConfig.RolledLyri;

    private static readonly CoverConfig _coverConfig = ConfigManager.InterfaceConfig.CoverConfig;

    public MusicCoverPageViewModel()
        : base("播放")
    {
        MusicPlayerViewModelOnCurrentMusicItemChanged(null, MusicPlayerViewModel.CurrentMusicItem);
        MusicPlayerViewModel.CurrentMusicItemChanged += MusicPlayerViewModelOnCurrentMusicItemChanged;

        MessageBus
            .ReceiveMessage<ExitReminderMessage>(this)
            .WithHandler(ExitReminderMessageHandler)
            .AsWeakReference()
            .Subscribe();
    }

    private void ExitReminderMessageHandler(ExitReminderMessage message, object _)
    {
        MusicPlayerViewModel.CurrentMusicItemChanged -= MusicPlayerViewModelOnCurrentMusicItemChanged;
    }

    public static string ShaderCode => ShaderConstants.WaveWarpShader;

    [ObservableProperty]
    public partial bool IsShaderAnimationEnabled { get; set; } = true;

    [ObservableProperty]
    public partial Bitmap CoverImage { get; set; } = MusicExtractor.DefaultCover;

    public double SelectLyricsTimePoint
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                MusicPlayerViewModel.Seek(field);
            }
        }
    }

    [ObservableProperty]
    public partial List<Color> ColorsList { get; set; } = _defaultColors;

    public static ThemeVariant ThemeVariant { get; set; } = ThemeVariant.Dark;

    private const int COLOR_COUNT = 4;

    private async void MusicPlayerViewModelOnCurrentMusicItemChanged(object? sender, MusicItemModel musicItem)
    {
        try
        {
            // 合并封面图片和颜色列表更新任务
            var coverTask = UpdateCoverImage(musicItem);
            var colorsTask = UpdateColorsList(musicItem);

            await Task.WhenAll(coverTask, colorsTask).ConfigureAwait(false);

            UpdateThemeVariantFromColors();

            await MessageBus
                .CreateMessage(new ThemeColorChangeMessage(ThemeVariant, typeof(MusicCoverPage)))
                .FromSender(this)
                .AddReceivers(typeof(MusicPlayListViewModel), typeof(MainWindowViewModel))
                .PublishAsync();
        }
        catch (Exception ex)
        {
            await LoggerService
                .ErrorAsync($"{nameof(MusicPlayerViewModelOnCurrentMusicItemChanged)} 发生错误 : {ex.Message}")
                .ConfigureAwait(false);
        }
    }

    private async Task UpdateCoverImage(MusicItemModel musicItem)
    {
        string? currentCoverPath = musicItem.CoverPath;
        bool shouldRetry;

        do
        {
            shouldRetry = false;

            if (currentCoverPath != null)
            {
                var bitmap = await MusicExtractor.LoadOriginalBitmap(currentCoverPath).ConfigureAwait(false);
                if (bitmap != null)
                {
                    CoverImage = bitmap;
                    return;
                }
            }

            // 尝试从音频文件中提取封面
            string? newCoverPath = await MusicExtractor
                .ExtractAndSaveCoverFromAudioAsync(musicItem.FilePath)
                .ConfigureAwait(false);
            if (newCoverPath != null)
            {
                currentCoverPath = newCoverPath;
                musicItem.CoverPath = Path.GetFileName(currentCoverPath); // 更新模型中的路径
                shouldRetry = true; // 重试加载新路径的封面
            }
            else
            {
                CoverImage = MusicExtractor.DefaultCover;
            }
        } while (shouldRetry);
    }

    private async Task UpdateColorsList(MusicItemModel musicItem)
    {
        // 如果没有封面路径，直接使用默认颜色
        if (string.IsNullOrWhiteSpace(musicItem.CoverPath))
        {
            ColorsList = _defaultColors;
            return;
        }

        // 尝试从已缓存的颜色中获取
        if (musicItem.CoverColors is { Length: >= COLOR_COUNT })
        {
            ColorsList = [.. musicItem.CoverColors.Select(Color.Parse)];
            return;
        }

        // 提取新的颜色
        var colorsList = await GetColorPalette(musicItem.CoverPath, COLOR_COUNT);

        // 缓存提取的颜色
        if (colorsList != null)
        {
            musicItem.CoverColors = colorsList.Select(x => x.ToString()).ToArray();
        }

        // 使用提取的颜色，为null则使用默认颜色
        ColorsList = colorsList ?? _defaultColors;
    }

    private void UpdateThemeVariantFromColors()
    {
        if (ColorsList.Count == 0)
        {
            ThemeVariant = ThemeVariant.Default;
            return;
        }

        // 计算平均亮度
        double totalLuminance = ColorsList.Sum(c => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0);
        double avgLuminance = totalLuminance / ColorsList.Count;

        // 根据平均亮度设置主题（反色）
        ThemeVariant = avgLuminance > 0.5 ? ThemeVariant.Light : ThemeVariant.Dark;

        OnPropertyChanged(nameof(ThemeVariant));
    }

    private static async Task<List<Color>?> GetColorPalette(string imagePath, int colorCount = 5)
    {
        // 尝试使用缓存的位图
        var bitmap = await MusicExtractor.LoadCompressedBitmap(imagePath);
        return bitmap == null
            ? null // 缓存不存在直接返回null
            : ColorExtraction.GetColorPaletteFromBitmap(
                bitmap,
                colorCount,
                _coverConfig.SelectedColorExtractionAlgorithm,
                _coverConfig.IgnoreWhite,
                _coverConfig.ToLab,
                _coverConfig.UseKMeansPp
            );
    }

    private static readonly List<Color> _defaultColors =
    [
        Color.Parse("#FFE2D9"),
        Color.Parse("#F3ECFE"),
        Color.Parse("#DFE7FF"),
        Color.Parse("#E4F2FF"),
    ];
    
    [RelayCommand]
    private  void OnVolumeBarPointerWheelChanged(PointerWheelEventArgs e)
    {
        // 阻止事件冒泡到父级元素
        e.Handled = true;

        switch (e.Delta.Y)
        {
            // 根据你的需求处理滚轮滚动事件
            case > 0:
                MusicPlayerViewModel.Volume += 2;
                break;
            case < 0:
                MusicPlayerViewModel.Volume -= 2;
                break;
        }
    }
}
