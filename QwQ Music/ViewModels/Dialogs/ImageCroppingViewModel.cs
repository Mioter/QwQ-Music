using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using QwQ_Music.Common.Services;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class ImageCroppingViewModel(Bitmap sourceImage) : ObservableObject, IDialogContext
{
    public Bitmap? SourceImage { get; set; } = sourceImage;

    [ObservableProperty] public partial Bitmap? CroppedImage { get; set; }

    [ObservableProperty] public partial double AspectRatio { get; set; } = AspectRatioMaps[0].Value;

    public static AspectRatioMap[] AspectRatioMaps { get; set; } =
        [new("1:1", 1.0), new("4:3", 4.0 / 3.0), new("16:9", 16.0 / 9.0), new("自由比例", 0.0)];

    [RelayCommand]
    private async Task SaveImageButtonClick()
    {
        if (App.TopLevel == null)
            return;

        var file = await App.TopLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "保存裁剪图片",
                SuggestedFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                DefaultExtension = "png",
                FileTypeChoices =
                [
                    new FilePickerFileType("PNG图片")
                    {
                        Patterns = ["*.png"],
                    },
                    new FilePickerFileType("JPEG图片")
                    {
                        Patterns = ["*.jpg", "*.jpeg"],
                    },
                ],
            }
        );

        if (file == null)
            return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            CroppedImage?.Save(stream);
        }
        catch (Exception ex)
        {
            NotificationService.Error("坏欸", $"保存文件失败了！\n{ex.Message}");
        }
    }

    #region 接口实现

    [RelayCommand]
    private void Ok()
    {
        Close(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        Close();
    }

    public void Close(object? result)
    {
        RequestClose?.Invoke(this, result);
    }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;

    #endregion
}

public record AspectRatioMap(string Key, double Value);
