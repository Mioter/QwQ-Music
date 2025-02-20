using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace QwQ_Music.Amusing;

public class LoveWindow : Window
{
    private static readonly List<LoveWindow> AllWindows = [];
    private readonly int _dx;
    private readonly int _dy;
    private readonly LoveWindow? _centerWindow;

    public LoveWindow(PixelPoint position, Color color, string content, int dx = 0, int dy = 0, LoveWindow? centerWindow = null, bool isCenter = false)
    {
        AllWindows.Add(this);
        Closed += OnWindowClosed;

        if (isCenter)
        {
            _centerWindow = this;
            Closed += OnCenterWindowClosed;
            PositionChanged += OnCenterPositionChanged; // 监听中心窗口的位置变化
        }
        else
        {
            _dx = dx;
            _dy = dy;
            _centerWindow = centerWindow;
        }

        InitializeWindow(position, color, content, isCenter);
    }

    private void InitializeWindow(PixelPoint position, Color color, string content, bool isCenter)
    {
        Title = $"♥️ > {AllWindows.Count}";
        Width = isCenter ? 200 : 80;
        Height = isCenter ? 160 : 80;
        Position = position;
        WindowStartupLocation = WindowStartupLocation.Manual;
        CanResize = false;
        Background = new SolidColorBrush(color);

        var textBlock = new TextBlock
        {
            Text = content,
            FontSize = isCenter ? 40 : 28,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Effect = new DropShadowEffect
            {
                BlurRadius = 10,
                Color = Colors.DarkGray,
                Opacity = 10,
            },
        };

        var panel = new Panel { Background = Brushes.Transparent };
        panel.Children.Add(textBlock);
        Content = panel;

        InitializeInteractions(panel);
    }

    private void OnCenterPositionChanged(object? sender, PixelPointEventArgs e)
    {
        // 中心窗口移动时更新所有关联小窗口的位置
        foreach (var window in AllWindows.Where(w => w._centerWindow == this))
        {
            window.Position = new PixelPoint(Position.X + window._dx, Position.Y + window._dy);
        }
    }

    private static void OnCenterWindowClosed(object? sender, EventArgs e)
    {
        var windowsToClose = AllWindows.ToList();
        foreach (var window in windowsToClose.OrderByDescending(w => AllWindows.IndexOf(w)))
        {
            window.Close();
        }
    }

    private void InitializeInteractions(Panel dragControl)
    {
        dragControl.PointerPressed += (_, e) => BeginMoveDrag(e);
        dragControl.DoubleTapped += (_, _) => Close();
        dragControl.PointerEntered += (_, _) =>
            RenderTransform = new ScaleTransform(1.05, 1.05);
        dragControl.PointerExited += (_, _) =>
            RenderTransform = null;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        // 清理事件订阅
        Closed -= OnWindowClosed;
        if (_centerWindow == this)
        {
            Closed -= OnCenterWindowClosed;
            PositionChanged -= OnCenterPositionChanged;
        }

        // 清理 RenderTransform
        RenderTransform = null;

        // 从全局列表中移除
        AllWindows.Remove(this);
    }
}