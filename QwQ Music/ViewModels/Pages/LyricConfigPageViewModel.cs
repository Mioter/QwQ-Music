using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views;
using QwQ.Avalonia.Utilities.MessageBus;
using static QwQ_Music.Models.LanguageModel;

namespace QwQ_Music.ViewModels.Pages;

public partial class LyricConfigPageViewModel : ViewModelBase
{
    public static string IsEnabledName => Lang[nameof(IsEnabledName)];
    public static string IsDoubleLineName => Lang[nameof(IsDoubleLineName)];
    public static string IsDualLangName => Lang[nameof(IsDualLangName)];
    public static string PositionXName => Lang[nameof(PositionXName)];
    public static string PositionYName => Lang[nameof(PositionYName)];
    public static string WidthName => Lang[nameof(WidthName)];
    public static string HeightName => Lang[nameof(HeightName)];
    public static string LyricMainTopColorName => Lang[nameof(LyricMainTopColorName)];
    public static string LyricMainBottomColorName => Lang[nameof(LyricMainBottomColorName)];
    public static string LyricMainBorderColorName => Lang[nameof(LyricMainBorderColorName)];
    public static string LyricAltTopColorName => Lang[nameof(LyricAltTopColorName)];
    public static string LyricAltBottomColorName => Lang[nameof(LyricAltBottomColorName)];
    public static string LyricAltBorderColorName => Lang[nameof(LyricAltBorderColorName)];

    private DesktopLyricsWindow? _desktopLyricsWindow;
    private DesktopLyricsWindowViewModel? _desktopLyricsWindowViewModel;

    public LyricConfigPageViewModel()
    {
        MessageBus
            .ReceiveMessage<ExitReminderMessage>(this)
            .WithHandler((_, _) => Dispatcher.UIThread.Post(HideLyricWindow))
            .AsWeakReference()
            .Subscribe();

        ToggleWindowDisplayStatus(LyricIsEnabled);
        OnPropertyChanged(nameof(LyricWidth));
        SetLyricsWindowWidth(LyricConfig.DesktopLyric.LyricIsDoubleLine);
    }

    public bool LyricIsEnabled
    {
        get => LyricConfig.DesktopLyric.LyricIsEnabled;
        set
        {
            if (LyricIsEnabled == value)
                return;

            LyricConfig.DesktopLyric.LyricIsEnabled = value;

            ToggleWindowDisplayStatus(value);
        }
    }

    private void ToggleWindowDisplayStatus(bool value)
    {
        if (value)
        {
            ShowLyricWindow();
        }
        else
        {
            HideLyricWindow();
        }
    }

    public bool LockLyricWindow
    {
        get => LyricConfig.DesktopLyric.LockLyricWindow;
        set
        {
            if (LockLyricWindow == value)
                return;

            LyricConfig.DesktopLyric.LockLyricWindow = value;
            _desktopLyricsWindow?.SetPenetrate(value);
        }
    }

    public double LyricWidth
    {
        get => LyricConfig.DesktopLyric.LyricWidth;
        set
        {
            LyricConfig.DesktopLyric.LyricWidth = value;

            SetLyricsWindowWidth(LyricConfig.DesktopLyric.LyricIsDoubleLine);
        }
    }

    private static double LyricWidowWidth =>
        LyricConfig.DesktopLyric.LyricWidth + LyricConfig.DesktopLyric.LyricSpacing * 2;

    public bool LyricIsDoubleLine
    {
        get => LyricConfig.DesktopLyric.LyricIsDoubleLine;
        set
        {
            if (LyricIsDoubleLine == value)
                return;

            LyricConfig.DesktopLyric.LyricIsDoubleLine = value;

            SetLyricsWindowWidth(value);
        }
    }

    private void SetLyricsWindowWidth(bool value)
    {
        if (_desktopLyricsWindow == null)
            return;

        // 获取屏幕缩放
        double scaling = _desktopLyricsWindow.Screens.Primary?.Scaling ?? 1.0;

        // 计算当前窗口中心位置（使用浮点数避免精度丢失）
        var currentCenter = new Point(
            _desktopLyricsWindow.Position.X + _desktopLyricsWindow.Width * scaling / 2,
            _desktopLyricsWindow.Position.Y + _desktopLyricsWindow.Height * scaling / 2
        );

        // 调整窗口宽度
        double newWidth = value ? LyricWidowWidth : LyricWidowWidth * 2;
        _desktopLyricsWindow.Width = newWidth;

        // 重新计算位置，保持中心点不变（考虑缩放）
        var newPosition = new PixelPoint(
            (int)(currentCenter.X - newWidth * scaling / 2),
            (int)(currentCenter.Y - _desktopLyricsWindow.Height * scaling / 2)
        );
        _desktopLyricsWindow.Position = newPosition;
    }

    public static LyricConfig LyricConfig { get; } = ConfigManager.LyricConfig;

    private void ShowLyricWindow()
    {
        _desktopLyricsWindow = new DesktopLyricsWindow
        {
            DataContext = _desktopLyricsWindowViewModel = new DesktopLyricsWindowViewModel(),
            Width = LyricWidowWidth,
        };
        _desktopLyricsWindow.Show();
    }

    private void HideLyricWindow()
    {
        _desktopLyricsWindow?.Close();
        _desktopLyricsWindow = null;
    }

    [RelayCommand]
    private void SetWindowPosition(string position)
    {
        if (_desktopLyricsWindow == null || _desktopLyricsWindow.Screens.Primary == null)
        {
            NotificationService.ShowLight("错误", "无法获取屏幕宽高~", NotificationType.Error);
            return;
        }

        int screenWidth = _desktopLyricsWindow.Screens.Primary.WorkingArea.Width;
        int screenHeight = _desktopLyricsWindow.Screens.Primary.WorkingArea.Height;
        double scaling = _desktopLyricsWindow.Screens.Primary.Scaling;
        double windowWidth = _desktopLyricsWindow.Width * scaling;
        double windowHeight = _desktopLyricsWindow.Height * scaling;

        var positions = new Dictionary<string, Func<PixelPoint>>
        {
            ["↖"] = () => new PixelPoint(0, 0),
            // ReSharper disable once PossibleLossOfFraction
            ["↑"] = () => new PixelPoint((int)(screenWidth / 2 - windowWidth / 2), 0),
            ["↗"] = () => new PixelPoint((int)(screenWidth - windowWidth), 0),
            ["↙"] = () => new PixelPoint(0, (int)(screenHeight - windowHeight)),
            // ReSharper disable once PossibleLossOfFraction
            ["↓"] = () => new PixelPoint((int)(screenWidth / 2 - windowWidth / 2), (int)(screenHeight - windowHeight)),
            ["↘"] = () => new PixelPoint((int)(screenWidth - windowWidth), (int)(screenHeight - windowHeight)),
        };

        if (positions.TryGetValue(position, out var getPosition))
        {
            _desktopLyricsWindow.Position = getPosition();
        }
        // 如果不是已知位置，则保持原位置
    }
}
