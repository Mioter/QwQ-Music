using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using QwQ_Music.Common.Managers;
using QwQ_Music.Windows;

namespace QwQ_Music.Common.Services;

/// <summary>
///     在鼠标进入屏幕顶部中心 1/3 区域时，以鼠标为中心显示 DesktopPlayControlWindow；
///     当鼠标既不在窗口上方，也不在该区域时隐藏窗口。
/// </summary>
public static partial class DesktopPlayControlService {
    public static DesktopPlayControlWindow? DesktopPlayControlWindow;
    private static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(120);
    private static DispatcherTimer? _timer;
    private static bool _isPointerOverWindow;
    private static bool _errorToRecord;

    // Windows
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out WinPoint lpPoint);

    // macOS (CoreGraphics)
    [LibraryImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics", EntryPoint = "CGEventCreateA")]
    private static partial IntPtr CGEventCreate(IntPtr source);

    [LibraryImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static partial CgPoint CGEventGetLocation(IntPtr @event);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    // Linux (X11) - Wayland 不支持时优雅降级
    [LibraryImport("libX11")]
    private static partial IntPtr XOpenDisplay(IntPtr display);

    [LibraryImport("libX11")]
    private static partial int XCloseDisplay(IntPtr display);

    [LibraryImport("libX11")]
    private static partial IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static extern int XQueryPointer(
        IntPtr display,
        IntPtr window,
        out IntPtr root_return,
        out IntPtr child_return,
        out int root_x_return,
        out int root_y_return,
        out int win_x_return,
        out int win_y_return,
        out uint mask_return);

    public static void Start() {
        if (_timer != null)
            return;

        _timer = new DispatcherTimer { Interval = _pollInterval };

        _timer.Tick += OnTick;
        _timer.Start();

        _errorToRecord = false;
    }

    public static void Stop() {
        if (_timer == null)
            return;

        _timer.Stop();
        _timer.Tick -= OnTick;
        _timer = null;

        // 关闭并销毁窗口
        CloseWindow();
    }

    private static void OnTick(object? sender, EventArgs e) {
        if (!TryGetCursorPixelPoint(out PixelPoint cursor))
            return;

        if (!TryGetCurrentScreenBounds(cursor, out PixelRect bounds))
            return;

        bool inTopCenterRegion = IsInTopCenterRegion(cursor, bounds);

        if (DesktopPlayControlWindow != null)
            _isPointerOverWindow = DesktopPlayControlWindow.IsPointerOver;

        if (inTopCenterRegion)
            ShowOrMoveWindow(cursor);
        else if (!_isPointerOverWindow)
            HideWindow();
    }

    private static void ShowOrMoveWindow(PixelPoint cursor) {
        EnsureWindow();

        if (DesktopPlayControlWindow == null)
            return;

        bool justShown = false;

        if (!DesktopPlayControlWindow.IsVisible) {
            DesktopPlayControlWindow.Show();
            DesktopPlayControlWindow.Activate();
            justShown = true;
        }

        // 仅在首次显示时，将窗口定位到鼠标为中心；显示后不再跟随鼠标
        if (!justShown)
            return;

        if (DesktopPlayControlWindow.IsMeasureValid)
            Reposition();
        else
            // 延迟到 UI 循环的下一帧，确保 SizeToContent 生效
            Dispatcher.UIThread.Post(Reposition, DispatcherPriority.Render);
        return;

        // 在布局稳定后将窗口移动到鼠标为中心（水平居中，垂直贴顶）
        void Reposition() {
            if (DesktopPlayControlWindow == null)
                return;
            if (!TryGetCurrentScreenBounds(cursor, out PixelRect bounds))
                return;

            int width = (int)DesktopPlayControlWindow.Bounds.Width;
            if (width <= 0)
                return;

            // 水平以鼠标为中心，并限制在屏幕内
            if (DesktopPlayControlWindow.Screens.Primary == null)
                return;

            double scaling = DesktopPlayControlWindow.Screens.Primary.Scaling;

            int x = (int)(cursor.X - width * scaling / 2);

            // 垂直贴近顶部（工作区顶部）
            int y = bounds.Y;

            DesktopPlayControlWindow.Position = new PixelPoint(x, y);
        }
    }

    private static void HideWindow() {
        if (DesktopPlayControlWindow == null)
            return;

        if (DesktopPlayControlWindow.IsVisible)
            DesktopPlayControlWindow.StartMovingOut = true;
    }

    private static void CloseWindow() {
        if (DesktopPlayControlWindow == null)
            return;

        // 解除事件订阅，防止潜在泄漏
        DesktopPlayControlWindow.PointerEntered -= OnDesktopPlayControlWindowPointerEntered;
        DesktopPlayControlWindow.PointerExited -= OnDesktopPlayControlWindowPointerExited;
        DesktopPlayControlWindow.Closed -= OnDesktopPlayControlWindowClosed;

        DesktopPlayControlWindow.Close();

        DesktopPlayControlWindow = null;
    }

    private static void EnsureWindow() {
        if (DesktopPlayControlWindow != null)
            return;

        DesktopPlayControlWindow = new DesktopPlayControlWindow();

        DesktopPlayControlWindow.PointerEntered += OnDesktopPlayControlWindowPointerEntered;
        DesktopPlayControlWindow.PointerExited += OnDesktopPlayControlWindowPointerExited;
        DesktopPlayControlWindow.Closed += OnDesktopPlayControlWindowClosed;
    }

    private static bool TryGetCursorPixelPoint(out PixelPoint cursor) {
        cursor = default;
        if (Application.Current == null)
            return false;

        try {
            if (OperatingSystem.IsWindows()) {
                if (!GetCursorPos(out WinPoint p))
                    return false;
                cursor = new PixelPoint(p.X, p.Y);
                return true;
            }

            if (OperatingSystem.IsMacOS()) {
                IntPtr evt = CGEventCreate(IntPtr.Zero);
                if (evt == IntPtr.Zero)
                    return false;

                try {
                    CgPoint loc = CGEventGetLocation(evt);
                    // macOS 坐标原点在左下，转换到 Avalonia 使用的左上原点
                    Screens? screens = App.TopLevel?.Screens;
                    Screen? primary = screens?.Primary;
                    if (primary == null) {
                        cursor = new PixelPoint((int)loc.X, (int)loc.Y);
                    } else {
                        PixelRect b = primary.Bounds;
                        int x = (int)Math.Round(loc.X);
                        int y = b.Y + b.Height - (int)Math.Round(loc.Y);
                        cursor = new PixelPoint(x, y);
                    }

                    return true;
                } finally {
                    CFRelease(evt);
                }
            }

            if (OperatingSystem.IsLinux()) {
                IntPtr display = XOpenDisplay(IntPtr.Zero);
                if (display == IntPtr.Zero)
                    return false;

                try {
                    IntPtr root = XDefaultRootWindow(display);
                    int status = XQueryPointer(
                        display,
                        root,
                        out _,
                        out _,
                        out int rootX,
                        out int rootY,
                        out int _,
                        out int _,
                        out uint _);
                    if (status == 0)
                        return false;
                    cursor = new PixelPoint(rootX, rootY);
                    return true;
                } finally {
                    int closeResult = XCloseDisplay(display);
                    if (closeResult != 0)
                        // 可选：记录日志（非致命错误）
                        LoggerService.Warning($"XCloseDisplay failed with code: {closeResult}");
                }
            }

            LoggerService.Warning("非桌面平台，或不支持的桌面环境！");
        } catch (Exception ex) {
            if (_errorToRecord)
                return false;

            // 忽略平台 P/Invoke 失败，降级为失败
            LoggerService.Error($"平台 P/Invoke 失败: {ex.Message}\n{ex.StackTrace}\n无法获取光标位置！");
            _errorToRecord = true;
        }

        return false;
    }

    private static bool TryGetCurrentScreenBounds(PixelPoint cursor, out PixelRect bounds) {
        bounds = default;
        Screens? screens = App.TopLevel?.Screens;
        Screen? screen = screens?.ScreenFromPoint(cursor) ?? screens?.Primary;
        if (screen == null)
            return false;
        bounds = screen.WorkingArea;
        return true;
    }

    private static bool IsInTopCenterRegion(PixelPoint cursor, PixelRect bounds) {
        int regionWidth = bounds.Width / 3;
        int regionX = bounds.X + (bounds.Width - regionWidth) / 2;
        int regionY = bounds.Y;
        int regionHeight = Math.Min(ConfigManager.DesktopControlConfig.TriggerDistance, bounds.Height);

        return cursor.X >= regionX &&
               cursor.X <= regionX + regionWidth &&
               cursor.Y >= regionY &&
               cursor.Y <= regionY + regionHeight;
    }

    private static void OnDesktopPlayControlWindowPointerEntered(object? sender, PointerEventArgs e) {
        _isPointerOverWindow = true;
    }

    private static void OnDesktopPlayControlWindowPointerExited(object? sender, PointerEventArgs e) {
        _isPointerOverWindow = false;
    }

    private static void OnDesktopPlayControlWindowClosed(object? sender, EventArgs e) {
        DesktopPlayControlWindow = null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WinPoint {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CgPoint {
        public double X;
        public double Y;
    }
}