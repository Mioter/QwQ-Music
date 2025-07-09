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
        // 获取清理前的内存信息
        long beforeTotalMemory = GC.GetTotalMemory(forceFullCollection: false);
        using var beforeProc = Process.GetCurrentProcess();
        long beforeWorkingSet = beforeProc.WorkingSet64;

        // 执行垃圾回收
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // 获取清理后的内存信息
        long afterTotalMemory = GC.GetTotalMemory(forceFullCollection: false);
        using var afterProc = Process.GetCurrentProcess();
        long afterWorkingSet = afterProc.WorkingSet64;

        // 计算释放的内存
        long releasedManagedMemory = beforeTotalMemory - afterTotalMemory;
        long releasedWorkingSet = beforeWorkingSet - afterWorkingSet;

        string platform = RuntimeInformation.OSDescription;
        string info = $"[MemoryCleaner] 当前平台: {platform}\n";
        info += $"托管内存: {afterTotalMemory / 1024 / 1024} MB";
        
        if (releasedManagedMemory > 0)
        {
            info += $" (释放了 {releasedManagedMemory / 1024 / 1024} MB)";
        }
        
        info += $"\n进程物理内存: {afterWorkingSet / 1024 / 1024} MB";
        
        if (releasedWorkingSet > 0)
        {
            info += $" (释放了 {releasedWorkingSet / 1024 / 1024} MB)";
        }

        return info;
    }
}
