using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModel;
using QwQ_Music.Utilities;

namespace QwQ_Music.Views;

public partial class DesktopLyricsWindow : Window
{
    public static DesktopLyricConfig LyricConfig => ConfigInfoModel.LyricConfig.DesktopLyric;

    public DesktopLyricsWindow()
    {
        if (!LyricConfig.LyricIsEnabled)
        {
            return;
        }

        InitializeComponent();

        Position = new PixelPoint(LyricConfig.LyricPositionX, LyricConfig.LyricPositionY);

        PositionChanged += Window_OnPositionChanged;
        PointerPressed += OnPointerPressed;
        Closed += OnClosed;

        base.Show();

        SetPenetrate(LyricConfig.LockLyricWindow);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        PositionChanged -= Window_OnPositionChanged;
        PointerPressed -= OnPointerPressed;
    }

    private void Window_OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        LyricConfig.LyricPositionX = Position.X;
        LyricConfig.LyricPositionY = Position.Y;
    }

    public void SetPenetrate(bool enabled = true)
    {
        MousePenetrate.SetPenetrate(TryGetPlatformHandle()!.Handle, enabled);
    }
}
