using Avalonia.Controls;
using Avalonia.Interactivity;
using QwQ_Music.Amusing;
using QwQ_Music.ViewModels;
using QwQ_Music.ViewModels.Pages;

namespace QwQ_Music.Views.Pages;

public partial class StatisticsPage : UserControl
{
    public StatisticsPage()
    {
        InitializeComponent();
        DataContext = new StatisticsPageViewModel();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        _ = new Love().GenerateHeart();
    }
}
