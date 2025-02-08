using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common;
using QwQ_Music.Models;
using QwQ_Music.Tools;

namespace QwQ_Music.ViewModels;

public partial class MusicPageViewModel : ViewModelBase
{

    [ObservableProperty] private MusicItemModel? _selectedItem;

    public MusicPlayerViewModel MusicPlayerViewModel { get; } = MusicPlayerViewModel.Instance;

    [RelayCommand]
    private void ToggleMusic()
    {
        if (SelectedItem != null)
            MusicPlayerViewModel.UpdateMusicPlaylist(SelectedItem);
    }

    [RelayCommand]
    private async Task DropFilesAsync(DragEventArgs? e)
    {
        if (e?.Data.Contains(DataFormats.Files) != true) return;

        var filePaths = e.Data.GetFiles()?.ToList();
        var audioFilePaths = AudioFileValidator.FilterAudioFiles(filePaths);

        if (audioFilePaths == null) return;

        var musicItems = await Task.WhenAll(audioFilePaths.Select(MusicExtractor.ExtractMusicInfoAsync));
        foreach (var musicItem in musicItems.Where(item => item != null))
        {
            MusicPlayerViewModel.MusicItems.Add(musicItem!);
        }
    }
}
