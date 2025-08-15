using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Helper;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.Enums;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Pages;

public partial class AboutPageViewModel : ViewModelBase
{
    private LoadingState? _coverStatus;

    public Bitmap BackgroundImage
    {
        get
        {
            // 如果正在加载中，返回默认封面
            if (_coverStatus is LoadingState.Loading or LoadingState.NotExist)
                return CacheManager.Default;

            // 尝试从缓存获取图片
            if (CacheManager.ImageCache.TryGetValue("关于:背景", out var image))
            {
                _coverStatus = LoadingState.Loaded;

                return image ?? CacheManager.Default;
            }

            Task.Run(async () =>
            {
                var bitmap = await ImageHelper.LoadFromWeb(new Uri("https://www.loliapi.com/acg/"));

                if (bitmap != null)
                {
                    CacheManager.ImageCache["关于:背景"] = bitmap;
                    OnPropertyChanged();
                }
                else
                {
                    _coverStatus = LoadingState.NotExist;
                }
            });

            return CacheManager.Default;
        }
    }

    public ContributorItem[] Contributors { get; } = [new("Mioter", "我感谢我自己"), new("metaone01"), new("AccMoment")];

    public ThankItem[] ThankItems { get; } =
    [
        new("Impressionist", "提供音乐专辑封面取色算法", "Storyteller-Studios/Impressionist"),
        new("SoundFlow", "音频播放核心，提供跨平台的音频播放能力", "LSXPrime/SoundFlow"),
        new("NcmdumpCSharp", "NCM解密支持", "Mioter/NcmdumpCSharp"),
        new("managed-midi", "MIDI音频处理支持", "atsushieno/managed-midi"),
        new("Z440.ALT", "音乐元数据读取与写入", "https://github.com/Zeugma440/atldotnet"),
        new("SkiaSharp", "着色器渲染支持", "https://github.com/mono/SkiaSharp"),
        new("Community Toolkit", "为MVVM开发模式提供基础框架", "https://github.com/CommunityToolkit/dotnet"),
        new("XAML Behaviors", "为XAML开发提供行为扩展", "https://github.com/wieslawsoltes/Xaml.Behaviors"),
    ];

    public SpecialThank[] SpecialThanks { get; } =
    [
        new("兔叽", "https://github.com/rabbitism.png", "https://github.com/rabbitism", "伟大无需多盐"),
        new(
            "Avalonia",
            "https://github.com/avaloniaui.png",
            "https://docs.avaloniaui.net/",
            "Develop Desktop, Embedded, Mobile and WebAssembly apps with C# and XAML. The most popular .NET UI client technology"
        ),
        new(
            "Semi.Avalonia",
            "https://github.com/irihitech.png",
            "https://docs.irihi.tech/semi/",
            "好看的Avalonia主题库"
        ),
        new(
            "Ursa.Avalonia",
            "https://github.com/irihitech.png",
            "https://github.com/irihitech/Ursa.Avalonia",
            "好用的Avalonia控件库"
        ),
        new(
            "LoliAPI",
            "https://cdn.iloli.love/img/liico.webp",
            "https://www.loliapi.com/",
            "这里是LoliAPI,提供免费api服务的站点之一"
        ),
        new(
            ".NET",
            "https://github.com/dotnet.png",
            "https://dotnet.microsoft.com/",
            ".NET 是免费的、开源的、跨平台的框架，用于构建新式应用和强大的云服务。"
        ),
        new(
            "网易云音乐",
            "https://p3.music.126.net/tBTNafgjNnTL1KlZMt7lVA==/18885211718935735.jpg",
            "https://music.163.com/",
            "网易云音乐是一款专注于发现与分享的音乐产品，依托专业音乐人、DJ、好友推荐及社交功能，为用户打造全新的音乐生活。"
        ),
        new(
            "Rider",
            "https://resources.jetbrains.com.cn/storage/products/company/brand/logos/Rider_icon.png",
            "https://www.jetbrains.com/zh-cn/rider/",
            "全球最受喜爱的 .NET 和游戏开发 IDE"
        ),
    ];

