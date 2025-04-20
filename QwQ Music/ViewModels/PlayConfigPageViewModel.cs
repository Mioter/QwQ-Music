using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

public partial class PlayConfigPageViewModel : ViewModelBase
{
    public PlayerConfig PlayerConfig { get; } = ConfigInfoModel.PlayerConfig;

    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    public AudioModifierConfig AudioModifierConfig { get; } = ConfigInfoModel.AudioModifierConfig;

    public PlayConfigPageViewModel()
    {
        ReplayGainCalculator.CalcCompletedChanged += ReplayGainCalculatorOnCalcCompletedChanged;
        StrongMessageBus.Instance.Subscribe<ExitReminderMessage>(ExitReminderMessageHandler);
        StrongMessageBus.Instance.Subscribe<LoadCompletedMessage>(LoadCompletedMessageHandler);
    }

    private async void LoadCompletedMessageHandler(LoadCompletedMessage obj)
    {
        if (obj.Name == nameof(MusicPlayerViewModel.MusicItems))
        {
            await RefreshNumberOfCompletedCalc();
        }
    }

    private void ReplayGainCalculatorOnCalcCompletedChanged(object? sender, EventArgs e) => NumberOfCompletedCalc++;

    private void ExitReminderMessageHandler(ExitReminderMessage obj)
    {
        ReplayGainCalculator.CalcCompletedChanged -= ReplayGainCalculatorOnCalcCompletedChanged;
    }

    public FadeModifier.FadeCurve[] FadeCurves { get; } = EnumHelper<FadeModifier.FadeCurve>.ToArray();

    #region 回放增益

    [ObservableProperty]
    public partial IAsyncTaskHandle? TaskHandle { get; set; }

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
                    break;
                case AsyncTaskStatus.Cancelled:
                default:
                    await LoggerService.ErrorAsync($"不存在的任务状态: {TaskHandle.Status}");
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
            AsyncTaskStatus.Running => "暂停 \u23f8",
            AsyncTaskStatus.Paused => "继续 ▶",
            _ => "开始 ▶",
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
                    await Task.Run(
                        async () =>
                        {
                            var ex = await item.GetExtensionsInfo();
                            item.Gain = AudioHelper.CalcGainOfMusicItem(item.FilePath, ex.SamplingRate, ex.Channels);
                        },
                        ct
                    );
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
        if (TaskHandle is null)
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
