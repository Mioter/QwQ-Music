using System;
using System.Runtime.InteropServices;

namespace QwQ_Music.Services;

public static class MousePenetrateService
{
    private const uint WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int LWA_ALPHA = 0x2;

    [DllImport("user32", EntryPoint = "SetWindowLong")]
    private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

    [DllImport("user32", EntryPoint = "GetWindowLong")]
    private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32", EntryPoint = "SetLayeredWindowAttributes")]
    private static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, int bAlpha, int dwFlags);

    /// <summary>
    /// 设置窗体具有鼠标穿透效果
    /// </summary>
    /// <param name="handle">Window Handle</param>
    /// <param name="flag">true穿透，false不穿透</param>
    public static void SetPenetrate(IntPtr handle, bool flag = true)
    {
        uint style = GetWindowLong(handle, GWL_EXSTYLE);
        if (flag)
            SetWindowLong(handle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        else
            SetWindowLong(handle, GWL_EXSTYLE, style & ~(WS_EX_TRANSPARENT | WS_EX_LAYERED));
        SetLayeredWindowAttributes(handle, 0, 100, LWA_ALPHA);
    }
}
