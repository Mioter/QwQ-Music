using System.Collections.Frozen;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Audio.SoundModifier;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;
using QwQ_Music.Views.Dialogs;
using Ursa.Controls;

namespace QwQ_Music.ViewModels.Pages;

public partial class PlayConfigPageViewModel : ViewModelBase {
    public PlayConfigPageViewModel() { MusicItemsManager.All.MusicItemsChanged += UpdateGainCount; }

    private void UpdateGainCount(object? sender, MusicItemsChangedEventArgs args) {
        if (args.OldItems is not null) {
            NumberOfCompletedCalc -= args.OldItems.Count(item => item.Gain != 0);
        }

        if (args.NewItems is not null) {
            NumberOfCompletedCalc += args.NewItems.Count(item => item.Gain != 0);
        }

        OnPropertyChanged(nameof(NumberOfCompletedCalc));
    }

    public static FrozenDictionary<AddMusicBehavior, string> AddMusicBehaviors { get; } =
        new Dictionary<AddMusicBehavior, string>().ToFrozenDictionary();

    public PlayerConfig PlayerConfig { get; } = ConfigManager.PlayerConfig;

    public static MusicItemsManager MusicItemsManager => MusicItemsManager.All;

    public PlayComponent PlayComponent { get; } = ConfigManager.SoundModifierConfig.PlayComponent;

    public Dictionary<FadeModifier.FadeCurve, string> FadeCurves { get; } = new() {
        [FadeModifier.FadeCurve.Cosine] = "余弦渐变",
        [FadeModifier.FadeCurve.Exponential] = "指数渐变",
        [FadeModifier.FadeCurve.Linear] = "线性渐变"
    };

    [RelayCommand]
    public void OpenCurrentStreamInfo() {
        // ReSharper disable once UseConfigureAwaitFalse
        OverlayDialog.ShowCustomAsync<CurrentStreamInfo, ViewModelBase?, DialogResult>(
                         null,
                         options: new OverlayDialogOptions {
                             Title = "详细信息", CanLightDismiss = true, Mode = DialogMode.Info
                         })
                     .ContinueWith(LoggerService.HandleException)
                     .ConfigureAwait(false);
    }

    #region 回放增益

    [ObservableProperty]
    public partial int NumberOfCompletedCalc { get; set; } =
        MusicItemsManager.MusicItems.Values.Count(item => item.Gain != 0);

