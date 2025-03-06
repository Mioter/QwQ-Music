using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using static QwQ_Music.Models.ConfigInfoModel;
using static QwQ_Music.Models.LanguageModel;
using static QwQ_Music.Services.MousePenetrateService;

namespace QwQ_Music.Views;

public partial class DesktopLyricsWindow : Window
{
    public DesktopLyricsWindow()
    {
        if (!DesktopLyricConfig.LyricIsEnabled)
        {
            return;
        }

        InitializeComponent();

        if (DesktopLyricConfig.LyricIsVertical)
        {
            MainLyric.RenderTransform = new RotateTransform(90);
            AltLyric.RenderTransform = new RotateTransform(90);
        }

        MainLyric.Text = Lang["Loading..."];
        if (DesktopLyricConfig is { LyricIsDoubleLine: false, LyricIsDualLang: false })
        {
            AltLyric.IsVisible = false;
            Grid.SetRowSpan(MainLyric, 2);
        }
        else
            AltLyric.Text = Lang["Loading..."];

        Width = DesktopLyricConfig.Size.Width;
        Height = DesktopLyricConfig.Size.Height;
        Position = new PixelPoint(DesktopLyricConfig.LyricPositionX, DesktopLyricConfig.LyricPositionY);
        base.Show();
        SetPenetrate(TryGetPlatformHandle()!.Handle);
    }
}
