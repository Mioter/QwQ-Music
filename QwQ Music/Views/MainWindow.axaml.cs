using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.ViewModels;
using QwQ_Music.ViewModels.UserControls;
using QwQ_Music.Views.UserControls;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.Views;

public partial class MainWindow : Window
{
    private readonly HotkeyService _hotkeyService;
    private bool _isClosing;
    private bool _isOpenClosingDialog;

    public MainWindow()
    {
        InitializeComponent();

        // 修改窗口关闭事件处理
        Closing += OnClosing;
        Closed += OnClosed;

        // 初始化热键服务
        _hotkeyService = new HotkeyService(MusicPlayerViewModel.Instance);

        // 注册按键事件
        KeyDown += MainWindow_KeyDown;

        Width = 1200;
        Height = 800;

        DataContext = new MainWindowViewModel();
        MusicCoverPagePanel.PointerPressed += MusicCoverPagePanelOnPointerPressed;
    }

    public void ShowMainWindow()
    {
        Show();
        Activate();
        WindowState = WindowState.Normal;
    }

    public void CloseMainWindow()
    {
        _isClosing = true;
        Close();
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            if (_isClosing)
                return;

            e.Cancel = true;

            switch (ConfigInfoModel.SystemConfig.ClosingBehavior)
            {
                case Definitions.Enums.ClosingBehavior.AskAbout:
                    if (_isOpenClosingDialog)
                    {
                        NotificationService.ShowLight(
                            new Notification(
                                "注意",
                                "请不要再点啦，先选择关闭行为吧！"
                            ),
                            NotificationType.Information,
                            showClose: true
                        );
                        break;
                    }
                    await ShowClosingDialog();
                    break;

                case Definitions.Enums.ClosingBehavior.Exit:
                    await ApplicationViewModel.ExitApplication();
                    break;

                case Definitions.Enums.ClosingBehavior.HideToTray:
                    Hide();
                    break;
                default:
                    await ApplicationViewModel.ExitApplication();
                    break;
            }
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"在程序退出时发生错误 : \n {ex.Message}");
        }
    }

    private async Task ShowClosingDialog()
    {
        var options = new OverlayDialogOptions
        {
            Title = "退出提示",
            Mode = DialogMode.Question,
            CanDragMove = true,
            CanResize = false,
        };

        var model = new ProgramExitConfirmViewModel(options);

        _isOpenClosingDialog = true;
        await OverlayDialog.ShowCustomModal<ProgramExitConfirm, ProgramExitConfirmViewModel, object>(
            model,
            options: options
        );

        if (model.IsCancel)
        {
            _isOpenClosingDialog  = false;
            return;
        }

        if (model.IsEnablePrompt)
        {
            ConfigInfoModel.SystemConfig.ClosingBehavior = model.ClosingBehavior;
        }

        switch (model.ClosingBehavior)
        {
            case Definitions.Enums.ClosingBehavior.Exit:
                await ApplicationViewModel.ExitApplication();
                break;
            case Definitions.Enums.ClosingBehavior.HideToTray:
                Hide();
                _isOpenClosingDialog  = false;
                break;
            case Definitions.Enums.ClosingBehavior.AskAbout:
            default:
                _isOpenClosingDialog  = false;
                return;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closing -= OnClosing;
        Closed -= OnClosed;
        MusicCoverPagePanel.PointerPressed -= MusicCoverPagePanelOnPointerPressed;
        KeyDown -= MainWindow_KeyDown;
    }

    private void MusicCoverPagePanelOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        // 使用热键服务处理按键事件
        _hotkeyService.HandleKeyDown(e);
    }
}
