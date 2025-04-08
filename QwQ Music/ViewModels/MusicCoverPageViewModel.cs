using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Services.Shader;
using QwQ_Music.Utilities.MessageBus;

namespace QwQ_Music.ViewModels;

public partial class MusicCoverPageViewModel : NavigationViewModel
{
    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

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
            CoverImage =
                musicItem.CoverPath != null
                    ? MusicExtractor.LoadOriginalBitmap(musicItem.CoverPath) ?? MusicExtractor.DefaultCover
                    : CoverImage = MusicExtractor.DefaultCover;

            if (!string.IsNullOrWhiteSpace(musicItem.CoverPath))
            {
                List<Color>? colorsList = null;
                await Task.Run(() =>
                {
                    if (musicItem.CoverColors is { Length: >= ColorCount })
                    {
                        colorsList = [.. musicItem.CoverColors.Select(Color.Parse)];
                    }
                    else
                    {
                        colorsList = ColorExtraction.GetColorPalette(
                            musicItem.CoverPath,
                            ColorCount,
                            ConfigInfoModel.InterfaceConfig.SelectedColorExtractionAlgorithm
                        );

                        if (colorsList != null)
                        {
                            musicItem.CoverColors = colorsList.Select(x => x.ToString()).ToArray();
                        }
                    }
                });

                ColorsList = colorsList ?? DefaultColors;
            }
            else
            {
                ColorsList = DefaultColors;
            }

            OnPropertyChanged(nameof(ColorsList));
        }
        catch (Exception ex)
        {
            LoggerService.Error($"{nameof(MusicPlayerViewModelOnCurrentMusicItemChanged)} 发生错误 : {ex.Message}");
        }
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
