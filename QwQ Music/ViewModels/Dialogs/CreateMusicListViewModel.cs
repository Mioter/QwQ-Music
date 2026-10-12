using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using QwQ_Music.Common.Managers;
using QwQ_Music.Common.Services;
using QwQ_Music.Common.Services.Databases;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;
using QwQ_Music.Views.Dialogs;
using Ursa.Controls;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class CreateMusicListViewModel : DataVerifyModelBase, IDialogContext {
    public CreateMusicListViewModel(string name) {
        Name = name;
        InitialValidate();
    }


    [ObservableProperty]
    public partial Bitmap Cover { get; set; } = CacheManager.Loading;

    public string Name {
        get;
        set {
            if (!ValidateName(value))
                return;
            field = value;
        }
    }

    [ObservableProperty]
    public partial string? Description { get; set; }

    [RelayCommand]
    private void SetCover() {
        if (App.TopLevel == null)
            return;

        var options = new OverlayDialogOptions { Title = "图片裁剪" };

        FileOperationService.OpenImageFile(App.TopLevel)
                            .ContinueWith(task => {
                                if (task is not { IsCompletedSuccessfully: true, Result: { } bitmap })
                                    return;
                                OverlayDialog.ShowCustomAsync<ImageCropping, ImageCroppingViewModel, Bitmap>(
                                                 new ImageCroppingViewModel(bitmap),
                                                 options: options)
                                             .ContinueWith(dialog => {
                                                 if (dialog is { IsCompletedSuccessfully: true, Result: { } result })
                                                     Cover = result;
                                             })
                                             .ConfigureAwait(false);
                            })
                            .ContinueWith(LoggerService.HandleException)
                            .ConfigureAwait(false);
    }

    public MusicListModel CreateMusicListModel() {
        var model = MusicListModel.Create(Name, Cover);

        if (Description != null)
            model.Description = Description;

        return model;
    }

    #region 数据校验

    private bool ValidateName(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            SetErrors(nameof(Name), "名称不可以为空");
            return false;
        }

        if (MusicListRepository.Instance.ExistsAsync((value, "_QWQ_LOCAL_USER"))
                               .ConfigureAwait(false)
                               .GetAwaiter()
                               .GetResult()) {
            SetErrors(nameof(Name), "该名称已存在");
            return false;
        }

        ClearErrors(nameof(Name));
        return true;
    }

    private void InitialValidate() { ValidateName(Name); }

    #endregion

    #region 接口实现

    [RelayCommand]
    private void Ok() { Close(CreateMusicListModel()); }

    [RelayCommand]
    private void Cancel() { Close(); }

    public void Close(object? result) { RequestClose?.Invoke(this, result); }

    public void Close() { RequestClose?.Invoke(this, null); }

    public event EventHandler<object?>? RequestClose;

    #endregion
}