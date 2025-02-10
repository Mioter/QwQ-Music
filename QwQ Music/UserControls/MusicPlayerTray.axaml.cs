using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using QwQ_Music.ViewModels;

namespace QwQ_Music.UserControls;

public partial class MusicPlayerTray : UserControl
{
    private readonly MusicPlayerTrayViewModel _viewModel = new();

    public MusicPlayerTray()
    {
        InitializeComponent();
        DataContext = _viewModel;

        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Unloaded -= OnUnloaded;

        if (_viewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
