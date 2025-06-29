using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Helper;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.Pages;

public partial class AboutPageViewModel : ViewModelBase
{
    public static IBrush RandomColor => ColorGenerator.GeneratePastelColor();

    private CoverStatus? _coverStatus;

    public Bitmap BackgroundImage
    {
        get
        {
            // 如果正在加载中，返回默认封面
            if (_coverStatus is CoverStatus.Loading or CoverStatus.NotExist)
                return MusicExtractor.DefaultCover;

            // 尝试从缓存获取图片
            if (MusicExtractor.ImageCache.TryGetValue("关于:背景", out var image))
            {
                _coverStatus = CoverStatus.Loaded;
                return image!;
            }

            Task.Run(async () =>
            {
                // 压缩图片到3MB以内
                const long maxSizeInBytes = 3 * 1024 * 1024; // 3MB
                var bitmap = await ImageHelper.LoadFromWebAndCompress(
                    new Uri("https://www.loliapi.com/acg/"),
                    maxSizeInBytes
                );

                if (bitmap != null)
                {
                    MusicExtractor.ImageCache["关于:背景"] = bitmap;
                    OnPropertyChanged();
                }
                else
                {
                    _coverStatus = CoverStatus.NotExist;
                }
            });

            return MusicExtractor.DefaultCover;
        }
    }

    public ContributorItem MainContributor { get; } = new("Mioter");

    public ContributorItem[] Contributors { get; } = [new("metaone01"), new("AccMoment")];

    public ThankItem[] ThankItems { get; } =
        [
            new("Impressionist", "提供音乐专辑封面取色算法", "Storyteller-Studios/Impressionist"),
            new("SoundFlow", "音频播放核心，提供跨平台的音频播放能力", "LSXPrime/SoundFlow"),
            new("NcmdumpCSharp", "NCM解密支持", "Mioter/NcmdumpCSharp"),
            new("managed-midi", "MIDI音频处理支持", "atsushieno/managed-midi"),
            new("Z440.ALT", "音乐元数据读取与写入", "https://github.com/Zeugma440/atldotnet"),
            new("managed-midi", "MIDI音频处理支持", "atsushieno/managed-midi"),
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

    [RelayCommand]
    private static async Task OpenContributorFromGayhub(string name)
    {
        var launcher = App.TopLevel.Launcher;
        await launcher.LaunchUriAsync(new Uri($"https://github.com/{name}"));
    }

    [RelayCommand]
    private static async Task OpenUri(string uri)
    {
        var launcher = App.TopLevel.Launcher;
        await launcher.LaunchUriAsync(new Uri(uri));
    }
}

public class ContributorItem(string name, string speak = "TA没有什么想说的~") : ObservableObject
{
    public string Name { get; set; } = name;

    private CoverStatus? _coverStatus;

    public Bitmap Hp
    {
        get
        {
            if (_coverStatus is CoverStatus.Loading or CoverStatus.NotExist)
                return MusicExtractor.DefaultCover;

            // 尝试从缓存获取图片
            if (MusicExtractor.ImageCache.TryGetValue($"贡献者:{Name}", out var image))
            {
                _coverStatus = CoverStatus.Loaded;
                return image!;
            }

            Task.Run(async () =>
            {
                // 压缩图片到1MB以内（头像通常较小）
                const long maxSizeInBytes = 1 * 1024 * 1024; // 1MB
                var bitmap = await ImageHelper.LoadFromWebAndCompress(
                    new Uri($"https://github.com/{Name}.png"),
                    maxSizeInBytes
                );

                if (bitmap != null)
                {
                    MusicExtractor.ImageCache[$"贡献者:{Name}"] = bitmap;
                    OnPropertyChanged();
                }
                else
                {
                    _coverStatus = CoverStatus.NotExist;
                }
            });

            return MusicExtractor.DefaultCover;
        }
    }

    public string Speak { get; set; } = speak;
}

public class SpecialThank(string name, string hpUri, string uri, string description) : ObservableObject
{
    public string Name { get; set; } = name;

    public string Description { get; set; } = description;

    private CoverStatus? _coverStatus;

    public Bitmap Logo
    {
        get
        {
            if (_coverStatus is CoverStatus.Loading or CoverStatus.NotExist)
                return MusicExtractor.DefaultCover;

            // 尝试从缓存获取图片
            if (MusicExtractor.ImageCache.TryGetValue($"鸣谢:{Name}", out var image))
            {
                _coverStatus = CoverStatus.Loaded;
                return image!;
            }

            Task.Run(async () =>
            {
                // 压缩图片到2MB以内
                const long maxSizeInBytes = 2 * 1024 * 1024; // 2MB
                var bitmap = await ImageHelper.LoadFromWebAndCompress(new Uri($"{hpUri}"), maxSizeInBytes);

                if (bitmap != null)
                {
                    MusicExtractor.ImageCache[$"鸣谢:{Name}"] = bitmap;
                    OnPropertyChanged();
                }
                else
                {
                    _coverStatus = CoverStatus.NotExist;
                }
            });

            return MusicExtractor.DefaultCover;
        }
    }

    public string Uri { get; set; } = uri;
}

public record ThankItem(string Name, string Description, string RepoUrl);
