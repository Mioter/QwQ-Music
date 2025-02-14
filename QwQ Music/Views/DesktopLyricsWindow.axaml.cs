using Avalonia.Controls;
using Avalonia.Media;
using Config = QwQ_Music.Models.DesktopLyricConfig;
using static QwQ_Music.Services.MousePenetrateService;
using static QwQ_Music.Models.LanguageModel;

namespace QwQ_Music.Views;

public partial class DesktopLyricsWindow : Window {
    public DesktopLyricsWindow() {
        if (!Config.IsEnabled) { return; }

        InitializeComponent();
        if (Config.IsVertical) {
            MainLyric.RenderTransform = new RotateTransform(90);
            AltLyric.RenderTransform = new RotateTransform(90);
        }

        MainLyric.Text = Lang["Waiting For Music..."];
        if (!Config.IsDoubleLine && !Config.IsDualLang) {
            AltLyric.IsVisible = false;
            Grid.SetRowSpan(MainLyric, 2);
        } else
            AltLyric.Text = Lang["Waiting For Music..."];

        Width = Config.Size.Width;
        Height = Config.Size.Height;
        Position = Config.Position; Show();
        SetPenetrate(TryGetPlatformHandle().Handle);
    }
}