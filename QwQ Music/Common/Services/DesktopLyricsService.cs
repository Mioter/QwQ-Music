using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using QwQ_Music.Common.Managers;
using QwQ_Music.ViewModels.Windows;
using QwQ_Music.Windows;
using Timer = System.Timers.Timer;

namespace QwQ_Music.Common.Services;

public static class DesktopLyricsService {
    private enum FadeStatus {
        NotFading, FadingIn, FadingOut
    }

    public static DesktopLyricsWindow? DesktopLyricsWindow { get; private set; }

    private static volatile FadeStatus _fadeStatus = FadeStatus.NotFading;

    private static Timer? _timer;

    public static void Create() {
        if (ConfigManager.LyricConfig.DesktopLyric.IsEnabled) {
            DesktopLyricsWindow ??= new DesktopLyricsWindow {
                DataContext = new DesktopLyricsWindowViewModel(), Width = ConfigManager.LyricConfig.DesktopLyric.Width
            };
        }
    }

    public static void Close() {
        DesktopLyricsWindow?.Close();
        DesktopLyricsWindow = null;
    }

    public static void TryFadeIn() {
        if (DesktopLyricsWindow is null ||
            Interlocked.Exchange(ref _fadeStatus, FadeStatus.FadingIn) == FadeStatus.FadingIn)
            return; // fastfail
        LoggerService.Info("桌面歌词正在淡入");
        Dispatcher.UIThread.Post(() => {
            DesktopLyricsWindow.Show();
            double time = (1 - DesktopLyricsWindow.Opacity) * ConfigManager.LyricConfig.DesktopLyric.FadeInMilliseconds;
            if (time > 0) {
                Fade(TimeSpan.FromMilliseconds(time), TimeSpan.Zero, 1f);
            } else {
                if (_transition is not null)
                    DesktopLyricsWindow.Transitions?.Remove(_transition);
                DesktopLyricsWindow.Opacity = 1;
            }
        });
    }

    public static void TryFadeOut() {
        if (DesktopLyricsWindow is null ||
            Interlocked.Exchange(ref _fadeStatus, FadeStatus.FadingOut) == FadeStatus.FadingOut)
            return; // fastfail
        LoggerService.Info("桌面歌词正在淡出");
        double time = DesktopLyricsWindow.Opacity * ConfigManager.LyricConfig.DesktopLyric.FadeOutMilliseconds;
        Dispatcher.UIThread.Post(() => {
            if (time > 0.01) {
                Fade(
                    TimeSpan.FromMilliseconds(time),
                    TimeSpan.FromMilliseconds(ConfigManager.LyricConfig.DesktopLyric.FadeOutDelayMilliseconds),
                    ConfigManager.LyricConfig.DesktopLyric.MinimumOpacity / 100f);
            } else {
                if (_transition is not null)
                    DesktopLyricsWindow.Transitions?.Remove(_transition);
                DesktopLyricsWindow.Opacity = ConfigManager.LyricConfig.DesktopLyric.MinimumOpacity;
                if (ConfigManager.LyricConfig.DesktopLyric.MinimumOpacity < 0.01)
                    DesktopLyricsWindow.Hide();
            }
        });
        if (time < 0.01)
            return;
        _timer?.Dispose();
        _timer = new Timer(time+ConfigManager.LyricConfig.DesktopLyric.FadeOutDelayMilliseconds);
        _timer.Elapsed += (_, _) => {
            if (ConfigManager.LyricConfig.DesktopLyric.MinimumOpacity < 0.01)
                Dispatcher.UIThread.Post(DesktopLyricsWindow.Hide);
            _timer?.Dispose();
            _timer = null;
        };
        _timer.Start();
    }

    private static ITransition? _transition;

    /// <summary>
    /// 执行透明度动画（从当前值过渡到目标值）
    /// </summary>
    /// <param name="duration">动画持续时间</param>
    /// <param name="delay">延迟启动时间</param>
    /// <param name="endValue">目标透明度（0~1）</param>
    public static void Fade(TimeSpan duration, TimeSpan delay, double endValue) {
        if (DesktopLyricsWindow is null)
            return; // fastfail
        DesktopLyricsWindow.Transitions ??= [];
        double op = DesktopLyricsWindow.Opacity;
        if (_transition is not null)
            DesktopLyricsWindow.Transitions.Remove(_transition);
        DesktopLyricsWindow.Opacity = op;
        ITransition transition = _transition =
            new DoubleTransition { Duration = duration, Delay = delay, Property = Visual.OpacityProperty };
        DesktopLyricsWindow.Transitions.Add(transition);
        DesktopLyricsWindow.Opacity = endValue;
    }
}