using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.Enums;
using QwQ_Music.ViewModels;
using QwQ_Music.ViewModels.Dialogs;
using QwQ_Music.Views.Dialogs;
using Ursa.Controls;

namespace QwQ_Music.Windows;

public partial class MainWindow : UrsaWindow {
    private bool _isClosing;
    private bool _isOpenClosingDialog;
    private object? _lastContent;

    public MainWindow() {
        InitializeComponent();
        Width = 1200;
        Height = 800;
        MusicPlayerPanel.TopPanel.PointerPressed += MusicCoverPageOnPointerPressed;
    }


    public void ShowMainWindow() {
        Show();
        Activate();
        WindowState = WindowState.Normal;
    }

    public void BackToMainContent() {
        if (_lastContent == null)
            return;

        SetValue(ContentProperty, _lastContent);
        _lastContent = null;
    }

    protected override void OnClosing(WindowClosingEventArgs e) {
        try {
            if (e.CloseReason is WindowCloseReason.ApplicationShutdown) {
                return;
            }

            if (_isClosing)
                return;

            if (e.CloseReason is WindowCloseReason.OSShutdown) {
                ApplicationViewModel.Shutdown();
                return;
            }

            e.Cancel = true;

            Dispatcher.UIThread.Invoke(HandleWindowClosingAsync);
        } catch (Exception ex) {
            LoggerService.Error($"在程序退出时发生错误 : \n {ex.Message}");
        }
    }

    private async Task HandleWindowClosingAsync() {
        ClosingBehavior behavior = ConfigManager.SystemConfig.ClosingBehavior;

        if (_isOpenClosingDialog) {
            NotificationService.Info("注意", "请不要再点啦，先选择关闭行为吧！");

            return;
        }

        _isOpenClosingDialog = true;

        if (behavior == Models.Enums.ClosingBehavior.Ask)
            behavior = await GetUserClosingBehaviorAsync().ConfigureAwait(true);

        switch (behavior) {
            case Models.Enums.ClosingBehavior.Exit:
                _isClosing = true;
                ApplicationViewModel.Shutdown();

                break;
            case Models.Enums.ClosingBehavior.HideToTray:
                Hide();

                break;
            case Models.Enums.ClosingBehavior.Ask:
            default:
                // 其它情况无需处理
                break;
        }

        _isOpenClosingDialog = false;
    }

    private static async Task<ClosingBehavior> GetUserClosingBehaviorAsync() {
        var options = new OverlayDialogOptions { Mode = DialogMode.Question, IsCloseButtonVisible = false };

        var model = new ExitConfirmViewModel();
        bool result = await OverlayDialog
                            .ShowCustomAsync<ExitConfirm, ExitConfirmViewModel, bool>(model, options: options)
                            .ConfigureAwait(false);

        if (!result)
            return Models.Enums.ClosingBehavior.Ask;

        if (model.IsEnablePrompt)
            ConfigManager.SystemConfig.ClosingBehavior = model.ClosingBehavior;

        return model.ClosingBehavior;
    }

    private void MusicCoverPageOnPointerPressed(object? sender, PointerPressedEventArgs e) {
        if (WindowState == WindowState.FullScreen)
            return;

        BeginMoveDrag(e);
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        // 使用热键服务处理按键事件
        HotkeyService.HandleKeyDown(e);
        base.OnKeyDown(e);
    }

    ~MainWindow() { MusicPlayerPanel.TopPanel.PointerPressed -= MusicCoverPageOnPointerPressed; }
}