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

    public static LyricConfig LyricConfig { get; } = ConfigManager.LyricConfig;

    private void ShowLyricWindow()
    {
        _desktopLyricsWindow = new DesktopLyricsWindow
        {
            DataContext = _desktopLyricsWindowViewModel = new DesktopLyricsWindowViewModel(),
        };
        _desktopLyricsWindow.Show();
    }

    private void HideLyricWindow()
    {
        _desktopLyricsWindow?.Close();
        _desktopLyricsWindowViewModel?.Unsubscribe();
        _desktopLyricsWindow = null;
        _desktopLyricsWindowViewModel = null;
    }

    [RelayCommand]
    private void SetWindowPosition(string position)
    {
        if (_desktopLyricsWindow == null || _desktopLyricsWindow.Screens.Primary == null)
        {
            NotificationService.ShowLight(
                new Notification("错误", "无法获取屏幕宽高~"),
                NotificationType.Error
            );
            return;
        }
    
        int screenWidth = _desktopLyricsWindow.Screens.Primary.Bounds.Width;
        int screenHeight = _desktopLyricsWindow.Screens.Primary.Bounds.Height;
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
