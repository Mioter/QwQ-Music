using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Audio;
using QwQ_Music.Common.Audio.SoundModifier;
using QwQ_Music.Common.Helper;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public partial class PlayConfigPageViewModel : ViewModelBase
{
    public PlayerConfig PlayerConfig { get; } = ConfigManager.PlayerConfig;

    public static MusicItemManager MusicItemManager => MusicItemManager.Default;

    public SoundModifierConfig SoundModifierConfig { get; } = ConfigManager.SoundModifierConfig;

    public PlayConfigPageViewModel()
    {
        NavigateService.ComeToOneselfEvents["播放"] = ComeToOneselfEvent;
    }

    private void ComeToOneselfEvent()
    {
        NumberOfCompletedCalc = MusicItemManager.MusicItems.Count(item => item.Gain > 0);
    }

    public Dictionary<FadeModifier.FadeCurve,string> FadeCurves { get; } = new()
    {
        [FadeModifier.FadeCurve.Cosine] = "余弦渐变",
        [FadeModifier.FadeCurve.Exponential] = "指数渐变",
        [FadeModifier.FadeCurve.Linear] = "线性渐变",
    };

    #region 回放增益
    
    [ObservableProperty]
    public partial int NumberOfCompletedCalc { get; set; }

    public static MusicReplayGainStandard[] MusicReplayGainStandardList { get; set; } =
        EnumHelper<MusicReplayGainStandard>.ToArray();

    [ObservableProperty]
    public partial MusicReplayGainStandard SelectedMusicReplayGainStandard { get; set; } =
        MusicReplayGainStandard.Streaming;

    [ObservableProperty]
    public partial double CustomMusicReplayGainStandard { get; set; } = 12;
    

    [ObservableProperty]
    public partial CancellationTokenSource? CancellationTokenSource { get; set; }


    [RelayCommand]
    private async Task ClearCallbackGain()
    {
        await Task.Run(() =>
        {
            foreach (var musicItem in MusicItemManager.MusicItems.Where(musicItem => musicItem.Gain >= 0))
            {
                musicItem.Gain = 0;
                NumberOfCompletedCalc--;
            }
        });
    }

    [RelayCommand]
    private async Task ToggleCalculation()
    {
        if (
            MusicItemManager.Count <= 0
            || NumberOfCompletedCalc == MusicItemManager.Count
        )
            return;

        await StartNewCalculation();
    }

    private async Task StartNewCalculation()
    {
        // 创建新的取消令牌源
        CancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = CancellationTokenSource.Token;

        var itemsToProcess = MusicItemManager.MusicItems.Where(item => item.Gain <= 0).ToList();

        try
        {
            await Task.Run(async () =>
            {
                // 使用并行处理
                await Parallel.ForEachAsync(itemsToProcess,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        CancellationToken = cancellationToken,
                    },
                    async (itemModel, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();

                        double gain = await Task.Run(() => AudioPreprocessor.CalcGainOfMusicItem(itemModel), ct);
                        itemModel.Gain = gain;

                        NumberOfCompletedCalc++;
                    });
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            CleanupTask();
        }
        finally
        {
            CleanupTask();
        }
    }
    
    [RelayCommand]
    public void CancelCalculation()
    {
        CancellationTokenSource?.Cancel();
    }
    
    private void CleanupTask()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = null;
    }
    
    #endregion
}
