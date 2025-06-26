using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace QwQ_Music.Utilities;

/// <summary>
/// 内存清理工具类，支持跨平台主动触发垃圾回收并返回内存信息。
/// </summary>
public static class MemoryCleaner
{
    /// <summary>
    /// 主动触发垃圾回收，并返回清理后的内存信息。
    /// </summary>
    /// <returns>清理后的内存信息字符串</returns>
    public static string CleanAndGetInfo()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long totalMemory = GC.GetTotalMemory(forceFullCollection: false);
        string platform = RuntimeInformation.OSDescription;
        string info = $"[MemoryCleaner] 当前平台: {platform}, 托管内存: {totalMemory / 1024 / 1024} MB";

        // 获取进程物理内存占用
        using var proc = Process.GetCurrentProcess();
        info += $", 进程物理内存: {proc.WorkingSet64 / 1024 / 1024} MB";

        return info;
    }
}
