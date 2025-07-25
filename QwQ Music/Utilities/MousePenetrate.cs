using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using QwQ_Music.Services;

namespace QwQ_Music.Utilities;

public static class MousePenetrate
{
    private static readonly Dictionary<IntPtr, bool> _windowStateCache = new();

    // Windows 平台常量
    private const uint WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int GWL_EXSTYLE = -20;
    private const int LWA_ALPHA = 0x2;

    // Windows API
    [DllImport("user32", EntryPoint = "SetWindowLong")]
    private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

    [DllImport("user32", EntryPoint = "GetWindowLong")]
    private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32", EntryPoint = "SetLayeredWindowAttributes")]
    private static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, int bAlpha, int dwFlags);

    // Linux X11 API (如果需要)
    [DllImport("libX11")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11")]
    private static extern int XCloseDisplay(IntPtr display);

    // X11 结构体与API
    [StructLayout(LayoutKind.Sequential)]
    private struct XWindowAttributes
    {
        public int x, y;
        public int width, height;
        public int border_width, depth;
        public IntPtr visual, root;
        public int class_, bit_gravity, win_gravity;
        public int backing_store;
        public uint backing_planes, backing_pixel;
        public bool save_under;
        public IntPtr colormap;
        public bool map_installed, map_state;
        public long all_event_masks, your_event_mask, do_not_propagate_mask;
        public bool override_redirect;
        public IntPtr screen;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XRectangle
    {
        public short x, y;
        public ushort width, height;
    }

    [DllImport("libX11")]
    private static extern int XGetWindowAttributes(IntPtr display, IntPtr window, out XWindowAttributes attributes);

    // X11 Shape 扩展
    [DllImport("libXext", EntryPoint = "XShapeCombineRectangles")]
    private static extern void XShapeCombineRectangles(
        IntPtr display,
        IntPtr window,
        int shape_kind,
        int x_off,
        int y_off,
        [In] XRectangle[]? rectangles,
        int n_rects,
        int op,
        int ordering
    );

    // macOS Objective-C 互操作
    [DllImport("libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string selectorName);

    [DllImport("libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool value);

    /// <summary>
    /// 设置窗体具有鼠标穿透效果
    /// </summary>
    /// <param name="handle">Window Handle</param>
    /// <param name="flag">true穿透，false不穿透</param>
    public static void SetPenetrate(IntPtr handle, bool flag = true)
    {
        try
        {
            // 检查缓存状态
            if (_windowStateCache.TryGetValue(handle, out bool currentState) && currentState == flag)
            {
                return; // 状态未改变，直接返回
            }

            // 根据平台选择实现方式
            if (OperatingSystem.IsWindows())
            {
                SetPenetrateWindows(handle, flag);
            }
            else if (OperatingSystem.IsLinux())
            {
                SetPenetrateLinux(handle, flag);
            }
            else if (OperatingSystem.IsMacOS())
            {
                SetPenetrateMacOs(handle, flag);
            }
            else
            {
                LoggerService.Warning("当前平台不支持鼠标穿透功能");
                return;
            }

            // 更新缓存
            _windowStateCache[handle] = flag;
            LoggerService.Debug($"成功设置窗口 {handle} 的鼠标穿透状态为: {flag}");
        }
        catch (Exception ex)
        {
            LoggerService.Error($"设置鼠标穿透失败: {ex.Message}");
            throw;
        }
    }

    private static void SetPenetrateWindows(IntPtr handle, bool flag)
    {
        uint style = GetWindowLong(handle, GWL_EXSTYLE);

        if (flag)
        {
            // 添加 WS_EX_LAYERED 和 WS_EX_TRANSPARENT 样式
            SetWindowLong(handle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            // 设置完全不透明（255）
            SetLayeredWindowAttributes(handle, 0, 255, LWA_ALPHA);
        }
        else
        {
            // 移除样式
            SetWindowLong(handle, GWL_EXSTYLE, style & ~(WS_EX_TRANSPARENT | WS_EX_LAYERED));
        }
    }

    private static void SetPenetrateLinux(IntPtr handle, bool flag)
    {
        // 检查是否为 Wayland 环境
        string? waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        if (!string.IsNullOrEmpty(waylandDisplay))
        {
            LoggerService.Warning("Wayland 平台暂不支持鼠标穿透。建议使用 X11 或 XWayland 运行。");
            return;
        }
        IntPtr display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
        {
            LoggerService.Error("无法打开 X11 Display");
            return;
        }
        try
        {
            if (flag)
            {
                // 穿透：输入区域设为空
                XShapeCombineRectangles(display, handle, 0, 0, 0, null, 0, 0, 0);
                LoggerService.Debug($"已为窗口 {handle} 启用 X11 鼠标穿透");
            }
            else
            {
                // 还原：输入区域设为窗口本身区域
                if (XGetWindowAttributes(display, handle, out var attr) == 0)
                {
                    LoggerService.Error("获取窗口属性失败，无法还原输入区域");
                }
                else
                {
                    XRectangle[] rects =
                    [
                        new () { x = 0, y = 0, width = (ushort)attr.width, height = (ushort)attr.height },
                    ];
                    XShapeCombineRectangles(display, handle, 0, 0, 0, rects, 1, 0, 0);
                    LoggerService.Debug($"已为窗口 {handle} 还原 X11 输入区域");
                }
            }
        }
        finally
        {
            int closeResult = XCloseDisplay(display);
            if (closeResult != 0)
            {
                LoggerService.Warning("XCloseDisplay 关闭失败");
            }
        }
    }

    private static void SetPenetrateMacOs(IntPtr handle, bool flag)
    {
        // handle 应为 NSWindow* 指针
        IntPtr sel = sel_registerName("setIgnoresMouseEvents:");
        objc_msgSend_bool(handle, sel, flag);
        LoggerService.Debug($"已为窗口 {handle} 设置 macOS 鼠标穿透: {flag}");
    }

    /// <summary>
    /// 清除窗口状态缓存
    /// </summary>
    /// <param name="handle">要清除的窗口句柄，如果为 null 则清除所有缓存</param>
    public static void ClearCache(IntPtr? handle = null)
    {
        if (handle.HasValue)
        {
            _windowStateCache.Remove(handle.Value);
        }
        else
        {
            _windowStateCache.Clear();
        }
    }
}
