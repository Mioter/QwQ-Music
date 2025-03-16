using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using QwQ_Music.Models;
using QwQ_Music.Services;

namespace QwQ_Music.ViewModels;

public class MusicPlayerPageViewModel : ViewModelBase
{
    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public MusicPlayerPageViewModel()
    {
        MusicPlayerViewModel.CurrentMusicItemChanging += MusicPlayerViewModelOnCurrentMusicItemChanging;
        ExitReminderService.ExitReminder += ExitReminderServiceOnExitReminder;
    }

    private void ExitReminderServiceOnExitReminder(object? sender, EventArgs e)
    {
        ExitReminderService.ExitReminder -= ExitReminderServiceOnExitReminder;
        MusicPlayerViewModel.CurrentMusicItemChanging -= MusicPlayerViewModelOnCurrentMusicItemChanging;
    }

    private bool IsNightMode { get; set; }
    private const int ColorCount = 2;

    private async void MusicPlayerViewModelOnCurrentMusicItemChanging(object? sender, MusicItemModel musicItem)
    {
        try
        {
            List<Color> colorsList = [];
            await Task.Run(() =>
            {
                if (musicItem.CoverColors is { Length: >= ColorCount })
                {
                    colorsList.AddRange(musicItem.CoverColors.Select(Color.Parse));
                }
                else
                {
                    colorsList = string.IsNullOrWhiteSpace(musicItem.CoverPath)
                        ? [Colors.AntiqueWhite, Colors.AliceBlue, Colors.PeachPuff]
                        : ColorExtraction.GetColorPalette(musicItem.CoverPath, ColorCount,ColorExtraction.ColorTone.Bright);
                    musicItem.CoverColors = colorsList.Select(x => x.ToString()).ToArray();
                }
            });

            ColorsList = colorsList;
            OnPropertyChanged(nameof(ColorsList));
        }
        catch (Exception ex)
        {
            LoggerService.Error($"{nameof(MusicPlayerViewModelOnCurrentMusicItemChanging)}: {ex.Message}");
        }
    }

    public List<Color> ColorsList { get; set; } = [Colors.AntiqueWhite, Colors.AliceBlue, Colors.PeachPuff];
}
