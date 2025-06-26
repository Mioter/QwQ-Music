using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia;
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
                var bmp = await ImageHelper.LoadFromWeb(new Uri("https://www.loliapi.com/acg/"));
                if (bmp != null)
                {
                    int targetWidth = (int)PageWidth;
                    int targetHeight = PanelHeight;
                    var pixelSize = new PixelSize(targetWidth, targetHeight);

                    // 计算缩放比例（cover）
                    double scale = Math.Max(
                        targetWidth / (double)bmp.PixelSize.Width,
                        targetHeight / (double)bmp.PixelSize.Height
                    );
                    int srcW = (int)(targetWidth / scale);
                    int srcH = (int)(targetHeight / scale);
                    int srcX = Math.Max(0, (bmp.PixelSize.Width - srcW) / 2);
                    int srcY = Math.Max(0, (bmp.PixelSize.Height - srcH) / 2);

                    var srcRect = new Rect(srcX, srcY, srcW, srcH);
                    var destRect = new Rect(0, 0, targetWidth, targetHeight);

                    var rtb = new RenderTargetBitmap(pixelSize);
                    using (var ctx = rtb.CreateDrawingContext())
                    {
                        ctx.DrawImage(bmp, srcRect, destRect);
                    }
                    field = rtb;
                }
                else
                {
                    field = bmp;
                }
                OnPropertyChanged();
            });

            return MusicExtractor.DefaultCover;
        }
    }

    public ContributorItem MainContributor { get; } = new("Mioter");

    public ContributorItem[] Contributors { get; set; } = [new("metaone01"), new("AccMoment")];

    public ThankItem[] ThankItems { get; set; } =
    [
        new("Impressionist", "提供音乐专辑封面取色算法", "Storyteller-Studios/Impressionist"),
        new("SoundFlow", "音频播放核心，提供跨平台的音频播放能力", "LSXPrime/SoundFlow"),
        new("NcmdumpCSharp", "NCM解密支持", "Mioter/NcmdumpCSharp"),
        new("managed-midi", "MIDI音频处理支持", "atsushieno/managed-midi"),
    ];

    public SpecialThank[] SpecialThanks { get; set; } =
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
        new("LoliAPI", "https://cdn.iloli.love/img/liico.webp", "https://www.loliapi.com/", "这里是LoliAPI,提供免费api服务的站点之一"),
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
