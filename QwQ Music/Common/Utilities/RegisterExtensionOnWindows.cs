#if _WIN_NT
namespace QwQ_Music.Common.Utilities;

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

public static class RegisterFileAssociationHelper {
    public static void RegisterAppForOpenWithList(string appPath, IEnumerable<string> extensions) {
        if (string.IsNullOrEmpty(appPath))
            throw new ArgumentNullException(nameof(appPath));
        if (!File.Exists(appPath))
            throw new FileNotFoundException("应用程序路径不存在", appPath);

        string appName = Path.GetFileName(appPath);
        // 注册应用程序命令
        string appKeyPath = $@"Software\Classes\Applications\{appName}";
        using (RegistryKey appKey = Registry.CurrentUser.CreateSubKey(appKeyPath)) {
            // 可选：设置默认值，或添加支持的文件类型等
            using (RegistryKey shellKey = appKey.CreateSubKey("shell"))
            using (RegistryKey openKey = shellKey.CreateSubKey("open"))
            using (RegistryKey commandKey = openKey.CreateSubKey("command")) {
                commandKey.SetValue("", $"\"{appPath}\" \"%1\"");
            }
        }

        // 对于每个扩展名，添加到 OpenWithList
        foreach (string ext in extensions) {
            if (ext[0] != '.')
                continue;
            string extKeyPath = $@"Software\Classes\{ext}";
            using var extKey = Registry.CurrentUser.CreateSubKey(extKeyPath);
            // 创建 OpenWithList 子键
            using var openWithListKey = extKey.CreateSubKey("OpenWithList");
            // 添加程序名作为子项（key）
            using var appSubKey = openWithListKey.CreateSubKey(appName);
            // 可以设置默认值为空，或者设置一些信息
            appSubKey.SetValue("", "");
        }
    }
}
#endif