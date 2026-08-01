using Avalonia;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public partial class DesktopFunctionsConfigPageViewModel : ViewModelBase {
    public DesktopControlConfig DesktopControlConfig => ConfigManager.DesktopControlConfig;

    public DesktopFunctionsConfigPageViewModel() {
        ToggleWindowDisplayStatus(LyricIsEnabled);
        ToggleDesktopPlayControlService(DesktopPlayControlIsEnabled);
        OnPropertyChanged(nameof(LyricConfig.DesktopLyric.Width));

        AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
    }

    public bool LyricIsEnabled {
        get => LyricConfig.DesktopLyric.IsEnabled;
        set {
            if (LyricIsEnabled == value)
                return;

            LyricConfig.DesktopLyric.IsEnabled = value;
            OnPropertyChanged();

            ToggleWindowDisplayStatus(value);
        }
    }


    public bool DesktopPlayControlIsEnabled {
        get => DesktopControlConfig.IsEnabled;
        set {
            if (DesktopPlayControlIsEnabled == value)
                return;

            DesktopControlConfig.IsEnabled = value;
            OnPropertyChanged();

            ToggleDesktopPlayControlService(value);
        }
    }

    public bool LockLyricWindow {
        get => LyricConfig.DesktopLyric.IsAnchored;
        set {
            if (LockLyricWindow == value)
                return;

            LyricConfig.DesktopLyric.IsAnchored = value;
            DesktopLyricsService.DesktopLyricsWindow?.SetPenetrate(value);
            OnPropertyChanged();
        }
    }

    public bool LyricIsDoubleLine {
        get => LyricConfig.DesktopLyric.IsDoubleLine;
        set {
            if (LyricIsDoubleLine == value)
                return;

            LyricConfig.DesktopLyric.IsDoubleLine = value;
            if (!value) {
                LyricConfig.DesktopLyric.IsKtvMode = false;
            }
            DesktopLyricsService.DesktopLyricsWindow?.UpdateFades();
            OnPropertyChanged();
        }
    }

    public int CrossFadeTime {
        get => (int)LyricConfig.DesktopLyric.CrossFadeTime.TotalMilliseconds;
        set {
            TimeSpan time = TimeSpan.FromMilliseconds(value);
            LyricConfig.DesktopLyric.CrossFadeTime = time;
            DesktopLyricsService.DesktopLyricsWindow?.UpdateFades();
            OnPropertyChanged();
        }
    }

    public int SlideTime {
        get => (int)LyricConfig.DesktopLyric.SlideTime.TotalMilliseconds;
        set {
            TimeSpan time = TimeSpan.FromMilliseconds(value);
            LyricConfig.DesktopLyric.SlideTime = time;
            OnPropertyChanged();
        }
    }


    public int FadeTime {
        get => (int)LyricConfig.DesktopLyric.FadeTime.TotalMilliseconds;
        set {
            TimeSpan time = TimeSpan.FromMilliseconds(value);
            LyricConfig.DesktopLyric.FadeTime = time;
            OnPropertyChanged();
        }
    }

    public bool IsPrimaryBold {
        get => LyricConfig.DesktopLyric.IsEnabled;
        set {
            if (LyricIsEnabled == value)
                return;

            LyricConfig.DesktopLyric.IsEnabled = value;
            OnPropertyChanged();

            ToggleWindowDisplayStatus(value);
        }
    }

    public static LyricConfig LyricConfig => ConfigManager.LyricConfig;

    private void CurrentDomainOnProcessExit(object? sender, EventArgs e) {
        AppDomain.CurrentDomain.ProcessExit -= CurrentDomainOnProcessExit;
        Dispatcher.UIThread.Post(() => {
            CloseLyricWindow();
            DesktopPlayControlService.Stop();
        });
    }

    private static void ToggleDesktopPlayControlService(bool value) {
        if (value)
            // 启动桌面播放控制服务
            DesktopPlayControlService.Start();
        else
            DesktopPlayControlService.Stop();
    }

    private void ToggleWindowDisplayStatus(bool value) {
        if (value)
            ShowLyricWindow();
        else
            CloseLyricWindow();
    }

    private void ShowLyricWindow() {
        if (!LyricConfig.DesktopLyric.IsEnabled)
            return;
        DesktopLyricsService.Create();
        DesktopLyricsService.DesktopLyricsWindow?.Show();
    }

    private void CloseLyricWindow() { DesktopLyricsService.Close(); }

    [RelayCommand]
    private void SetWindowPosition(string position) {
        if (DesktopLyricsService.DesktopLyricsWindow == null) {
            NotificationService.Error("请先启动歌词窗口~");

            return;
        }

        Screen? screen =
            DesktopLyricsService.DesktopLyricsWindow.Screens.ScreenFromWindow(DesktopLyricsService.DesktopLyricsWindow);
        if (screen == null) {
            NotificationService.Error("无法获取屏幕宽高~");

            return;
        }

        int screenWidth = screen.WorkingArea.Width;
        int screenHeight = screen.WorkingArea.Height;
        double scaling = screen.Scaling;
        double windowWidth = DesktopLyricsService.DesktopLyricsWindow.Width * scaling;
        double windowHeight = DesktopLyricsService.DesktopLyricsWindow.Height * scaling;

        var positions = new Dictionary<string, Func<PixelPoint>> {
            ["↖"] = () => new PixelPoint(0, 0),

            // ReSharper disable once PossibleLossOfFraction
            ["↑"] = () => new PixelPoint((int)(screenWidth / 2 - windowWidth / 2), 0),
            ["↗"] = () => new PixelPoint((int)(screenWidth - windowWidth), 0),
            ["↙"] = () => new PixelPoint(0, (int)(screenHeight - windowHeight)),

            // ReSharper disable once PossibleLossOfFraction
            ["↓"] = () => new PixelPoint((int)(screenWidth / 2 - windowWidth / 2), (int)(screenHeight - windowHeight)),
            ["↘"] = () => new PixelPoint((int)(screenWidth - windowWidth), (int)(screenHeight - windowHeight))
        };

        if (positions.TryGetValue(position, out Func<PixelPoint>? getPosition))
            DesktopLyricsService.DesktopLyricsWindow.Position = getPosition();

        // 如果不是已知位置，则保持原位置
    }

    #region 多语言

    public static string IsEnabledV => I18NService.Lang.Translation[nameof(IsEnabledV)];

    public static string IsAutoFadeV => I18NService.Lang.Translation[nameof(IsAutoFadeV)];

    public static string IsDoubleLineV => I18NService.Lang.Translation[nameof(IsDoubleLineV)];

    // ReSharper disable once InconsistentNaming
    public static string IsKTVModeV => I18NService.Lang.Translation[nameof(IsKTVModeV)];

    public static string IsDualLangV => I18NService.Lang.Translation[nameof(IsDualLangV)];

    public static string IsBoldV => I18NService.Lang.Translation[nameof(IsBoldV)];

    // ReSharper disable once InconsistentNaming
    public static string PositionXV => I18NService.Lang.Translation[nameof(PositionXV)];

    // ReSharper disable once InconsistentNaming
    public static string PositionYV => I18NService.Lang.Translation[nameof(PositionYV)];

    public static string WidthV => I18NService.Lang.Translation[nameof(WidthV)];

    public static string HeightV => I18NService.Lang.Translation[nameof(HeightV)];

    public static string LyricMainTopColorV => I18NService.Lang.Translation[nameof(LyricMainTopColorV)];

    public static string LyricMainBottomColorV => I18NService.Lang.Translation[nameof(LyricMainBottomColorV)];

    public static string LyricMainBorderColorV => I18NService.Lang.Translation[nameof(LyricMainBorderColorV)];

    public static string LyricAltTopColorV => I18NService.Lang.Translation[nameof(LyricAltTopColorV)];

    public static string LyricAltBottomColorV => I18NService.Lang.Translation[nameof(LyricAltBottomColorV)];

    public static string LyricAltBorderColorV => I18NService.Lang.Translation[nameof(LyricAltBorderColorV)];

    #endregion
}