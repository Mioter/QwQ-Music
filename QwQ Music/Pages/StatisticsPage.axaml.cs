using Avalonia.Controls;
using Avalonia.Interactivity;
using QwQ_Music.Amusing;

namespace QwQ_Music.Pages;

public partial class StatisticsPage : UserControl
{
    public StatisticsPage()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = new Love().GenerateHeart();
    }
}
