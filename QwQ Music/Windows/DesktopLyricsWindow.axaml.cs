using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Utilities;
using QwQ_Music.Models.ConfigModels;

namespace QwQ_Music.Windows;

public partial class DesktopLyricsWindow : Window {
    private IPageTransition _fade = UpdateFade(LyricConfig.CrossFadeTime);

    private static IPageTransition UpdateFade(TimeSpan time) {
        if (ConfigManager.LyricConfig.DesktopLyric.IsDoubleLine)
            return new PageSlide(time,PageSlide.SlideAxis.Vertical) {FillMode = FillMode.Forward};
        return new CrossFade(time);
    }

    public void UpdateFades() {
        if (LyricConfig.CrossFadeTime.Ticks == 0) {
            LyricsContainer.PageTransition = null;
            return;
        }
        
        _fade = UpdateFade(LyricConfig.CrossFadeTime);
        LyricsContainer.PageTransition = _fade;
    }

    public DesktopLyricsWindow() {
        InitializeComponent();
        Position = LyricConfig.Position;
        // LyricsContainer.PageTransition = _fade;
        PositionChanged += Window_OnPositionChanged;
        Closed += OnClosed;
    }

    public static DesktopLyricConfig LyricConfig => ConfigManager.LyricConfig.DesktopLyric;

    protected override void OnPointerReleased(PointerReleasedEventArgs e) {
        SizeToContent = SizeToContent.Height;
        base.OnPointerReleased(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e) {
        BeginMoveDrag(e);
        base.OnPointerPressed(e);
    }

    private void OnClosed(object? sender, EventArgs e) {
        Closed -= OnClosed;
        PositionChanged -= Window_OnPositionChanged;
    }

    public override void Show() {
        base.Show();
        SetPenetrate(LyricConfig.IsAnchored);
    }

    private void Window_OnPositionChanged(object? sender, PixelPointEventArgs e) { LyricConfig.Position = Position; }

    public void SetPenetrate(bool enabled) {
        if (TryGetPlatformHandle() is { } handle)
            MousePenetrate.SetPenetrate(handle.Handle, enabled);
    }
}