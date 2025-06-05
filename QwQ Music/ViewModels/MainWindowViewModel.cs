using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.Pages;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.Pages;
using QwQ.Avalonia.Utilities.MessageBus;
using static QwQ_Music.Models.LanguageModel;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.ViewModels;

public partial class MainWindowViewModel : NavigationViewModel
{
    public MainWindowViewModel()
        : base("窗口")
    {
        MessageBus
            .ReceiveMessage<ViewChangeMessage>(this)
            .WithHandler(ViewChangeMessageHandle)
            .AsWeakReference()
            .Subscribe();
    }

    public ObservableCollection<IconItem> IconItems { get; set; } =
        [
            new(MusicName, new GeometryIconSource(IconService.GetIcon("SemiIconSong")), true),
            new(ClassificationName, new GeometryIconSource(IconService.GetIcon("SemiIconDisc")), true),
            new(StatisticsName, new GeometryIconSource(IconService.GetIcon("SemiIconKanban")), true),
            new(SettingsName, new GeometryIconSource(IconService.GetIcon("SemiIconSetting")), true),
        ];

    private readonly Dictionary<string, string> _viewNameMap = [];

    private async void ViewChangeMessageHandle(ViewChangeMessage message, object sender)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (message.IsRemove)
                {
                    Pages.RemoveAt(NavigateService.GetChildViewIndex(NavViewName, _viewNameMap[message.ViewTitle]));
                    NavigateService.RemoveChildView(NavViewName, _viewNameMap[message.ViewTitle]);
                    var i = IconItems.FirstOrDefault(x => x.Name == message.ViewTitle);
                    if (i == null)
                        return;

                    IconItems.Remove(i);
                    _viewNameMap.Remove(i.Name);
                }
                else if (message.View != null)
                {
                    string viewName = $"{message.GetType()}-{message.ViewTitle}";
                    if (IconItems.FirstOrDefault(x => $"{message.GetType()}-{x.Name}" == $"{viewName}") == null)
                    {
                        IconItems.Add(new IconItem(message.ViewTitle, new BitmapIconSource(message.ViewIcon)));

                        Pages.Add(message.View);
                        NavigateService.AddChildView(NavViewName, viewName);
                        _viewNameMap.Add(message.ViewTitle, viewName);
                    }
                    NavigationIndex = NavigateService.GetChildViewIndex(NavViewName, viewName);
                }
            });
        }
        catch (Exception e)
        {
            NotificationService.ShowLight(
                new Notification("坏欸", $"查看歌单《{message.ViewTitle}》失败了！QwQ遇到了错误 : \n {e.Message} "),
                NotificationType.Error,
                showClose: false
            );
        }
    }

    [RelayCommand]
    private void RemoveIconItem(IconItem iconItem)
    {
        Pages.RemoveAt(NavigateService.GetChildViewIndex(NavViewName, _viewNameMap[iconItem.Name]));
        NavigateService.RemoveChildView(NavViewName, _viewNameMap[iconItem.Name]);
        IconItems.Remove(iconItem);
        _viewNameMap.Remove(iconItem.Name);
    }

    public static string MusicName => Lang[nameof(MusicName)];
    public static string ClassificationName => Lang[nameof(ClassificationName)];
    public static string StatisticsName => Lang[nameof(StatisticsName)];
    public static string SettingsName => Lang[nameof(SettingsName)];

    public static ObservableCollection<Control> Pages { get; } =
        [
            new AllMusicPage { DataContext = new AllMusicPageViewModel() },
            new ClassificationPage { DataContext = new ClassificationPageViewModel() },
            new StatisticsPage { DataContext = new StatisticsPageViewModel() },
            new ConfigMainPage { DataContext = new ConfigPageViewModel() },
        ];

    [ObservableProperty]
    public partial Control CurrentPage { get; set; } = Pages[0];

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
    [NotifyPropertyChangedFor(nameof(IsBackgroundLayerVisible), nameof(MusicPlayListWidth))]
    public partial bool IsMusicPlayListVisible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MusicCoverPageHeight))]
    public partial bool IsMusicCoverPageVisible { get; set; }

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

    public static bool IsBackgroundLayerVisible => false;

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

    protected override bool InNavigateTo(int index)
    {
        if (index < IconItems.Count && index >= 0)
            return base.InNavigateTo(index);

        ViewBackward();
        return false;
    }

    protected override void OnNavigateTo(int index)
    {
        base.OnNavigateTo(index);
        CurrentPage = Pages[index];
        UpdateNavigationProperties();
    }

    private void UpdateNavigationProperties()
    {
        CanGoBack = NavigateService.CanGoBack;
        CanGoForward = NavigateService.CanGoForward;
    }

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void ViewForward()
    {
        NavigateService.GoForward();
        UpdateNavigationProperties();
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void ViewBackward()
    {
        NavigateService.GoBack();
        UpdateNavigationProperties();
    }
}

public record IconItem(string Name, IconSource Source, bool AlwaysHide = false);
