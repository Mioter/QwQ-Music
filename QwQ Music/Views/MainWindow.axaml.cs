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

namespace QwQ_Music.Views;

public partial class MainWindow : Window
{
    private bool _isClosing;
    private bool _isOpenClosingDialog;

    public MainWindow()
    {
        InitializeComponent();

        Width = 1200;
        Height = 800;

        DataContext = MainWindowViewModel.Instance;

        MusicCoverPage.TopPanel.PointerPressed += MusicCoverPageOnPointerPressed;
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
        
        MusicCoverPage.TopPanel.PointerPressed -= MusicCoverPageOnPointerPressed;
        Close();
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            if (_isClosing)
                return;

            e.Cancel = true;

            switch (ConfigManager.SystemConfig.ClosingBehavior)
            {
                case Definitions.Enums.ClosingBehavior.AskAbout:
                    if (_isOpenClosingDialog)
                    {
                        NotificationService.ShowLight(
                            "注意",
                            "请不要再点啦，先选择关闭行为吧！",
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
            
            base.OnClosing(e);
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
            _isOpenClosingDialog = false;
            return;
        }

        if (model.IsEnablePrompt)
        {
            ConfigManager.SystemConfig.ClosingBehavior = model.ClosingBehavior;
        }

        switch (model.ClosingBehavior)
        {
            case Definitions.Enums.ClosingBehavior.Exit:
                await ApplicationViewModel.ExitApplication();
                break;
            case Definitions.Enums.ClosingBehavior.HideToTray:
                Hide();
                _isOpenClosingDialog = false;
                break;
            case Definitions.Enums.ClosingBehavior.AskAbout:
            default:
                _isOpenClosingDialog = false;
                return;
        }
    }

    private void MusicCoverPageOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (WindowState == WindowState.FullScreen)
            return;

        BeginMoveDrag(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // 使用热键服务处理按键事件
        HotkeyService.HandleKeyDown(e);
        base.OnKeyDown(e);
    }
}
