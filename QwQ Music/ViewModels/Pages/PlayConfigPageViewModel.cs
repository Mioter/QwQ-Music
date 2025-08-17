using System.Collections.Generic;
using QwQ_Music.Common.Audio.SoundModifier;
using QwQ_Music.Common.Manager;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public class PlayConfigPageViewModel : ViewModelBase
{
    public PlayerConfig PlayerConfig { get; } = ConfigManager.PlayerConfig;

    public static MusicPlayerViewModel MusicPlayerViewModel => MusicPlayerViewModel.Default;

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

    /*#region 回放增益

    [ObservableProperty]
    public partial TaskController? TaskController { get; set; }

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
            foreach (var musicItem in MusicPlayerViewModel.MusicLists)
            {
                if (musicItem.Gain < 0)
                    continue;
                musicItem.Gain = -1;
                NumberOfCompletedCalc--;
            }
        });
    }

    [RelayCommand]
    private async Task ToggleCalculation()
    {
        if (
            MusicPlayerViewModel.MusicLists.Count <= 0
            || NumberOfCompletedCalc == MusicPlayerViewModel.MusicLists.Count
        )
            return;

        // 状态判断和操作一体化处理
        if (TaskController != null)
        {
            switch (TaskController.State)
            {
                case TaskExecutionState.Error:
                case TaskExecutionState.NotStarted:
                case TaskExecutionState.Stopped:
                case TaskExecutionState.Cancelled:
                case TaskExecutionState.Completed:
                case TaskExecutionState.Timeout:
                    StartNewCalculation();
                    break;

                case TaskExecutionState.Running:
                    await TaskController.PauseAsync();
                    break;

                case TaskExecutionState.Paused:
                    await TaskController.StartAsync();
                    break;
                default:
                    await LoggerService.ErrorAsync($"不存在的任务状态: {TaskController.State}");
                    break;
            }
        }
        else
        {
            StartNewCalculation();
        }

        UpdatePromptText();
    }

    private void UpdatePromptText()
    {
        CalculationButtonText = TaskController?.State switch
        {
            TaskExecutionState.Running => "暂停 \u23f8",
            TaskExecutionState.Paused => "继续 ▶",
            _ => "开始 ▶",
        };
    }

    private void StartNewCalculation()
    {
        TaskController = new TaskController();

        var itemsToProcess = MusicPlayerViewModel.MusicLists.Where(item => item.Gain <= 0).ToList();

        TaskManager
            .CreateMultiTask(
                itemsToProcess,
                item =>
                {
                    var ex = MusicExtractor.ExtractExtensionsInfo(item.FilePath);
                    item.Gain = AudioHelper.CalcGainOfMusicItem(item.FilePath, ex.SamplingRate, ex.Channels);
                    return Task.CompletedTask;
                }
            )
            .SetController(TaskController)
            .SetErrorHandler(ex =>
            {
                if (ex is OperationCanceledException)
                {
                    CleanupTask();
                }
            })
            .SetCompleteCallback(_ => CleanupTask())
            .RunAsync();
    }

    private void CleanupTask()
    {
        UpdatePromptText();
        TaskController?.Dispose();
        TaskController = null;
    }

    [RelayCommand]
    private async Task CancelCalcCallbackGain()
    {
        if (TaskController is null)
            return;

        await TaskController.CancelAsync();
        CleanupTask();
    }

    private async Task RefreshNumberOfCompletedCalc()
    {
        if (MusicPlayerViewModel.MusicLists.Count <= 0)
            return;

        await Task.Run(() => NumberOfCompletedCalc = MusicPlayerViewModel.MusicLists.Count(x => x.Gain > 0));
    }

    #endregion*/
}
