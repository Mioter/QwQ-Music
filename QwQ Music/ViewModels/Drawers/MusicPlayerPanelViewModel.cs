using System.Diagnostics;
using System.Timers;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Shader;
using QwQ_Music.Models;
using QwQ_Music.Models.ConfigModels;
using QwQ_Music.ViewModels.Bases;
using MusicItemsManager = QwQ_Music.Common.Managers.MusicItemsManager;
using Timer = System.Timers.Timer;

namespace QwQ_Music.ViewModels.Drawers;

public partial class MusicPlayerPanelViewModel : NavigableViewModel {
    private const int _COLOR_COUNT = 4;

    public static VisualConfig VisualConfig => ConfigManager.UiConfig.VisualConfig;

    private static readonly Color[] _defaultColors = [
        Color.Parse("#FFE2D9"), Color.Parse("#F3ECFE"), Color.Parse("#DFE7FF"), Color.Parse("#E4F2FF")
    ];

    private static readonly IBrush _darkThemeBrush = Brush.Parse("#22FFFFFF");

    private static readonly IBrush _lightThemeBrush = Brush.Parse("#88FFFFFF");
    private Timer? _coverDebounce;

    public MusicPlayerPanelViewModel() : base("播放") {
        _coverDebounce = new Timer(1000) { AutoReset = false };

        if (AudioPlayManager.CurrentMusicItem != PlaylistItemModel.RefDefault)
            OnMusicItemChanged(
                this,
                new MusicItemChangedEventArgs(PlaylistItemModel.RefDefault, AudioPlayManager.CurrentMusicItem));

        AudioPlayManager.MusicItemChanged += OnMusicItemChanged;
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_OnProcessExit;
    }

    public Bitmap Cover {
        get {
            Bitmap image = AudioPlayManager.CurrentMusicItem.Model.Cover;
            if (image == CacheManager.Loading) {
                if (_coverDebounce is null) {
                    _coverDebounce = new Timer(200) { AutoReset = false };
                    _coverDebounce.Elapsed += Update;
                    _coverDebounce.Start();
                }

                field = image;
            } else {
                _coverDebounce?.Stop();
                _coverDebounce?.Dispose();
                _coverDebounce = null;
                SetProperty(ref field, image);
            }


            return field;

            void Update(object? sender, ElapsedEventArgs args) {
                OnPropertyChanged();
                _coverDebounce.Elapsed -= Update;
                _coverDebounce.Close();
                _coverDebounce = null;
            }
        }
    } = CacheManager.NotExist;

    [ObservableProperty]
    public partial Color DropShadowColor { get; private set; } = Avalonia.Media.Colors.Black;

    public static DrawerManager DrawerManager => DrawerManager.Instance;

    public static AudioPlayManager AudioPlayManager => AudioPlayManager.Instance;

    public static RolledLyricConfig RolledLyric { get; } = ConfigManager.LyricConfig.RolledLyric;

    public static SpectrumConfig SpectrumConfig { get; } = ConfigManager.UiConfig.SpectrumConfig;

    public static string ShaderCode => ShaderConstants.WaveWarpShader;

    public double SelectLyricsTimePoint {
        get;
        set {
            if (SetProperty(ref field, value))
                AudioPlayManager.Position = field;
        }
    }

    [ObservableProperty]
    public partial Color[] Colors { get; set; } = _defaultColors;

    [ObservableProperty]
    public partial IBrush SpectrumVisualizerBrush { get; set; } = Brushes.White;

    private void CurrentDomain_OnProcessExit(object? sender, EventArgs e) {
        AudioPlayManager.MusicItemChanged -= OnMusicItemChanged;
        AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_OnProcessExit;
    }

    private void OnMusicItemChanged(object? sender, MusicItemChangedEventArgs args) {
        _ = Cover;
        _ = Task.Run(async Task? () => {
                    try {
                        await UpdateColorsListAsync(args.NewItem.Model).ConfigureAwait(false);
                        await Dispatcher.UIThread.InvokeAsync(UpdateThemeVariantFromColors);
                    } catch (Exception ex) {
                        LoggerService.HandleException(Task.FromException(ex));
                    }
                })
                .ContinueWith(LoggerService.HandleException)
                .ConfigureAwait(false);
    }

    private async Task UpdateColorsListAsync(MusicItemModel musicItem) {
        Color[] resolvedColors;

        // 如果没有封面Id，直接使用默认颜色
        if (!musicItem.HasCover) {
            resolvedColors = _defaultColors;
            await Dispatcher.UIThread.InvokeAsync(() => Colors = resolvedColors);

            return;
        }

        // 尝试从已缓存的颜色中获取
        if (musicItem.CoverColors is { Length: >= _COLOR_COUNT }) {
            resolvedColors = [.. musicItem.CoverColors.Select(Color.Parse)];
            await Dispatcher.UIThread.InvokeAsync(() => Colors = resolvedColors);

            return;
        }

        // 提取新的颜色
        Color[]? colorsList = await GetColorPaletteAsync(musicItem.Thumbnail, _COLOR_COUNT).ConfigureAwait(false);

        // 使用提取的颜色，为null则使用默认颜色
        resolvedColors = colorsList ?? _defaultColors;
        await Dispatcher.UIThread.InvokeAsync(() => Colors = resolvedColors);

        // 缓存提取的颜色
        if (colorsList != null) {
            musicItem.CoverColors = colorsList.Select(x => x.ToString()).ToArray();


            await MusicItemsManager.UpdateAsync(
                                       musicItem,
                                       new Dictionary<string, object?> {
                                           [nameof(MusicItemModel.CoverColors)] = string.Join(
                                               "、",
                                               musicItem.CoverColors)
                                       })
                                   .ConfigureAwait(false);
        }
    }

    private void UpdateThemeVariantFromColors() {
        Debug.Assert(Dispatcher.UIThread.CheckAccess());
        if (Colors.Length == 0) {
            DrawerManager.Instance.MusicPlayerPanelThemeVariant = "Default";

            return;
        }

        // 计算平均亮度
        double totalLuminance = Colors.Sum(c => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0);
        double avgLuminance = totalLuminance / Colors.Length;

        // 根据平均亮度设置主题（反色）
        bool isDarkTheme = avgLuminance > 0.5;

        SpectrumVisualizerBrush = isDarkTheme ? _lightThemeBrush : _darkThemeBrush;
        DrawerManager.Instance.MusicPlayerPanelThemeVariant = isDarkTheme ? "Light" : "Dark";
        DropShadowColor = isDarkTheme ? Avalonia.Media.Colors.White : Avalonia.Media.Colors.Black;
    }

    private static async Task<Color[]?> GetColorPaletteAsync(Bitmap cover, int colorCount = 5) {
        return !CacheManager.IsValid(cover) ?
            null : // 缓存不存在直接返回null
            await ColorExtraction.GetColorPaletteFromBitmapAsync(
                                     cover,
                                     colorCount,
                                     VisualConfig.SelectedColorExtractionAlgorithm,
                                     VisualConfig.IgnoreWhite,
                                     VisualConfig.ToLab,
                                     VisualConfig.UseKMeansPp)
                                 .ConfigureAwait(false);
    }

    [RelayCommand]
    private static void OnVolumeBarPointerWheelChanged(PointerWheelEventArgs e) {
        // 阻止事件冒泡到父级元素
        e.Handled = true;

        switch (e.Delta.Y) {
            // 根据你的需求处理滚轮滚动事件
            case > 0:
                AudioPlayManager.Volume += 2;

                break;
            case < 0:
                AudioPlayManager.Volume -= 2;

                break;
        }
    }
}