using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using QwQ_Music.Common.Services;
using QwQ_Music.ViewModels.Windows;

namespace QwQ_Music.Windows;

public partial class DesktopPlayControlWindow : Window {
    public static readonly StyledProperty<bool> StartMovingOutProperty =
        AvaloniaProperty.Register<DesktopPlayControlWindow, bool>(nameof(StartMovingOut));

    public static readonly StyledProperty<TimeSpan> RemoveAnimationDurationProperty =
        AvaloniaProperty.Register<DesktopPlayControlWindow, TimeSpan>(nameof(RemoveAnimationDuration));

    public DesktopPlayControlWindow() {
        InitializeComponent();
        DataContext = new DesktopPlayControlWindowViewModel();
    }

    public bool StartMovingOut {
        get => GetValue(StartMovingOutProperty);
        set => SetValue(StartMovingOutProperty, value);
    }

    public TimeSpan RemoveAnimationDuration {
        get => GetValue(RemoveAnimationDurationProperty);
        set => SetValue(RemoveAnimationDurationProperty, value);
    }

    // 监听 StartMovingOut 属性变化
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
        base.OnPropertyChanged(change);

        if (change.Property == IsVisibleProperty) {
            object? newValue = change.NewValue;

            if (newValue is true)
                StartMovingOut = false;
        }

        if (change.Property != StartMovingOutProperty)
            return;

        if (change.NewValue is true)
            BeginFadeOut();
    }

    private void BeginFadeOut() {
        // 等待指定的动画时长后，在 UI线程上隐藏窗口
        Task.Delay(RemoveAnimationDuration)
            .ContinueWith(_ => Dispatcher.UIThread.Post(Hide))
            .ContinueWith(LoggerService.HandleException)
            .ConfigureAwait(false);
    }
}