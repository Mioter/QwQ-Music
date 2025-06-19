using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Helper;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Services;
using QwQ_Music.Services.Audio;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ.Avalonia.Utilities.MessageBus;
using QwQ.Avalonia.Utilities.TaskManager;
using SoundFlow.Modifiers;
using PlayerConfig = QwQ_Music.Models.ConfigModels.PlayerConfig;

namespace QwQ_Music.ViewModels.Pages;

public partial class PlayConfigPageViewModel : ViewModelBase
{
    public PlayerConfig PlayerConfig { get; } = ConfigManager.PlayerConfig;

    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public AudioModifierConfig AudioModifierConfig { get; } = ConfigManager.AudioModifierConfig;

    public PlayConfigPageViewModel()
    {
        ReplayGainCalculator.CalcCompletedChanged += ReplayGainCalculatorOnCalcCompletedChanged;
        MessageBus
            .ReceiveMessage<ExitReminderMessage>(this)
            .WithHandler(ExitReminderMessageHandler)
            .AsWeakReference()
            .Subscribe();
        MessageBus
            .ReceiveMessage<LoadCompletedMessage>(this)
            .WithHandler(LoadCompletedMessageHandler)
            .AsWeakReference()
            .Subscribe();
    }

    private async void LoadCompletedMessageHandler(LoadCompletedMessage obj, object? sender)
    {
        try
        {
            if (obj.Name == nameof(MusicPlayerViewModel.MusicItems))
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
    }

    public FadeModifier.FadeCurve[] FadeCurves { get; } = EnumHelper<FadeModifier.FadeCurve>.ToArray();

    #region 回放增益

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
            foreach (var musicItem in MusicPlayerViewModel.MusicItems)
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
            MusicPlayerViewModel.MusicItems.Count <= 0
            || NumberOfCompletedCalc == MusicPlayerViewModel.MusicItems.Count
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

        var itemsToProcess = MusicPlayerViewModel.MusicItems.Where(item => item.Gain <= 0).ToList();

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
        if (MusicPlayerViewModel.MusicItems.Count <= 0)
            return;

        await Task.Run(() => NumberOfCompletedCalc = MusicPlayerViewModel.MusicItems.Count(x => x.Gain > 0));
    }

    #endregion

    #region 随机颜色

    private readonly Random _random = new();

    public IBrush RandomColor => GeneratePastelColor();

    private SolidColorBrush GeneratePastelColor()
    {
        // 生成明亮的色相（0-360度）
        double hue = _random.Next(0, 360);

        // 保持高饱和度（70%-100%）
        double saturation = _random.Next(70, 100) / 100.0;

        // 保持较高亮度（70%-90%）
        double value = _random.Next(70, 90) / 100.0;

        var color = HsvToRgb(hue, saturation, value);
        return new SolidColorBrush(color);
    }

    private static Color HsvToRgb(double hue, double saturation, double value)
    {
        int hi = (int)(hue / 60) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        double v = value * 255;
        byte vByte = (byte)Math.Round(v);
        double p = v * (1 - saturation);
        byte pByte = (byte)Math.Round(p);
        double q = v * (1 - f * saturation);
        byte qByte = (byte)Math.Round(q);
        double t = v * (1 - (1 - f) * saturation);
        byte tByte = (byte)Math.Round(t);

        return hi switch
        {
            0 => Color.FromRgb(vByte, tByte, pByte),
            1 => Color.FromRgb(qByte, vByte, pByte),
            2 => Color.FromRgb(pByte, vByte, tByte),
            3 => Color.FromRgb(pByte, qByte, vByte),
            4 => Color.FromRgb(tByte, pByte, vByte),
            _ => Color.FromRgb(vByte, pByte, qByte),
        };
    }

    #endregion
}
