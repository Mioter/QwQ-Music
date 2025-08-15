using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Shader;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Drawers;

public partial class MusicCoverPageViewModel : NavigationViewModel
{
    private const int COLOR_COUNT = 4;

    private static readonly CoverConfig _coverConfig = ConfigManager.UiConfig.CoverConfig;

    private static readonly List<Color> _defaultColors =
    [
        Color.Parse("#FFE2D9"),
        Color.Parse("#F3ECFE"),
        Color.Parse("#DFE7FF"),
        Color.Parse("#E4F2FF"),
    ];

    public MusicCoverPageViewModel()
        : base("播放")
    {
        MusicPlayerViewModelOnPlayerItemChanged(null, MusicPlayerViewModel.CurrentMusicItem);
        MusicPlayerViewModel.PlayerItemChanged += MusicPlayerViewModelOnPlayerItemChanged;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_OnProcessExit;
    }

    public static DrawerStatusViewModel DrawerStatusViewModel => DrawerStatusViewModel.Default;

    public static string OffsetName => LanguageModel.Lang[nameof(OffsetName)];

    public static MusicPlayerViewModel MusicPlayerViewModel => MusicPlayerViewModel.Default;

    public static RolledLyricConfig RolledLyric { get; } = ConfigManager.LyricConfig.RolledLyric;

    public static string ShaderCode => ShaderConstants.WaveWarpShader;

    [ObservableProperty] public partial Bitmap CoverImage { get; set; } = CacheManager.Default;

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

    [ObservableProperty] public partial List<Color> ColorsList { get; set; } = _defaultColors;

    private void CurrentDomain_OnProcessExit(object? sender, EventArgs e)
    {
        MusicPlayerViewModel.PlayerItemChanged -= MusicPlayerViewModelOnPlayerItemChanged;
        AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_OnProcessExit;
    }

    private async void MusicPlayerViewModelOnPlayerItemChanged(object? sender, MusicItemModel musicItem)
    {
        try
        {
            // 合并封面图片和颜色列表更新任务
            var coverTask = UpdateCoverImage(musicItem);
            var colorsTask = UpdateColorsList(musicItem);

            await Task.WhenAll(coverTask, colorsTask);

            UpdateThemeVariantFromColors();
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"{nameof(MusicPlayerViewModelOnPlayerItemChanged)} 发生错误 : {ex.Message}");
        }
    }

    private async Task UpdateCoverImage(MusicItemModel musicItem)
    {
        string? coverId = musicItem.CoverId;

        if (coverId != null)
        {
            var bitmap = await MusicExtractor.LoadBitmapFromFileAsync(coverId);

            if (bitmap != null)
            {
                if (!ConfigManager.UiConfig.CoverConfig.AllowNonSquareCover)
                {
                    CoverImage = Dispatcher.UIThread.Invoke(() => BitmapCropper.Crop(bitmap, 1.0));

                    return;
                }

                CoverImage = bitmap;

                return;
            }
        }

        // 尝试从音频文件中提取封面
        CoverImage = await MusicExtractor.GetCoverFromAudioAsync(musicItem.FilePath) ?? CacheManager.Default;
    }

    private async Task UpdateColorsList(MusicItemModel musicItem)
    {
        // 如果没有封面Id，直接使用默认颜色
        if (string.IsNullOrWhiteSpace(musicItem.CoverId))
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
        var colorsList = await GetColorPalette(musicItem.CoverId, COLOR_COUNT);

        // 缓存提取的颜色
        if (colorsList != null)
        {
            musicItem.CoverColors = colorsList.Select(x => x.ToString()).ToArray();
            await MusicItemManager.UpdateCoverColors(musicItem.FilePath, musicItem.CoverColors);
        }

        // 使用提取的颜色，为null则使用默认颜色
        ColorsList = colorsList ?? _defaultColors;
    }

    private void UpdateThemeVariantFromColors()
    {
        if (ColorsList.Count == 0)
        {
            DrawerStatusViewModel.Default.MusicPlayerPanelThemeVariant = "Default";

            return;
        }

        // 计算平均亮度
        double totalLuminance = ColorsList.Sum(c => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0);
        double avgLuminance = totalLuminance / ColorsList.Count;

        // 根据平均亮度设置主题（反色）
        DrawerStatusViewModel.Default.MusicPlayerPanelThemeVariant = avgLuminance > 0.5 ? "Light" : "Dark";
    }

    private static async Task<List<Color>?> GetColorPalette(string imagePath, int colorCount = 5)
    {
        // 尝试使用缓存的位图
        var bitmap = await MusicExtractor.LoadBitmapFromFileAsync(MusicExtractor.GetMusicCoverFullPath(imagePath));

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

    [RelayCommand]
    private static void OnVolumeBarPointerWheelChanged(PointerWheelEventArgs e)
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
