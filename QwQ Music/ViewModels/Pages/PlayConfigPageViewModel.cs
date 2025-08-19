using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Audio;
using QwQ_Music.Common.Audio.SoundModifier;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;
using Ursa.Controls;

namespace QwQ_Music.ViewModels.Pages;

public partial class PlayConfigPageViewModel : ViewModelBase
{
    public PlayConfigPageViewModel()
    {
        NavigateService.ComeToOneselfEvents["播放"] = ComeToOneselfEvent;
    }

    public PlayerConfig PlayerConfig { get; } = ConfigManager.PlayerConfig;

    public static MusicItemManager MusicItemManager => MusicItemManager.Default;

    public SoundModifierConfig SoundModifierConfig { get; } = ConfigManager.SoundModifierConfig;

    public Dictionary<FadeModifier.FadeCurve, string> FadeCurves { get; } = new()
    {
        [FadeModifier.FadeCurve.Cosine] = "余弦渐变",
        [FadeModifier.FadeCurve.Exponential] = "指数渐变",
        [FadeModifier.FadeCurve.Linear] = "线性渐变",
    };

    private void ComeToOneselfEvent()
    {
        NumberOfCompletedCalc = MusicItemManager.MusicItems.Count(item => item.Gain > 0);
    }

    #region 回放增益

    [ObservableProperty] public partial int NumberOfCompletedCalc { get; set; }

    public static Dictionary<MusicReplayGainStandard, string> MusicReplayGainStandards { get; set; } = new()
    {
        [MusicReplayGainStandard.Streaming] = "流媒体优化（-16 LUFS）",
        [MusicReplayGainStandard.EbuR128] = "EBU R128广播标准（-23 LUFS）",
        [MusicReplayGainStandard.ReplayGain2] = "ReplayGain 2.0标准（-18 LUFS）",
        [MusicReplayGainStandard.Custom] = "自定义目标响度",
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMusicReplayGainStandardDescription))]
    public partial MusicReplayGainStandard SelectedMusicReplayGainStandard { get; set; } =
        MusicReplayGainStandard.Streaming;

    public string SelectedMusicReplayGainStandardDescription => MusicReplayGainStandards[SelectedMusicReplayGainStandard];

    [ObservableProperty] public partial CancellationTokenSource? CancellationTokenSource { get; set; }

    [RelayCommand]
    private async Task ClearCallbackGain()
    {
        var result = await MessageBox.ShowOverlayAsync(
            "你真的要清空已经计算的回放增益值吗？",
            "警告",
            icon: MessageBoxIcon.Warning,
            button: MessageBoxButton.YesNo
        );

        if (result != MessageBoxResult.Yes)
            return;

        var musicItems = MusicItemManager.MusicItems.Where(item => item.Gain > 0).ToList();

        foreach (var musicItem in musicItems)
        {
            musicItem.Gain = 0;
            NumberOfCompletedCalc--;
        }

        await Task.Run(() =>
        {
            foreach (var musicItem in musicItems)
            {
                MusicItemManager.Update(musicItem.FilePath, new Dictionary<string, object?>
                {
                    [nameof(MusicItemModel.Gain)] = musicItem.Gain,
                });
            }
        });

        NotificationService.Info("回放增益值已清空！");
    }

    [RelayCommand]
    private async Task ToggleCalculation()
    {
        if (
            MusicItemManager.Count <= 0
         || NumberOfCompletedCalc == MusicItemManager.Count
        )
        {
            NotificationService.Info("已经没有需要计算回放增益的音乐啦~");

            return;
        }

        await StartNewCalculation();
    }

    private async Task StartNewCalculation()
    {
        CancellationTokenSource = new CancellationTokenSource();

        try
        {
            var itemsToProcess = MusicItemManager.MusicItems.Where(item => item.Gain <= 0).ToList();
            await ProcessItemsAsync(itemsToProcess, CancellationTokenSource.Token);
            NotificationService.Info("回放增益计算结束！");
        }
        catch (OperationCanceledException)
        {
            NotificationService.Info("回放增益计算已取消！");
        }
        catch (Exception e)
        {
            NotificationService.Error($"计算任务出错退出！\n{e.Message}");
            await LoggerService.ErrorAsync($"计算任务出错退出！\n{e.Message}\n{e.StackTrace}");
        }
        finally
        {
            CleanupTask();
        }
    }

    private async Task ProcessItemsAsync(List<MusicItemModel> items, CancellationToken cancellationToken)
    {
        await Parallel.ForEachAsync(items,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken,
            },
            async (item, ct) =>
            {
                try
                {
                    await ProcessSingleItemAsync(item, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) // 取消异常
                {
                    NotificationService.Info("回放增益计算已取消！");
                }
                catch (Exception ex)
                {
                    NotificationService.Error($"计算{item.Title}的回放增益时出现错误：\n{ex.Message}");
                    await LoggerService.ErrorAsync($"计算{item.Title}的回放增益时出现错误：\n{ex.Message}\n{ex.StackTrace}");
                }
            });
    }

    private async Task ProcessSingleItemAsync(MusicItemModel item, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            double gain = AudioPreprocessor.CalcGainOfMusicItem(
                item, SelectedMusicReplayGainStandard, PlayerConfig.CustomMusicReplayGainStandard);

            MusicItemManager.Update(item.FilePath, new Dictionary<string, object?>
            {
                [nameof(MusicItemModel.Gain)] = gain,
            });

            item.Gain = gain;
            NumberOfCompletedCalc++;
        }, cancellationToken);
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
