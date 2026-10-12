using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using QwQ_Music.Common.Services;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class ImageCroppingViewModel(Bitmap sourceImage) : ObservableObject, IDialogContext {
    public Bitmap SourceImage { get; set; } = sourceImage;

    [ObservableProperty]
    public partial Bitmap? CroppedImage { get; set; }

    [ObservableProperty]
    public partial double AspectRatio { get; set; } = AspectRatioMaps["1:1"];

    public static OrderedDictionary<string, double> AspectRatioMaps { get; set; } = new() {
        ["1:1"] = 1.0d, ["4:3"] = 4.0d / 3.0d, ["16:9"] = 16.0d / 9.0d, ["Free"] = 0d
    };

    [RelayCommand]
    private void SaveImageButtonClick() {
        if (App.TopLevel == null)
            return;

        App.TopLevel.StorageProvider.SaveFilePickerAsync(
               new FilePickerSaveOptions {
                   Title = "保存裁剪图片",
                   SuggestedFileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                   DefaultExtension = "png",
                   FileTypeChoices = [
                       new FilePickerFileType("PNG图片") { Patterns = ["*.png"] },
                       new FilePickerFileType("JPEG图片") { Patterns = ["*.jpg", "*.jpeg"] }
                   ]
               })
           .ContinueWith(task => {
               if (task is not { IsCompletedSuccessfully: true, Result: { } file })
                   return;
               file.OpenWriteAsync()
                   .ContinueWith(read => {
                       if (read is { IsCompletedSuccessfully: true, Result: { } stream })
                           CroppedImage?.Save(stream);
                       else
                           NotificationService.Error("坏欸", "保存文件失败了！");
                   });
           })
           .ContinueWith(LoggerService.HandleException)
           .ConfigureAwait(false);
    }

    #region 接口实现

    [RelayCommand]
    private void Ok() { Close(CroppedImage); }

    [RelayCommand]
    private void Cancel() { Close(); }

    public void Close(object? result) { RequestClose?.Invoke(this, result); }

    public void Close() { RequestClose?.Invoke(this, null); }

    public event EventHandler<object?>? RequestClose;

    #endregion
}