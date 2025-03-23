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
using QwQ_Music.Services.Audio.Play;
using QwQ_Music.Utilities;
using QwQ_Music.Utilities.MessageBus;
using SoundFlow.Modifiers;

namespace QwQ_Music.ViewModels;

public partial class SoundEffectConfigViewModel : NavigationViewModel
{
    public SoundEffectConfigViewModel()
        : base("音效")
    {
        SoundEffectConfig = ConfigInfoModel.SoundEffectConfig;
        RefreshNumberOfCompletedCalc().ConfigureAwait(false);
        
        ReplayGainCalculator.CalcCompletedChanged += ReplayGainCalculatorOnCalcCompletedChanged;
        StrongMessageBus.Instance.Subscribe<ExitReminderMessage>(ExitReminderMessageChanged);
    }

    private void ReplayGainCalculatorOnCalcCompletedChanged(object? sender, EventArgs e) => NumberOfCompletedCalc++;

    private void ExitReminderMessageChanged(ExitReminderMessage obj)
    {
        ReplayGainCalculator.CalcCompletedChanged -= ReplayGainCalculatorOnCalcCompletedChanged;
    }

    public SoundEffectConfig SoundEffectConfig { get; }

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    [ObservableProperty] public partial AsyncTaskHandle? TaskHandle { get; set; }

    [ObservableProperty] public partial int NumberOfCompletedCalc { get; set; }

    public static MusicReplayGainStandard[] MusicReplayGainStandardList { get; set; } = EnumHelper<MusicReplayGainStandard>.ToArray();

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
                NumberOfCompletedCalc++;
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
        switch (TaskHandle?.Status)
        {
            case AsyncTaskStatus.Created:
                break;
            case null:
            case AsyncTaskStatus.Cancelled:
            case AsyncTaskStatus.Completed:
            case AsyncTaskStatus.Faulted:
                StartNewCalculation();
                break;

            case AsyncTaskStatus.Running:
                await TaskHandle.PauseAsync();
                break;

            case AsyncTaskStatus.Paused:
                TaskHandle.Resume();
                break;
            default:
                LoggerService.Error($"不存在的任务状态: {TaskHandle.Status}");
                break;
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
                await Task.Run(
                    () =>
                    {
                        foreach (var item in MusicPlayerViewModel.MusicItems)
                        {
                            // 增加暂停检查点
                            if (TaskHandle?.Status == AsyncTaskStatus.Paused)
                                TaskHandle.WaitIfPausedAsync().Wait(ct);

                            ct.ThrowIfCancellationRequested();

                            if (item.Gain > 0)
                                continue;

                            AudioHelper.CalcGainOfMusicItem(item);
                        }
                    },
                    ct
                );
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
