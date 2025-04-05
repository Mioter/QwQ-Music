using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Controls;
using QwQ_Music.Helper;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;
using QwQ_Music.Services.Audio;
using QwQ_Music.Utilities;
using QwQ_Music.Utilities.MessageBus;
using QwQ_Music.Utilities.Tasks;
using SoundFlow.Modifiers;

namespace QwQ_Music.ViewModels;

public partial class SoundEffectConfigViewModel : NavigationViewModel
{
    public SoundEffectConfigViewModel()
        : base("音效")
    {
        SoundEffectConfig = ConfigInfoModel.SoundEffectConfig;
        _ = RefreshNumberOfCompletedCalc();

        ReplayGainCalculator.CalcCompletedChanged += ReplayGainCalculatorOnCalcCompletedChanged;
        StrongMessageBus.Instance.Subscribe<ExitReminderMessage>(ExitReminderMessageHandler);
    }

    private void ReplayGainCalculatorOnCalcCompletedChanged(object? sender, EventArgs e) => NumberOfCompletedCalc++;

    private void ExitReminderMessageHandler(ExitReminderMessage obj)
    {
        ReplayGainCalculator.CalcCompletedChanged -= ReplayGainCalculatorOnCalcCompletedChanged;
    }

    public SoundEffectConfig SoundEffectConfig { get; }

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    [ObservableProperty]
    public partial IAsyncTaskHandle? TaskHandle { get; set; }

    [ObservableProperty]
    public partial int NumberOfCompletedCalc { get; set; }

    public static MusicReplayGainStandard[] MusicReplayGainStandardList { get; set; } =
        EnumHelper<MusicReplayGainStandard>.ToArray();

    public float SpatialAngle
    {
        get => SoundEffectConfig.SpatialModifier.Angle;
        set
        {
            SoundEffectConfig.SpatialModifier.Angle = value;
            OnPropertyChanged();
        }
    }

    public float SpatialDistance
    {
        get => SoundEffectConfig.SpatialModifier.Distance;
        set
        {
            SoundEffectConfig.SpatialModifier.Distance = value;
            OnPropertyChanged();
        }
    }

    public List<EqualizerBand> ParametricEqualizerBands
    {
        get => SoundEffectConfig.ParametricEqualizer.Bands;
        set
        {
            SoundEffectConfig.ParametricEqualizer.Bands = value;
            OnPropertyChanged();
        }
    }

    public int NoiseReductionFftSize
    {
        get => (int)Math.Log(SoundEffectConfig.NoiseReductionModifier.FftSize, 2);
        set
        {
            SoundEffectConfig.NoiseReductionModifier.FftSize = (int)Math.Pow(2, value);
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private void OnSpeakerPositionChanged(PositionChangedEventArgs e)
    {
        SpatialAngle = (float)e.Angle;
        SpatialDistance = (float)e.Distance;
    }

    [RelayCommand]
    private void RestoreDefaultEqualizer()
    {
        var temp = new List<EqualizerBand>(ParametricEqualizerBands);
        foreach (var equalizerBand in temp)
        {
            equalizerBand.GainDb = 0f;
        }
        ParametricEqualizerBands = temp;
    }

    [ObservableProperty]
    public partial string CalculationButtonText { get; set; } = "开始计算 ▶";

    [RelayCommand]
    private Task ClearCallbackGain()
    {
        Task.Run(() =>
        {
            foreach (var musicItem in MusicPlayerViewModel.MusicItems)
            {
                if (musicItem.Gain < 0)
                    continue;
                musicItem.Gain = -1;
                NumberOfCompletedCalc--;
            }
        });

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ToggleCalculation()
    {
        if (MusicPlayerViewModel.MusicItems.Count <= 0)
            return;

        // 状态判断和操作一体化处理
        if (TaskHandle != null)
        {
            switch (TaskHandle.Status)
            {
                case AsyncTaskStatus.Faulted:
                    StartNewCalculation();
                    break;

                case AsyncTaskStatus.Running:
                    await TaskHandle.PauseAsync();
                    break;

                case AsyncTaskStatus.Paused:
                    TaskHandle.Resume();
                    break;
                case AsyncTaskStatus.Created:
                case AsyncTaskStatus.Completed:
                case AsyncTaskStatus.Cancelled:
                    break;
                default:
                    LoggerService.Error($"不存在的任务状态: {TaskHandle.Status}");
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
        CalculationButtonText = TaskHandle?.Status switch
        {
            AsyncTaskStatus.Running => "暂停计算 ♪",
            AsyncTaskStatus.Paused => "继续计算 ▶",
            _ => "开始计算 ▶",
        };
    }

    private void StartNewCalculation()
    {
        TaskHandle = AsyncTaskManager.CreateTask(
            async ct =>
            {
                // 不使用一次性的 Task.Run，而是逐个处理每个音乐项
                foreach (var item in MusicPlayerViewModel.MusicItems)
                {
                    // 在每个项目处理前检查取消请求
                    ct.ThrowIfCancellationRequested();

                    // 在每个项目处理前等待暂停状态解除
                    if (TaskHandle is AsyncTaskHandle handle)
                    {
                        await handle.WaitIfPausedAsync();
                    }

                    // 跳过已计算的项目
                    if (item.Gain > 0)
                        continue;

                    // 使用较小的任务单元，以便能够及时响应暂停
                    await Task.Run(() => AudioHelper.CalcGainOfMusicItem(item), ct);
                }
            },
            CleanupTask,
            ex =>
            {
                if (ex is OperationCanceledException)
                {
                    CleanupTask();
                }
            }
        );
    }

    private void CleanupTask()
    {
        TaskHandle?.Dispose();
        UpdatePromptText();
        TaskHandle = null;
    }

    [RelayCommand]
    private void CancelCalcCallbackGain()
    {
        if (TaskHandle is not { Status: < AsyncTaskStatus.Completed })
            return;

        TaskHandle.Cancel();
        CleanupTask();
    }

    private async Task RefreshNumberOfCompletedCalc()
    {
        if (MusicPlayerViewModel.MusicItems.Count <= 0)
            return;

        await Task.Run(() => NumberOfCompletedCalc = MusicPlayerViewModel.MusicItems.Count(x => x.Gain > 0));
    }
}
