using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Utilities;
using QwQ_Music.ViewModels.Pages;
using QwQ_Music.ViewModels.UserControls;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.Pages;
using QwQ.Avalonia.Utilities.MessageBus;
using static QwQ_Music.Models.LanguageModel;

namespace QwQ_Music.ViewModels;

public partial class MainWindowViewModel : NavigationViewModel
{
    private static readonly Lazy<MainWindowViewModel> _instance = new(() => new MainWindowViewModel());
    public static MainWindowViewModel Instance => _instance.Value;

    private MainWindowViewModel()
        : base("窗口")
    {
        CurrentPage = Pages[0];
        NavigateService.CurrentViewChanged += CurrentViewChanged;

        // 注册热键功能
        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.ViewForward,
            () =>
            {
                if (CanGoForward)
                    ViewForwardCommand.Execute(null);
            }
        );

        HotkeyService.RegisterFunctionAction(
            HotkeyFunction.ViewBackward,
            () =>
            {
                if (CanGoBack)
                    ViewBackwardCommand.Execute(null);
            }
        );
    }

    public void Shutdown()
    {
        NavigateService.CurrentViewChanged -= CurrentViewChanged;
    }

    private void CurrentViewChanged(string obj)
    {
        CanGoBack = NavigateService.CanGoBack;
        CanGoForward = NavigateService.CanGoForward;
    }

    public ObservableCollection<IconItem> IconItems { get; set; } =
        [
            new(MusicName, new GeometryIconSource(IconService.GetIcon("SemiIconSong")), "", true),
            new(ClassificationName, new GeometryIconSource(IconService.GetIcon("SemiIconDisc")), "", true),
            new(OtherName, new GeometryIconSource(IconService.GetIcon("SemiIconKanban")), "", true),
            new(SettingsName, new GeometryIconSource(IconService.GetIcon("SemiIconSetting")), "", true),
        ];

    public void UpdateIconItems(string id, string name, IconSource coverImage)
    {
        var item = IconItems.FirstOrDefault(i => i.Id == id);
        if (item == null)
            return;

        item.Title = name;
        item.Source = coverImage;
    }

    public void RemoveTabPage(string tabId)
    {
        int index = NavigateService.GetChildViewIndex(NavViewName, tabId);
        if (index <= Pages.Count && index > 0)
        {
            Pages.RemoveAt(index);
        }

        NavigateService.RemoveChildView(NavViewName, tabId);
        var i = IconItems.FirstOrDefault(x => x.Id == tabId);
        if (i == null)
            return;

        IconItems.Remove(i);
    }

    public void AddTabPage(string tabId, string title, Bitmap icon, UserControl view)
    {
        if (IconItems.FirstOrDefault(x => x.Id == tabId) == null)
        {
            IconItems.Add(new IconItem(title, new BitmapIconSource(icon), tabId));

            Pages.Add(view);
            NavigateService.AddChildView(NavViewName, tabId);
        }

        NavigationIndex = NavigateService.GetChildViewIndex(NavViewName, tabId);
    }

    [RelayCommand]
    private void RemoveIconItem(IconItem iconItem)
    {
        Pages.RemoveAt(NavigateService.GetChildViewIndex(NavViewName, iconItem.Id));
        NavigateService.RemoveChildView(NavViewName, iconItem.Id);
        IconItems.Remove(iconItem);
    }

    public static string MusicName => Lang[nameof(MusicName)];
    public static string ClassificationName => Lang[nameof(ClassificationName)];
    public static string OtherName => Lang[nameof(OtherName)];
    public static string SettingsName => Lang[nameof(SettingsName)];

    public ObservableCollection<Control> Pages { get; } =
        [
            new AllMusicPage { DataContext = new AllMusicPageViewModel() },
            new ClassificationPage { DataContext = new ClassificationPageViewModel() },
            new OtherPage { DataContext = new OtherPageViewModel() },
            new ConfigMainPage { DataContext = new ConfigPageViewModel() },
        ];

    [ObservableProperty]
    public partial Control CurrentPage { get; set; }

    public int WindowWidth
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(MusicPlayerTrayWidth));
            OnPropertyChanged(nameof(MusicPlayListWidth));
        }
    }

    public int WindowHeight
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(MusicCoverPageHeight));
        }
    }

    public int MusicPlayerTrayWidth => WindowWidth / 2;

    public int MusicPlayListWidth => IsMusicPlayListVisible ? WindowWidth / 4 : 0;

    public int MusicCoverPageHeight => IsMusicCoverPageVisible ? WindowHeight : 0;

    [ObservableProperty]
    public partial bool IsMusicPlayerTrayVisible { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicPlayListWidth))]
    public partial bool IsMusicPlayListVisible { get; set; }

    public bool IsMusicCoverPageVisible
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(MusicCoverPageHeight));

            object brush;
            if (field)
            {
                brush = MusicCoverPageViewModel.ThemeVariant == "Light" ? Brushes.DimGray : Brushes.GhostWhite;
            }
            else
            {
                if (ConfigManager.InterfaceConfig.ThemeConfig.LightDarkMode == "Default")
                {
                    var color = ResourceDictionaryManager.Get<Color>("SemiGrey0Color");

                    brush = IsBrightColor(color) ? Brushes.DimGray : Brushes.GhostWhite;
                }
                else
                {
                    brush =
                        ConfigManager.InterfaceConfig.ThemeConfig.LightDarkMode == "Light"
                            ? Brushes.DimGray
                            : Brushes.GhostWhite;
                }
            }

            ResourceDictionaryManager.Set("CaptionButtonForeground", brush);

            MessageBus
                .CreateMessage(new IsPageVisibleChangeMessage(field, typeof(MusicCoverPage)))
                .FromSender(this)
                .AddReceivers(typeof(MusicPlayListViewModel))
                .Publish();
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NavigationWidth), nameof(IsMusicAlbumCoverTrayVisible))]
    public partial bool IsNavigationExpand { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMusicAlbumCoverTrayVisible))]
    public partial bool IsMusicAlbumCoverPanelPointerOver { get; set; }

    public bool IsMusicAlbumCoverTrayVisible => IsMusicAlbumCoverPanelPointerOver || IsNavigationExpand;

    [ObservableProperty]
    public partial double MusicPlayerTrayYaxisOffset { get; set; }

    [ObservableProperty]
    public partial double MusicPlayListXaxisOffset { get; set; }

    [ObservableProperty]
    public partial double MusicAlbumCoverPanelXaxisOffset { get; set; }

    [ObservableProperty]
    public partial double MusicCoverPageYaxisOffset { get; set; }

    public double NavigationWidth => IsNavigationExpand ? 150 : 75;

    [RelayCommand]
    private void ShowMusicPlaylist()
    {
        IsMusicPlayListVisible = !IsMusicPlayListVisible;
    }

    [RelayCommand]
    private void ShowMusicPlayerPage()
    {
        IsMusicCoverPageVisible = !IsMusicCoverPageVisible;
    }

    [RelayCommand]
    private void GlobalButtonClick()
    {
        IsMusicPlayListVisible = false;
    }

    [RelayCommand]
    private void PointerWheelChanged(PointerWheelEventArgs e)
    {
        IsMusicPlayerTrayVisible = e.Delta.Y switch
        {
            // 检查滚动的方向
            > 0 =>
            // 向上滚动
            true,
            < 0 =>
            // 向下滚动
            false,
            _ => IsMusicPlayerTrayVisible,
        };

        /*
        // 如果支持水平滚动，则可以检查Delta.X
        if (e.Delta.X != 0)
        {
            Console.WriteLine($"Mouse wheel scrolled horizontally by {e.Delta.X}.");
        }
        */
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewBackwardCommand))]
    public partial bool CanGoBack { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ViewForwardCommand))]
    public partial bool CanGoForward { get; set; }

    protected override void OnNavigateTo(int index)
    {
        base.OnNavigateTo(index);
        if (index >= Pages.Count || index < 0)
            return;
        CurrentPage = Pages[index];
    }

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void ViewForward()
    {
        NavigateService.GoForward();
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void ViewBackward()
    {
        NavigateService.GoBack();
    }

    public static bool IsBrightColor(Color color)
    {
        // 亮度归一化到0~1
        double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
        return luminance > 0.5;
    }
}

public partial class IconItem(string title, IconSource source, string id = "", bool alwaysHide = false)
    : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = title;

    [ObservableProperty]
    public partial IconSource Source { get; set; } = source;

    public bool AlwaysHide => alwaysHide;

    public string Id = id;
}
