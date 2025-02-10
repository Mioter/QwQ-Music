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
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        PointerWheelChanged += OnPointerWheelChanged;
        Closed += OnClosed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        PointerWheelChanged -= OnPointerWheelChanged;
        _viewModel.ManualCleaning();
        Closed -= OnClosed;
    }


    private void SelectionList_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is SelectionList selectionList)
        {
            selectionList.SelectedIndex = 0;
        }
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
