using System;
using System.Runtime.InteropServices;

namespace QwQ_Music.Services;

public static class MousePenetrateService
{
    private const uint WsExLayered = 0x80000;
    private const int WsExTransparent = 0x20;
    private const int GwlStyle = -16;
    private const int GwlExstyle = -20;
    private const int LwaAlpha = 0x2;

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
        uint style = GetWindowLong(handle, GwlExstyle);
        if (flag)
            SetWindowLong(handle, GwlExstyle, style | WsExTransparent | WsExLayered);
        else
            SetWindowLong(handle, GwlExstyle, style & ~(WsExTransparent | WsExLayered));
        SetLayeredWindowAttributes(handle, 0, 100, LwaAlpha);
    }
}
