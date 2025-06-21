using System.Threading.Tasks;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Definitions;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Pages;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.Pages;
using QwQ.Avalonia.Utilities.MessageBus;

namespace QwQ_Music.ViewModels.UserControls;

public partial class MusicPlayListViewModel : ViewModelBase
{
    public MusicPlayListViewModel()
    {
        MessageBus
            .ReceiveMessage<ThemeColorChangeMessage>(this)
            .WithHandler(ThemeColorChangeMessageHandler)
            .SubscribeAsWeakReference();
        MessageBus
            .ReceiveMessage<IsPageVisibleChangeMessage>(this)
            .WithHandler(IsPageVisibleChangeMessageHandler)
            .SubscribeAsWeakReference();
    }

    private void ThemeColorChangeMessageHandler(ThemeColorChangeMessage message, object sender)
    {
        if (message.PageType != typeof(MusicCoverPage) || !_isCoverPageVisible)
            return;

        ThemeVariant = message.Theme;
    }

    private void IsPageVisibleChangeMessageHandler(IsPageVisibleChangeMessage message, object sender)
    {
        if (message.PageType != typeof(MusicCoverPage))
            return;

        _isCoverPageVisible = message.IsVisible;
        ThemeVariant = _isCoverPageVisible ? MusicCoverPageViewModel.ThemeVariant : ThemeVariant.Default;
    }

    private bool _isCoverPageVisible;

    [ObservableProperty]
    public partial ThemeVariant ThemeVariant { set; get; } = ThemeVariant.Default;

    [ObservableProperty]
    public partial MusicItemModel? SelectedItem { get; set; }
    public static MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    [RelayCommand]
    private async Task ToggleMusicAsync()
    {
        if (SelectedItem == null)
            return;

        await MusicPlayerViewModel.ToggleMusicAsync(SelectedItem).ConfigureAwait(false);
    }

    [RelayCommand]
    private static void ClearMusicPlayList()
    {
        MusicPlayerViewModel.PlayList.MusicItems.Clear();
    }

    [RelayCommand]
    private void SelectedCurrentMusicItem()
    {
        SelectedItem = null;
        SelectedItem = MusicPlayerViewModel.CurrentMusicItem;
    }
}
