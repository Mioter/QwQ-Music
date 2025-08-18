using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Utilities;
using DesktopLyricConfig = QwQ_Music.Models.ConfigModels.DesktopLyricConfig;

namespace QwQ_Music.Views;

public partial class DesktopLyricsWindow : Window
{
    public DesktopLyricsWindow()
    {
        if (!LyricConfig.LyricIsEnabled)
        {
            return;
        }
        
#if DEBUG
        this.AttachDevTools();
#endif

        InitializeComponent();

        Position = LyricConfig.Position;

        PositionChanged += Window_OnPositionChanged;
        Closed += OnClosed;
        base.Show();

        SetPenetrate(LyricConfig.LockLyricWindow);
    }

    public static DesktopLyricConfig LyricConfig => ConfigManager.LyricConfig.DesktopLyric;

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        SizeToContent = SizeToContent.Height;
        base.OnPointerReleased(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
        base.OnPointerPressed(e);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        PositionChanged -= Window_OnPositionChanged;
    }

    private void Window_OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        LyricConfig.Position = Position;
    }

    public void SetPenetrate(bool enabled = true)
    {
        if (TryGetPlatformHandle() is { } handle)
        {
            MousePenetrate.SetPenetrate(handle.Handle, enabled);
        }
    }
}
