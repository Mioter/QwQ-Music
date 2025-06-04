using System;
using Avalonia.Controls;
using Avalonia.Input;
using QwQ_Music.Services;
using QwQ_Music.ViewModels;

namespace QwQ_Music.Views;

public partial class MainWindow : Window
{
    private readonly HotkeyService _hotkeyService;

    public MainWindow()
    {
        InitializeComponent();

        Closed += OnClosed;

        // 初始化热键服务
        _hotkeyService = new HotkeyService(MusicPlayerViewModel.Instance);

        // 注册按键事件
        KeyDown += MainWindow_KeyDown;

        Width = 1200;
        Height = 800;

        DataContext = new MainWindowViewModel();
        MusicCoverPagePanel.PointerPressed += MusicCoverPagePanelOnPointerPressed;
        Closed += OnClosed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        MusicCoverPagePanel.PointerPressed -= MusicCoverPagePanelOnPointerPressed;
        KeyDown -= MainWindow_KeyDown;
    }

    private void MusicCoverPagePanelOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        // 使用热键服务处理按键事件
        _hotkeyService.HandleKeyDown(e);
    }
}
