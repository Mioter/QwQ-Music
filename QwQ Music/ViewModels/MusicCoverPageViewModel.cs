using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;
using QwQ_Music.Services.Shader;
using QwQ_Music.Utilities.MessageBus;

namespace QwQ_Music.ViewModels;

public partial class MusicCoverPageViewModel : NavigationViewModel
{
    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public static RolledLyricsConfig RolledLyricsConfig { get; } = ConfigInfoModel.LyricConfig.RolledLyricsConfig;

    public MusicCoverPageViewModel()
        : base("播放")
    {
        MusicPlayerViewModel.CurrentMusicItemChanged += MusicPlayerViewModelOnCurrentMusicItemChanged;
        StrongMessageBus.Instance.Subscribe<ExitReminderMessage>(ExitReminderMessageHandler);
    }

    private void ExitReminderMessageHandler(ExitReminderMessage message)
    {
        MusicPlayerViewModel.CurrentMusicItemChanged -= MusicPlayerViewModelOnCurrentMusicItemChanged;
    }

    public static string ShaderCode => ShaderConstants.WaveWarpShader;

    [ObservableProperty]
    public partial bool IsShaderAnimationEnabled { get; set; } = true;

    [ObservableProperty]
    public partial Bitmap CoverImage { get; set; } = MusicExtractor.DefaultCover;

    private const int ColorCount = 4;

    private async void MusicPlayerViewModelOnCurrentMusicItemChanged(object? sender, MusicItemModel musicItem)
    {
        try
        {
            await UpdateCoverImage(musicItem);
            await UpdateColorsList(musicItem);
        }
        catch (Exception ex)
        {
            LoggerService.Error($"{nameof(MusicPlayerViewModelOnCurrentMusicItemChanged)} 发生错误 : {ex.Message}");
        }
    }

    private async Task UpdateCoverImage(MusicItemModel musicItem)
    {
        CoverImage =
            musicItem.CoverPath != null
                ? await MusicExtractor.LoadOriginalBitmap(musicItem.CoverPath) ?? MusicExtractor.DefaultCover
                : MusicExtractor.DefaultCover;
    }

    private async Task UpdateColorsList(MusicItemModel musicItem)
    {
        // 如果没有封面路径，直接使用默认颜色
        if (string.IsNullOrWhiteSpace(musicItem.CoverPath))
        {
            ColorsList = DefaultColors;
            OnPropertyChanged(nameof(ColorsList));
            return;
        }

        // 尝试从已缓存的颜色中获取
        if (musicItem.CoverColors is { Length: >= ColorCount })
        {
            ColorsList = [.. musicItem.CoverColors.Select(Color.Parse)];
            OnPropertyChanged(nameof(ColorsList));
            return;
        }

        // 提取新的颜色
        var colorsList = await ColorExtraction.GetColorPalette(
            musicItem.CoverPath,
            ColorCount,
            ConfigInfoModel.InterfaceConfig.SelectedColorExtractionAlgorithm
        );

        // 缓存提取的颜色
        if (colorsList != null)
        {
            musicItem.CoverColors = colorsList.Select(x => x.ToString()).ToArray();
        }

        // 使用提取的颜色，为null则使用默认颜色
        ColorsList = colorsList ?? DefaultColors;
        OnPropertyChanged(nameof(ColorsList));
    }

    public List<Color> ColorsList { get; set; } = DefaultColors;

    private static readonly List<Color> DefaultColors =
    [
        Color.Parse("#FFE2D9"),
        Color.Parse("#F3ECFE"),
        Color.Parse("#DFE7FF"),
        Color.Parse("#E4F2FF"),
    ];
}
