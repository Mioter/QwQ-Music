using System;
using System.Collections.Generic;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty][NotifyPropertyChangedFor(nameof(IsBackgroundLayerVisible))]
    private bool _isMusicPlayListVisible;

    [ObservableProperty] private bool _isNavigationExpand = true;

    [ObservableProperty] private bool _isWindowMaximizedOrFullScreen;

    [ObservableProperty] private WindowState _mainWindowState;

    [ObservableProperty] private double _musicPlayerTrayYaxisOffset;

    [ObservableProperty] private double _musicPlayListXaxisOffset;

    [ObservableProperty] private double _navigationBarWidth;

    [ObservableProperty] private object? _navigationSelectedItem;

    [ObservableProperty] private UserControl? _pageContent;
    
    public bool IsBackgroundLayerVisible =>
        /*if(false) // 暂且如此，待后续添加控制逻辑
                return IsMusicPlayListVisible;*/
        false;

    public MainWindowViewModel()
    {
        SetNavigationBarWidth();
    }

    partial void OnMusicPlayListXaxisOffsetChanging(double oldValue, double newValue)
    {
        if (oldValue < newValue || newValue > 0.9) return;

        IsMusicPlayListVisible = false;
        IsMusicPlayListVisible = true;
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

    [RelayCommand]
    private void ShowMusicPlaylist()
    {
        IsMusicPlayListVisible = !IsMusicPlayListVisible;
    }

    [RelayCommand]
    private void GlobalButtonClick()
    {
        IsMusicPlayListVisible = false;
    }

    private void TogglePage(object value)
    {
        if (value is not StackPanel stackPanel || stackPanel.Children[1] is not TextBlock { Text: not null } textBlock) return;

        PageContent = _textToPageDictionary[textBlock.Text];
    }

    private void SetNavigationBarWidth()
    {
        NavigationBarWidth = IsNavigationExpand ? IsWindowMaximizedOrFullScreen ? 200 : 150 : 75;
    }

    public void ManualCleaning()
    {
        foreach (var userControl in _textToPageDictionary.Values)
        {
            if (userControl.DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
