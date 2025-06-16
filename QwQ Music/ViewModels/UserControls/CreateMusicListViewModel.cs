using System;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Models;
using QwQ_Music.Services;
using QwQ_Music.Services.ConfigIO;
using QwQ_Music.ViewModels.ViewModelBases;
using QwQ_Music.Views.UserControls;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.ViewModels.UserControls;

public partial class CreateMusicListViewModel(OverlayDialogOptions options, string oldName = "")
    : DialogViewModelBase(options)
{
    [ObservableProperty]
    public partial Bitmap? Cover { get; set; }

    public string? Name
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            if (string.IsNullOrWhiteSpace(field))
            {
                ErrorText = "名称不能为空!";
                return;
            }

            // 检查歌单名称是否已存在
            if (
                DataBaseService.RecordExists(DataBaseService.Table.LISTINFO, nameof(MusicListModel.Name), field)
                && field != oldName
            )
            {
                ErrorText = "歌单名称已存在!";
                return;
            }

            ErrorText = null;
        }
    }

    [ObservableProperty]
    public partial string? Description { get; set; }

    public bool CanOk => string.IsNullOrEmpty(ErrorText);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOk))]
    public partial string? ErrorText { get; set; } = "名称不能为空!";

    [RelayCommand]
    private async Task AddCover()
    {
        var options = new OverlayDialogOptions
        {
            Title = "裁剪图片",
            Buttons = DialogButton.OKCancel,
            Mode = DialogMode.Info,
            CanDragMove = true,
            CanResize = false,
        };

        var bitmap = await OpenImageFile();

        if (bitmap != null)
        {
            var model = new ImageCroppingViewModel(bitmap);
            var result = await OverlayDialog.ShowModal<ImageCropping, ImageCroppingViewModel>(model, options: options);

            if (result == DialogResult.OK)
            {
                Cover = model.CroppedImage;
            }
        }
    }

    private static async Task<Bitmap?> OpenImageFile()
    {
        var files = await App.TopLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "选择图片",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("图片文件") { Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp"] },
                ],
            }
        );

        if (files.Count <= 0)
            return null;

        try
        {
            var file = files[0];
            await using var stream = await file.OpenReadAsync();
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            NotificationService.ShowLight(
                new Notification("坏欸", $"打开文件失败了！" + $"{ex.Message}"),
                NotificationType.Error
            );

            return null;
        }
    }

    [RelayCommand]
    private void Ok()
    {
        Close(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Close(false);
    }
}
