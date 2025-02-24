using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using QwQ_Music.ViewModels;
using Ursa.Controls;
namespace QwQ_Music.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();
    private readonly Window _desktopLyricsWindow;
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _desktopLyricsWindow = new DesktopLyricsWindow();
        PointerWheelChanged += OnPointerWheelChanged;
    }
    
    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _viewModel.IsMusicPlayerTrayVisible = e.Delta.Y switch
        {
            // 检查滚动的方向
            > 0 =>
                // 向上滚动
                true,
            < 0 =>
                // 向下滚动
                false,
            _ => _viewModel.IsMusicPlayerTrayVisible,
        };

        /*
        // 如果支持水平滚动，则可以检查Delta.X
        if (e.Delta.X != 0)
        {
            Console.WriteLine($"Mouse wheel scrolled horizontally by {e.Delta.X}.");
        }
        */

    }
}
