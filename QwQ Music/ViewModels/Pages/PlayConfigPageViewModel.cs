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
using QwQ_Music.Common.Utilities;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public partial class PlayConfigPageViewModel : ViewModelBase
{
    public PlayerConfig PlayerConfig { get; } = ConfigManager.PlayerConfig;

    public static MusicItemManager MusicItemManager => MusicItemManager.Default;

    public SoundModifierConfig SoundModifierConfig { get; } = ConfigManager.SoundModifierConfig;

    /*public PlayConfigPageViewModel()
    {
        ReplayGainCalculator.CalcCompletedChanged += ReplayGainCalculatorOnCalcCompletedChanged;
        MessageBus
            .ReceiveMessage<ExitReminderMessage>(this)
            .WithHandler(ExitReminderMessageHandler)
            .AsWeakReference()
            .Subscribe();
        MessageBus
            .ReceiveMessage<OperateCompletedMessage>(this)
            .WithHandler(LoadCompletedMessageHandler)
            .AsWeakReference()
            .Subscribe();
    }

    private async void LoadCompletedMessageHandler(OperateCompletedMessage obj, object? sender)
    {
        try
        {
            if (obj.Name == nameof(MusicPlayerViewModel.MusicLists))
            {
                await RefreshNumberOfCompletedCalc();
            }
        }
        catch (Exception e)
        {
            await LoggerService.ErrorAsync($"在加载时刷新已计算增益的歌词数量时出错: {e.Message}");
        }
    }

    private void ReplayGainCalculatorOnCalcCompletedChanged(object? sender, EventArgs e) => NumberOfCompletedCalc++;

    private void ExitReminderMessageHandler(ExitReminderMessage obj, object? sender)
    {
        ReplayGainCalculator.CalcCompletedChanged -= ReplayGainCalculatorOnCalcCompletedChanged;
    }*/

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
    public partial string CalculationButtonText { get; set; } = "开始 ▶";

    [RelayCommand]
    private async Task ClearCallbackGain()
    {
        await Task.Run(() =>
        {
            foreach (var musicItem in MusicItemManager.MusicItems.Where(musicItem => musicItem.Gain >= 0))
            {
                musicItem.Gain = -1;
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
    

    private CancellationTokenSource? _cancellationTokenSource;

    private async Task StartNewCalculation()
    {
        // 如果有正在运行的任务，先取消它
        if (_cancellationTokenSource != null)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
        }

        // 创建新的取消令牌源
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var itemsToProcess = MusicItemManager.MusicItems.Where(item => item.Gain <= 0).ToList();
        
        try
        {
            await Task.Run(() =>
            {
                using var audioSlicer = new AudioSlicer();

                foreach (var itemModel in itemsToProcess)
                {
                    // 检查是否已请求取消
                    cancellationToken.ThrowIfCancellationRequested();

                    itemModel.Gain = AudioPreprocessor.CalcGainOfMusicItem(audioSlicer, itemModel);
                    NumberOfCompletedCalc++;

                    // 可选：在每次处理后再次检查取消状态
                    cancellationToken.ThrowIfCancellationRequested();
                }
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
        _cancellationTokenSource?.Cancel();
    }


    private void CleanupTask()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }


    #endregion
}
