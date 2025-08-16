using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using QwQ_Music.Common.Manager;
using QwQ_Music.Common.Services;
using QwQ_Music.Models.Enums;
using QwQ_Music.ViewModels;
using QwQ_Music.ViewModels.Dialogs;
using QwQ_Music.Views.Dialogs;
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

        MusicPlayerPanel.TopPanel.PointerPressed += MusicCoverPageOnPointerPressed;
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

        MusicPlayerPanel.TopPanel.PointerPressed -= MusicCoverPageOnPointerPressed;
        Close();
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            base.OnClosing(e);

            if (_isClosing)
                return;

            e.Cancel = true;

            await HandleWindowClosingAsync();
        }
        catch (Exception ex)
        {
            await LoggerService.ErrorAsync($"在程序退出时发生错误 : \n {ex.Message}");
        }
    }

    private async Task HandleWindowClosingAsync()
    {
        var behavior = ConfigManager.SystemConfig.ClosingBehavior;

        if (_isOpenClosingDialog)
        {
            NotificationService.Info("注意", "请不要再点啦，先选择关闭行为吧！");

            return;
        }

        _isOpenClosingDialog = true;

        if (behavior == Models.Enums.ClosingBehavior.AskAbout)
        {
            behavior = await GetUserClosingBehaviorAsync();
        }

        switch (behavior)
        {
            case Models.Enums.ClosingBehavior.Exit:
                _isClosing = true;
                await ApplicationViewModel.ExitApplication();
                break;
            case Models.Enums.ClosingBehavior.HideToTray:
                Hide();

                break;
            case Models.Enums.ClosingBehavior.AskAbout:
            default:
                // 其它情况无需处理
                break;
        }

        _isOpenClosingDialog = false;
    }

    private static async Task<ClosingBehavior> GetUserClosingBehaviorAsync()
    {
        var options = new OverlayDialogOptions
        {
            Mode = DialogMode.Question,
            CanDragMove = true,
            CanResize = false,
        };

        var model = new ExitConfirmViewModel();
        bool result = await OverlayDialog.ShowCustomModal<ExitConfirm, ExitConfirmViewModel, bool>(model, options: options);

        if (!result)
            return Models.Enums.ClosingBehavior.AskAbout;

        if (model.IsEnablePrompt)
            ConfigManager.SystemConfig.ClosingBehavior = model.ClosingBehavior;

        return model.ClosingBehavior;
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