    public static Dictionary<MusicReplayGainStandard, string> MusicReplayGainStandards { get; set; } = new() {
        [MusicReplayGainStandard.Streaming] = "流媒体优化（-16 LUFS）",
        [MusicReplayGainStandard.EbuR128] = "EBU R128广播标准（-23 LUFS）",
        [MusicReplayGainStandard.ReplayGain2] = "ReplayGain 2.0标准（-18 LUFS）",
        [MusicReplayGainStandard.Custom] = "自定义目标响度"
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMusicReplayGainStandardDescription))]
    public partial MusicReplayGainStandard SelectedMusicReplayGainStandard { get; set; } =
        MusicReplayGainStandard.Streaming;

    public string SelectedMusicReplayGainStandardDescription =>
        MusicReplayGainStandards[SelectedMusicReplayGainStandard];

    [ObservableProperty]
    public partial CancellationTokenSource? CancellationTokenSource { get; set; }

    [RelayCommand]
    public void ForceRefreshMusicItemsTags() {
        Task.Run(() => MusicItemsManager.MusicItems.Values.AsParallel()
                                        .ForAll(item => item.UpdateMetaDataAsync(true).ConfigureAwait(false)))
            .ContinueWith(LoggerService.HandleException)
            .ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task ClearCallbackGain() {
        MessageBoxResult result = await MessageBox.ShowAsync(
                                                      "你真的要清空已经计算的回放增益值吗？",
                                                      "警告",
                                                      icon: MessageBoxIcon.Warning,
                                                      button: MessageBoxButton.YesNo)
                                                  .ConfigureAwait(false);

        if (result is not MessageBoxResult.Yes)
            return;

        await Task.Run(async Task? () => {
                      await foreach (MusicItemModel musicItem in MusicItemsManager.MusicItems.Values
                                         .Where(item => item.Gain > 0)
                                         .ToAsyncEnumerable()
                                         .ConfigureAwait(false)) {
                          musicItem.Gain = 0;
                          await MusicItemsManager.UpdateAsync(
                                                     musicItem,
                                                     new Dictionary<string, object?> {
                                                         [nameof(MusicItemModel.Gain)] = musicItem.Gain
                                                     })
                                                 .ConfigureAwait(false);
                      }
                  })
                  .ConfigureAwait(false);


        NumberOfCompletedCalc = 0;
        NotificationService.Info("回放增益值已清空！");
    }


    [RelayCommand]
    private async Task ToggleCalculation() {
        if (MusicItemsManager.Count <= 0 || NumberOfCompletedCalc == MusicItemsManager.Count) {
            NotificationService.Info("已经没有需要计算回放增益的音乐啦~");

            return;
        }

        await Task.Run(async Task? () => await StartNewCalculationAsync().ConfigureAwait(false)).ConfigureAwait(false);
    }

    private async Task StartNewCalculationAsync() {
        CancellationTokenSource = new CancellationTokenSource();
        await Task.Run(async Task? () => {
                      try {
                          IEnumerable<MusicItemModel> itemsToProcess =
                              MusicItemsManager.MusicItems.Values.Where(item => item.Gain <= 0);
                          ProcessItems(itemsToProcess, CancellationTokenSource.Token);
                          NotificationService.Info("回放增益计算结束！");
                      } catch (OperationCanceledException) {
                          NotificationService.Info("回放增益计算已取消！");
                      } catch (Exception e) {
                          NotificationService.Error($"计算任务出错退出！\n{e.Message}");
                          await LoggerService.ErrorAsync($"计算任务出错退出！\n{e.Message}\n{e.StackTrace}")
                                             .ConfigureAwait(false);
                      } finally {
                          CleanupTask();
                      }
                  })
                  .ConfigureAwait(false);
    }

    private void ProcessItems(IEnumerable<MusicItemModel> items, CancellationToken cancellationToken) {
        items.AsParallel()
             .WithCancellation(cancellationToken)
             .ForAll(item => {
                 if (cancellationToken.IsCancellationRequested)
                     return;

                 try {
                     using var audioGainCalculator = new AudioGainCalculator();
                     ProcessSingleItemAsync(audioGainCalculator, item).ConfigureAwait(false).GetAwaiter().GetResult();
                 } catch (Exception ex) {
                     NotificationService.Error($"计算{item.Title}的回放增益时出现错误：\n{ex.Message}");
                     LoggerService.Error($"计算{item.Title}的回放增益时出现错误。", ex);
                 }
             });
    }


    private async Task ProcessSingleItemAsync(AudioGainCalculator audioGainCalculator, MusicItemModel item) {
        double gain = await audioGainCalculator.CalculateGainAsync(
                                                   item,
                                                   SelectedMusicReplayGainStandard,
                                                   PlayerConfig.CustomMusicReplayGainStandard)
                                               .ConfigureAwait(false);

        await MusicItemsManager.UpdateAsync(
                                   item,
                                   new Dictionary<string, object?> { [nameof(MusicItemModel.Gain)] = gain })
                               .ConfigureAwait(false);

        item.Gain = gain;
        NumberOfCompletedCalc++;
    }

    [RelayCommand]
    public void CancelCalculation() { CancellationTokenSource?.Cancel(); }

    private void CleanupTask() {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = null;
    }

    #endregion

    ~PlayConfigPageViewModel() {
        MusicItemsManager.All.MusicItemsChanged -= UpdateGainCount;
        CleanupTask();
    }
}