using Avalonia.Threading;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views;
using QwQ.Avalonia.Utilities.MessageBus;
using static QwQ_Music.Models.LanguageModel;

namespace QwQ_Music.ViewModels.Pages;

public class LyricConfigPageViewModel : ViewModelBase
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

    public static LyricConfig LyricConfig { get; } = ConfigInfoModel.LyricConfig;

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
}
