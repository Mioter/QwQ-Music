﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using QwQ_Music.Models;

namespace QwQ_Music.Pages;

public partial class LyricConfigPage : UserControl {
    public LyricConfigPage() { InitializeComponent(); }

    private void MaximizeLyricWidth(object sender, RoutedEventArgs e) {
        var screen = TopLevel.GetTopLevel(this)!.Screens!.Primary!;
        DesktopLyricConfig.Size = new Size(screen.WorkingArea.Width / screen.Scaling, DesktopLyricConfig.Size.Height);
    }

    private void MaximizeLyricHeight(object? sender, RoutedEventArgs e) {
        var screen = TopLevel.GetTopLevel(this)!.Screens!.Primary!;
        DesktopLyricConfig.Size = new Size(DesktopLyricConfig.Size.Width, screen.WorkingArea.Height / screen.Scaling);
    }

    private void ResetLyricWidth(object? sender, RoutedEventArgs e) { }
    private void ResetLyricHeight(object? sender, RoutedEventArgs e) { }

    private void SetLyricBackground(object? sender, PointerEventArgs e) {
        DesktopLyricConfig.BackgroundColor = Color.Parse("#80000000");
    }

    private void UnsetLyricBackground(object? sender, PointerEventArgs e) {
        DesktopLyricConfig.BackgroundColor = Colors.Transparent;
    }
}