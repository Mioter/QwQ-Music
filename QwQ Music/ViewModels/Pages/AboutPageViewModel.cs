using System.Runtime.CompilerServices;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public partial class AboutPageViewModel : ViewModelBase {
    /*
    private LoadingState? _coverStatus;

    public Bitmap BackgroundImage
    {
        get
        {
            // 如果正在加载中，返回默认封面
            if (_coverStatus == LoadingState.Loading)
                return CacheManager.Default;

            // 尝试从缓存获取图片
            if (CacheManager.ImageCache.TryGetValue("关于:背景", out var image)
             && image != null)
            {
                return image;
            }

            _coverStatus = LoadingState.Loading;

            Task.Run(() =>
            {
                var bitmap = CacheManager.GetBuiltInImage("蓝发猫猫.webp");

                CacheManager.ImageCache["关于:背景"] = bitmap;
                _coverStatus = LoadingState.Loaded;

                OnPropertyChanged();
            });

            return CacheManager.Default;
        }
    }
    */

    public ContributorItem[] Contributors { get; } = [new("Mioter", "我感谢我自己"), new("metaone01"), new("AccMoment")];

    public ThankItem[] ThankItems { get; } = [
        new("Impressionist", "提供音乐专辑封面取色算法", "Storyteller-Studios/Impressionist"),
        new("SoundFlow", "音频播放核心，提供跨平台的音频播放能力", "LSXPrime/SoundFlow"),
        new("NcmdumpCSharp", "NCM解密支持", "Mioter/NcmdumpCSharp"),
        new("managed-midi", "MIDI音频处理支持", "atsushieno/managed-midi"),
        new("Z440.ALT", "音乐元数据读取与写入", "https://github.com/Zeugma440/atldotnet"),
        new("SkiaSharp", "着色器渲染支持", "https://github.com/mono/SkiaSharp"),
        new("Community Toolkit", "为MVVM开发模式提供基础框架", "https://github.com/CommunityToolkit/dotnet"),
        new("XAML Behaviors", "为XAML开发提供行为扩展", "https://github.com/wieslawsoltes/Xaml.Behaviors")
    ];

    public SpecialThank[] SpecialThanks { get; } = [
        new("兔叽", "https://github.com/rabbitism.png", "https://github.com/rabbitism", "伟大无需多盐"),
        new(
            "Avalonia",
            "https://github.com/avaloniaui.png",
            "https://docs.avaloniaui.net/",
            "Develop Desktop, Embedded, Mobile and WebAssembly apps with C# and XAML. The most popular .NET UI client technology"),
        new("Semi.Avalonia", "https://github.com/irihitech.png", "https://docs.irihi.tech/semi/", "好看的Avalonia主题库"),
        new(
            "Ursa.Avalonia",
            "https://github.com/irihitech.png",
            "https://github.com/irihitech/Ursa.Avalonia",
            "好用的Avalonia控件库"),
        new(
            ".NET",
            "https://github.com/dotnet.png",
            "https://dotnet.microsoft.com/",
            ".NET 是免费的、开源的、跨平台的框架，用于构建新式应用和强大的云服务。"),
        new(
            "网易云音乐",
            "https://p3.music.126.net/tBTNafgjNnTL1KlZMt7lVA==/18885211718935735.jpg",
            "https://music.163.com/",
            "网易云音乐是一款专注于发现与分享的音乐产品，依托专业音乐人、DJ、好友推荐及社交功能，为用户打造全新的音乐生活。"),
        new(
            "Rider",
            "https://resources.jetbrains.com.cn/storage/products/company/brand/logos/Rider_icon.png",
            "https://www.jetbrains.com/zh-cn/rider/",
            "全球最受喜爱的 .NET 和游戏开发 IDE"),
        new(
            "沙丢sado",
            "https://i0.hdslb.com/bfs/face/6990461e04e9c3acdd7798e575f2097bea747864.jpg@128w_128h_1c_1s.webp",
            "https://space.bilibili.com/3546706807360209",
            "提供了此页的背景图~"),
        new("沙雕群友", "https://p.qlogo.cn/gh/397510870/397510870/100/", "https://qm.qq.com/q/kRktVpnTIA", "397510870")
    ];

    public static string Version => Program.Version;

    [RelayCommand]
    private static void OpenContributorFromGayHub(string name) {
        App.TopLevel?.Launcher.LaunchUriAsync(new Uri($"https://github.com/{name}"))
           .ContinueWith(LoggerService.HandleException)
           .ConfigureAwait(false);
    }

    [RelayCommand]
    private static void OpenUri(string uri) {
        App.TopLevel?.Launcher.LaunchUriAsync(new Uri(uri))
           .ContinueWith(LoggerService.HandleException)
           .ConfigureAwait(false);
    }

    [RelayCommand]
    private static void CopyText() {
        // 使用topLevel进行操作
        IClipboard? clipboard = App.TopLevel?.Clipboard;

        if (clipboard == null) {
            LoggerService.Warning($"版本号复制失败：剪贴板不存在。TopLevel:{App.TopLevel}");
            NotificationService.Error($"版本号“{Version}”复制失败！\n无法找到剪贴板！〒▽〒");
            return;
        }

        clipboard.SetTextAsync(Version)
                 .ContinueWith(LoggerService.HandleException)
                 .ConfigureAwait(false)
                 .GetAwaiter()
                 .OnCompleted(() => {
                     LoggerService.Info("版本号复制成功。");
                     NotificationService.Success($"版本号“{Version}”复制成功！");
                 });
    }
}

public class ContributorItem(string name, string? comment = null) : ObservableObject {
    public string Name { get; set; } = name;

    public Bitmap Hp {
        get {
            ConfiguredValueTaskAwaitable<Bitmap>.ConfiguredValueTaskAwaiter result = CacheManager.TryLoadFromWebAsync(
                    Name,
                    "贡献者",
                    "头像",
                    new Uri($"https://github.com/{Name}.png"),
                    () => OnPropertyChanged())
                .ConfigureAwait(false)
                .GetAwaiter();
            if (result.IsCompleted)
                return result.GetResult();

            return CacheManager.Loading;
        }
    }


    public string Comment { get; set; } = comment ?? "Ta没有什么想说的~";
}

public class SpecialThank(string name, string logoUri, string uri, string description) : ObservableObject {
    public string Name { get; set; } = name;

    public string Description { get; set; } = description;

    public Bitmap Logo {
        get {
            ConfiguredValueTaskAwaitable<Bitmap>.ConfiguredValueTaskAwaiter result = CacheManager
                .TryLoadFromWebAsync(Name, "鸣谢", "Logo", new Uri(logoUri), () => OnPropertyChanged())
                .ConfigureAwait(false)
                .GetAwaiter();
            if (result.IsCompleted)
                return result.GetResult();

            return CacheManager.Loading;
        }
    }

    public string Uri { get; set; } = uri;
}

public readonly record struct ThankItem(string Name, string Description, string RepoUrl);