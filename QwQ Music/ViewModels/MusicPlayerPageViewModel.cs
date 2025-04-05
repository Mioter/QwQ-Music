using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Services.Shader;
using QwQ_Music.Utilities.MessageBus;

namespace QwQ_Music.ViewModels;

public partial class MusicPlayerPageViewModel : ViewModelBase
{
    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public MusicPlayerPageViewModel()
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
    
    private const int ColorCount = 4;

    private async void MusicPlayerViewModelOnCurrentMusicItemChanged(object? sender, MusicItemModel musicItem)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(musicItem.CoverPath))
            {

                List<Color>? colorsList = null;
                await Task.Run(() =>
                {
                    colorsList = musicItem.CoverColors is { Length: >= ColorCount } 
                        ? [..musicItem.CoverColors.Select(Color.Parse)] 
                        : ColorExtraction.GetColorPalette(musicItem.CoverPath, ColorCount,ColorExtractionAlgorithm.OctTree);
                });

                if (colorsList != null)
                {
                    musicItem.CoverColors = colorsList.Select(x => x.ToString()).ToArray();
                }
                
                ColorsList = colorsList ?? GetDefaultColors;
            }
            else
            {
                ColorsList =  GetDefaultColors;
            }
            
            OnPropertyChanged(nameof(ColorsList));
        }
        catch (Exception ex)
        {
            LoggerService.Error($"{nameof(MusicPlayerViewModelOnCurrentMusicItemChanged)} 发生错误 : {ex.Message}");
        }
    }

    public List<Color>? ColorsList { get; set; } = GetDefaultColors;

    private static List<Color>? GetDefaultColors =>
        [Colors.AntiqueWhite, Colors.AliceBlue, Colors.PapayaWhip, Colors.NavajoWhite];
}
