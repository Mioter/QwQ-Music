using System;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QwQ_Music.Services;
using QwQ_Music.ViewModels.ViewModelBases;
using Notification = Ursa.Controls.Notification;

namespace QwQ_Music.ViewModels.UserControls;

public partial class ImageCroppingViewModel(Bitmap sourceImage) : ViewModelBase
{
    public Bitmap? SourceImage { get; set; } = sourceImage;

    public double AspectRatio => SelectedItem.Value;

    [ObservableProperty]
    public partial Bitmap? CroppedImage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AspectRatio))]
    public partial AspectRatioMap SelectedItem { get; set; } = AspectRatioMaps[0];

    public static AspectRatioMap[] AspectRatioMaps { get; set; } =
        [new("1:1", 1.0), new("4:3", 4.0 / 3.0), new("16:9", 16.0 / 9.0), new("自由比例", 0.0)];

    [RelayCommand]
    private async Task SaveImageButtonClick()
    {
        var file = await App.TopLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "保存裁剪图片",
                SuggestedFileName = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                DefaultExtension = "png",
                FileTypeChoices =
                [
                    new FilePickerFileType("PNG图片") { Patterns = ["*.png"] },
                    new FilePickerFileType("JPEG图片") { Patterns = ["*.jpg", "*.jpeg"] },
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
            NotificationService.ShowLight(
                new Notification("坏欸", $"保存文件失败了！\n" + $"{ex.Message}"),
                NotificationType.Error
            );
        }
    }
}

public record AspectRatioMap(string Key, double Value);
