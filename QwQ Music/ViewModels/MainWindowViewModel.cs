using System;
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

        MessageBus
            .ReceiveMessage<ViewChangeMessage>(this)
            .WithHandler(ViewListChangeMessageHandle)
            .AsWeakReference()
            .Subscribe();

        MessageBus
            .ReceiveMessage<ExitReminderMessage>(this)
            .WithHandler(ExitReminderMessageHandler)
            .AsWeakReference()
            .Subscribe();
    }

    private void ExitReminderMessageHandler(ExitReminderMessage message, object sender)
    {
        NavigateService.CurrentViewChanged -= CurrentViewChanged;
    }

    private void CurrentViewChanged(string obj)
    {
        CanGoBack = NavigateService.CanGoBack;
        CanGoForward = NavigateService.CanGoForward;
    }

    public static ObservableCollection<IconItem> IconItems { get; set; } =
        [
            new(MusicName, new GeometryIconSource(IconService.GetIcon("SemiIconSong")), "", true),
            new(ClassificationName, new GeometryIconSource(IconService.GetIcon("SemiIconDisc")), "", true),
            new(OtherName, new GeometryIconSource(IconService.GetIcon("SemiIconKanban")), "", true),
            new(SettingsName, new GeometryIconSource(IconService.GetIcon("SemiIconSetting")), "", true),
        ];

    public static void UpdateIconItems(string id, string name, IconSource coverImage)
    {
        var item = IconItems.FirstOrDefault(i => i.Id == id);
        if (item == null)
            return;

        item.Title = name;
        item.Source = coverImage;
    }

    private async void ViewListChangeMessageHandle(ViewChangeMessage message, object sender)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (message.IsRemove)
                {
                    int index = NavigateService.GetChildViewIndex(NavViewName, message.Id);
                    if (index <= Pages.Count && index > 0)
                    {
                        Pages.RemoveAt(index);
                    }

                    NavigateService.RemoveChildView(NavViewName, message.Id);
                    var i = IconItems.FirstOrDefault(x => x.Id == message.Id);
                    if (i == null)
                        return;

                    IconItems.Remove(i);
                }
                else if (message.View != null)
                {
                    if (IconItems.FirstOrDefault(x => x.Id == message.Id) == null)
                    {
                        IconItems.Add(
                            new IconItem(message.ViewTitle, new BitmapIconSource(message.ViewIcon), message.Id)
                        );

                        Pages.Add(message.View);
                        NavigateService.AddChildView(NavViewName, message.Id);
                    }
                    NavigationIndex = NavigateService.GetChildViewIndex(NavViewName, message.Id);
                }
            });
        }
        catch (Exception e)
        {
            NotificationService.ShowLight(
                new Notification("坏欸", $"查看歌单《{message.ViewTitle}》失败了！QwQ遇到了错误 : \n {e.Message} "),
                NotificationType.Error
            );
        }
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

    public static ObservableCollection<Control> Pages { get; } =
        [
            new AllMusicPage { DataContext = new AllMusicPageViewModel() },
            new ClassificationPage { DataContext = new ClassificationPageViewModel() },
            new OtherPage { DataContext = new OtherPageViewModel() },
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
