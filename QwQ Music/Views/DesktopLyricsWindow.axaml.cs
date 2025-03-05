using Avalonia.Controls;
using Avalonia.Media;
using QwQ_Music.Models;
using static QwQ_Music.Models.LanguageModel;
using static QwQ_Music.Services.MousePenetrateService;

namespace QwQ_Music.Views;

public partial class DesktopLyricsWindow : Window
{
    public DesktopLyricsWindow()
    {
        if (!DesktopLyricConfig.IsEnabled)
        {
            return;
        }

        InitializeComponent();
        if (DesktopLyricConfig.IsVertical)
        {
            MainLyric.RenderTransform = new RotateTransform(90);
            AltLyric.RenderTransform = new RotateTransform(90);
        }

        MainLyric.Text = Lang["Loading..."];
        if (!DesktopLyricConfig.IsDoubleLine && !DesktopLyricConfig.IsDualLang)
        {
            AltLyric.IsVisible = false;
            Grid.SetRowSpan(MainLyric, 2);
        }
        else
            AltLyric.Text = Lang["Loading..."];

        Width = DesktopLyricConfig.Size.Width;
        Height = DesktopLyricConfig.Size.Height;
        Position = DesktopLyricConfig.Position;
        base.Show();
        SetPenetrate(TryGetPlatformHandle()!.Handle);
    }
}
