using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Helper;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Services;
using QwQ_Music.Services.Audio.Play;
using QwQ_Music.Utilities;

namespace QwQ_Music.ViewModels;

public partial class SoundEffectConfigViewModel : NavigationViewModel
{
    public SoundEffectConfigViewModel()
        : base("音效")
    {
        ExitReminderService.ExitReminder += ExitReminderServiceOnExitReminder;
        MusicPlayerViewModel.MusicItemsChanged += MusicPlayerViewModelOnMusicItemsChanged;

        SoundEffectConfig = ConfigInfoModel.SoundEffectConfig;
        SoundEffectConfig.Initialization(MusicPlayerViewModel.AudioPlay);
    }

    private void ExitReminderServiceOnExitReminder(object? sender, EventArgs e)
    {
        ExitReminderService.ExitReminder -= ExitReminderServiceOnExitReminder;
        MusicPlayerViewModel.MusicItemsChanged -= MusicPlayerViewModelOnMusicItemsChanged;
    }

    private void MusicPlayerViewModelOnMusicItemsChanged(object? sender, ObservableCollection<MusicItemModel> e)
    {
        RefreshNumberOfCompletedCalc().ConfigureAwait(false);
    }

    public SoundEffectConfig SoundEffectConfig { get; }

    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    [ObservableProperty]
    private AsyncTaskHandle? _taskHandle;

    private int _numberOfCompletedCalc;
    public int NumberOfCompletedCalc
    {
        get => _numberOfCompletedCalc;
        set => SetProperty(ref _numberOfCompletedCalc, value);
    }

    [ObservableProperty]
    private List<MusicReplayGainStandard> _musicReplayGainStandardList = EnumHelper<MusicReplayGainStandard>.ToList();

    [ObservableProperty]
    private string _calculationButtonText = "开始计算 ▶";

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
                // 使用线程安全计数器
                Interlocked.Decrement(ref _numberOfCompletedCalc);
                OnPropertyChanged(nameof(NumberOfCompletedCalc));
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

                            item.Gain = ReplayGainCalculator.CalculateGain(
                                item.FilePath,
                                SoundEffectConfig.SelectedMusicReplayGainStandard
                            );

                            // 使用线程安全计数器
                            Interlocked.Increment(ref _numberOfCompletedCalc);
                            OnPropertyChanged(nameof(NumberOfCompletedCalc));
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

    public async Task RefreshNumberOfCompletedCalc()
    {
        if (MusicPlayerViewModel.MusicItems.Count <= 0)
            return;

        await Task.Run(() => NumberOfCompletedCalc = MusicPlayerViewModel.MusicItems.Count(x => x.Gain > 0));
    }
}
