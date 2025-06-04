using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using QwQ_Music.Amusing;

namespace QwQ_Music.Views.Pages;

public partial class StatisticsPage : UserControl
{
    public StatisticsPage()
    {
        InitializeComponent();
    }

    private void ClickMeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = new Love().GenerateHeart();
    }

    private void LagButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Thread.Sleep(5000);
    }
}
