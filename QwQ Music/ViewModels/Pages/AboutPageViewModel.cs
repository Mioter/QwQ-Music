using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Services;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.ViewModelBases;

namespace QwQ_Music.ViewModels.Pages;

public partial class AboutPageViewModel : ViewModelBase
{
    public static IBrush RandomColor => ColorGenerator.GeneratePastelColor();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundImage))]
    public partial double PageWidth { get; set; }

    public static int PanelHeight => 120;

    [field: AllowNull]
    [field: MaybeNull]
    public Bitmap BackgroundImage
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            if (PageWidth <= 0)
                return MusicExtractor.DefaultCover;

            Task.Run(async () =>
            {
                field = await ImageHelper.LoadFromWeb(new Uri("https://www.loliapi.com/acg/"));

                OnPropertyChanged();
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
            new("AngleSharp", "提供HTML、CSS解析，与DOM构建功能", "https://github.com/AngleSharp/AngleSharp"),
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

    [field: AllowNull]
    [field: MaybeNull]
    public Bitmap Hp
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            Task.Run(async () =>
            {
                field = await ImageHelper.LoadFromWeb(new Uri($"https://github.com/{Name}.png"));
                OnPropertyChanged();
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

    [field: AllowNull]
    [field: MaybeNull]
    public Bitmap Hp
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            Task.Run(async () =>
            {
                field = await ImageHelper.LoadFromWeb(new Uri($"{hpUri}"));
                OnPropertyChanged();
            });

            return MusicExtractor.DefaultCover;
        }
    }

    public string Uri { get; set; } = uri;
}

public record ThankItem(string Name, string Description, string RepoUrl);
