using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace QwQ_Music.UserControls;

public partial class TitleBarRightContent : UserControl
{
    public TitleBarRightContent()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/Mioter/QwQ-Music")
        {
            UseShellExecute = true,
        });
    }
}
