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
        // TODO: 实现 Linux 平台的鼠标穿透
        // 可以使用 X11 或 Wayland 的相应 API
        LoggerService.Warning("Linux 平台的鼠标穿透功能尚未实现");
    }

    private static void SetPenetrateMacOs(IntPtr handle, bool flag)
    {
        // TODO: 实现 macOS 平台的鼠标穿透
        // 可以使用 Cocoa/NSWindow API
        LoggerService.Warning("macOS 平台的鼠标穿透功能尚未实现");
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
