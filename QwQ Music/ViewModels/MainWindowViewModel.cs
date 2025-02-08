using System.Collections.Generic;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using QwQ_Music.Pages;

namespace QwQ_Music.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly Dictionary<string, UserControl> _textToPageDictionary = new()
    {
        ["音乐"] = new MusicPage
        {
            DataContext = new MusicPageViewModel(),
        },
        ["分类"] = new ClassifiedPage
        {
            DataContext = new ClassifiedPageViewModel(),
        },
        ["统计"] = new StatisticsPage
        {
            DataContext = new StatisticsPageViewModel(),
        },
    };

    [ObservableProperty] private bool _isMusicPlayerTrayVisible = true;

    [ObservableProperty] private bool _isNavigationExpand = true;

    [ObservableProperty] private bool _isWindowMaximizedOrFullScreen;

    [ObservableProperty] private WindowState _mainWindowState;

    [ObservableProperty] private double _navigationBarWidth;

    [ObservableProperty] private double _navigationBarYaxisOffset;

    [ObservableProperty] private object? _navigationSelectedItem;

    [ObservableProperty] private UserControl? _pageContent;

    public MainWindowViewModel()
    {
        SetNavigationBarWidth();
    }

    partial void OnIsNavigationExpandChanged(bool value)
    {
        SetNavigationBarWidth(); // 属性变更后调用方法
    }

    partial void OnMainWindowStateChanged(WindowState value)
    {
        IsWindowMaximizedOrFullScreen = MainWindowState is WindowState.Maximized or WindowState.FullScreen;
        SetNavigationBarWidth();
    }

    partial void OnNavigationSelectedItemChanged(object? value)
    {
        if (value == null) return;
        TogglePage(value);
    }

    private void SetNavigationBarWidth()
    {
        NavigationBarWidth = IsNavigationExpand ? IsWindowMaximizedOrFullScreen ? 200 : 150 : 75;
    }

    private void TogglePage(object value)
    {
        if (value is not StackPanel stackPanel || stackPanel.Children[1] is not TextBlock { Text: not null } textBlock) return;

        PageContent = _textToPageDictionary[textBlock.Text];
    }
}