    public static string VersionText => Program.VersionText;

    [RelayCommand]
    private static async Task OpenContributorFromGayhub(string name)
    {
        if (App.TopLevel == null)
            return;

        var launcher = App.TopLevel.Launcher;
        await launcher.LaunchUriAsync(new Uri($"https://github.com/{name}"));
    }

    [RelayCommand]
    private static async Task OpenUri(string uri)
    {
        if (App.TopLevel == null)
            return;

        var launcher = App.TopLevel.Launcher;
        await launcher.LaunchUriAsync(new Uri(uri));
    }

    [RelayCommand]
    private static async Task CopyText()
    {
        // 使用topLevel进行操作
        var clipboard = App.TopLevel?.Clipboard;

        if (clipboard == null)
        {
            NotificationService.Error($"版本号“{VersionText}”复制失败！\n无法找到剪贴板！〒▽〒");

            return;
        }

        await clipboard.SetTextAsync(VersionText);

        NotificationService.Success($"版本号“{VersionText}”复制成功！");
    }

    [RelayCommand]
    private async Task SaveBackgroundImage()
    {
        if (App.TopLevel == null)
        {
            return;
        }

        string? path = await FileOperationService.GetSavePathAsync(App.TopLevel, "保存背景图片", [FilePickerFileTypes.ImagePng], $"{Guid.NewGuid()}.png");

        if (path != null)
            await FileOperationService.SaveImageAsync(BackgroundImage, path);
    }
}

public class ContributorItem(string name, object? speak = null) : ObservableObject
{
    private LoadingState? _coverStatus;

    public string Name { get; set; } = name;

    public Bitmap Hp
    {
        get
        {
            if (_coverStatus is LoadingState.Loading or LoadingState.NotExist)
                return CacheManager.Default;

            // 尝试从缓存获取图片
            if (CacheManager.ImageCache.TryGetValue($"贡献者:{Name}", out var image))
            {
                _coverStatus = LoadingState.Loaded;

                return image ?? CacheManager.Default;
            }

            Task.Run(async () =>
            {
                var bitmap = await ImageHelper.LoadFromWebAndDecodeToWidth(
                    new Uri($"https://github.com/{Name}.png")
                );

                if (bitmap != null)
                {
                    CacheManager.ImageCache[$"贡献者:{Name}"] = bitmap;
                    OnPropertyChanged();
                }
                else
                {
                    _coverStatus = LoadingState.NotExist;
                }
            });

            return CacheManager.Default;
        }
    }

    public object Speak { get; set; } = speak ?? "TA没有什么想说的~";
}

public class SpecialThank(string name, string hpUri, string uri, string description) : ObservableObject
{
    private LoadingState? _coverStatus;

    public string Name { get; set; } = name;

    public string Description { get; set; } = description;

    public Bitmap Logo
    {
        get
        {
            if (_coverStatus is LoadingState.Loading or LoadingState.NotExist)
                return CacheManager.Default;

            // 尝试从缓存获取图片
            if (CacheManager.ImageCache.TryGetValue($"鸣谢:{Name}", out var image))
            {
                _coverStatus = LoadingState.Loaded;

                return image ?? CacheManager.Default;
            }

            Task.Run(async () =>
            {
                // 压缩图片到1MB以内
                var bitmap = await ImageHelper.LoadFromWebAndDecodeToWidth(new Uri($"{hpUri}"));

                if (bitmap != null)
                {
                    CacheManager.ImageCache[$"鸣谢:{Name}"] = bitmap;
                    OnPropertyChanged();
                }
                else
                {
                    _coverStatus = LoadingState.NotExist;
                }
            });

            return CacheManager.Default;
        }
    }

    public string Uri { get; set; } = uri;
}

public record ThankItem(string Name, string Description, string RepoUrl);
