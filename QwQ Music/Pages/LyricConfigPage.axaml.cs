using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Config = QwQ_Music.Models.DesktopLyricConfig;

namespace QwQ_Music.Pages;

public partial class LyricConfigPage : UserControl {
    public LyricConfigPage() { InitializeComponent(); }

    private void MaximizeLyricWidth(object sender, RoutedEventArgs e) {
        var Screen = TopLevel.GetTopLevel(this).Screens.Primary;
        Config.Size = new(Screen.WorkingArea.Width / Screen.Scaling, Config.Size.Height);
    }

    private void MaximizeLyricHeight(object? sender, RoutedEventArgs e) {
        var Screen = TopLevel.GetTopLevel(this).Screens.Primary;
        Config.Size = new(Config.Size.Width, Screen.WorkingArea.Height / Screen.Scaling);
    }

    private void ResetLyricHeight(object? sender, RoutedEventArgs e) { }
    private void SetLyricBackground(object? sender, PointerEventArgs e) {
        Config.BackgroundColor = Color.Parse("#80000000");
    }

    private void UnsetLyricBackground(object? sender, PointerEventArgs e) {
        Config.BackgroundColor = Colors.Transparent;
    }
}