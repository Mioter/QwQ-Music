using Avalonia;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Common.Utilities;
using AudioPlayManager = QwQ_Music.Common.Managers.AudioPlayManager;

namespace QwQ_Music;

public static class Program {
    public const string Version = "2.2.3";
    public const string AppId = "com.Mioter.QwQMusic";
    public static string[]? OpenWithFiles { get; private set; }

    [STAThread]
    public static async Task Main(string[] args) {
#if _WIN_NT
        if (Environment.ProcessPath is not null)
            RegisterFileAssociationHelper.RegisterAppForOpenWithList(
                Environment.ProcessPath,
                [".mp3", ".wav", ".flac"]);
        else
            // ReSharper disable once MethodHasAsyncOverload
            LoggerService.Warning("无法获取程序路径，未知原因");
#endif
        // ReSharper disable once MethodHasAsyncOverload
        LoggerService.Debug($"启动参数：{string.Join(',', args)}");
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        try {
            // ReSharper disable once MethodHasAsyncOverload
            LoggerService.Info(
                $"""
                 ===========================================
                                                                                                             
                   _|_|                          _|_|          _|      _|                      _|            
                 _|    _|  _|      _|      _|  _|    _|        _|_|  _|_|  _|    _|    _|_|_|        _|_|_|  
                 _|  _|_|  _|      _|      _|  _|  _|_|        _|  _|  _|  _|    _|  _|_|      _|  _|        
                 _|    _|    _|  _|  _|  _|    _|    _|        _|      _|  _|    _|      _|_|  _|  _|        
                   _|_|  _|    _|      _|        _|_|  _|      _|      _|    _|_|_|  _|_|_|    _|    _|_|_|  
                                                           
                                              
                        ▶  QwQ Music v{Version}  🔊
                      "Where emotions meet melody"

                 ===========================================
                 """);
            if (args.Length > 0) {
                Dictionary<bool, string[]> separated =
                    args.GroupBy(Path.Exists).ToDictionary(k => k.Key, v => v.ToArray());
                separated.TryGetValue(true, out string[]? files);
                OpenWithFiles = files;
                args = separated.GetValueOrDefault(false, []);
                if (files is not null) {
                    // ReSharper disable once MethodHasAsyncOverload
                    LoggerService.Info($"带文件启动：{files.Length}个文件");
                }
            }

            BuildAvaloniaApp()
#if DEBUG
                .WithDeveloperTools()
#endif
                .StartWithClassicDesktopLifetime(args);
        } catch (Exception e) {
            await LoggerService.ErrorAsync($"程序异常退出！\n捕捉到未处理异常:\n {e.Message}\n {e.StackTrace}").ConfigureAwait(false);
            throw;
        } finally {
            await ShutdownAsync().ConfigureAwait(false);
        }

        // Environment.Exit(0);
    }

    private static async Task ShutdownAsync() {
        await LoggerService.InfoAsync("正在关闭...").ConfigureAwait(false);
        try {
            await AudioPlayManager.Instance.DisposeAsync().ConfigureAwait(false);
            ConfigManager.SaveConfig(); // 需要用到 AudioPlayManager释放时修改的数据，不要修改前后顺序
            await LoggerService.InfoAsync("设置已保存").ConfigureAwait(false);
            MousePenetrate.ClearCache();
            HotkeyService.ClearCache();
            NavigateService.ClearCache();
            CacheManager.Dispose();
            await MusicItemRepository.Instance.DisposeAsync().ConfigureAwait(false);
            await MusicListRepository.Instance.DisposeAsync().ConfigureAwait(false);
            await MusicListItemsRepository.Instance.DisposeAsync().ConfigureAwait(false);
            await LoggerService.InfoAsync("资源已释放。").ConfigureAwait(false);
        } catch (Exception ex) {
            await LoggerService.ErrorAsync("关闭App时发生错误", ex).ConfigureAwait(false);
        } finally {
            await LoggerService.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static AppBuilder BuildAvaloniaApp() {
        return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
    }
}