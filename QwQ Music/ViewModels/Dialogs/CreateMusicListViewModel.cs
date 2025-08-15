using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using QwQ_Music.Common.Services;
using QwQ_Music.Models;
using QwQ_Music.ViewModels.Bases;
using QwQ_Music.Views;
using QwQ_Music.Views.Dialogs;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class CreateMusicListViewModel
    : DialogViewModelBase, IDialogContext
{
    public CreateMusicListViewModel(string title)
    {
        Title = title;
        InitialValidate();
    }

    public string Title { get; set; }

    [ObservableProperty] public partial Bitmap? Cover { get; set; }

    public string? Name
    {
        get;
        set
        {
            field = value;
            ValidateName(value);
        }
    }

    [ObservableProperty] public partial string? Description { get; set; }

    [RelayCommand]
    private async Task SetCover()
    {
        if (App.TopLevel == null)
            return;

        var options = new ShowWindowOptions
        {
            Title = "裁剪图片",
            IsRestoreButtonVisible = false,
            IsFullScreenButtonVisible = false,
        };

        var bitmap = await FileOperationService.OpenImageFile(App.TopLevel);

        if (bitmap == null)
            return;

        var model = new ImageCroppingViewModel(bitmap);
        bool result = await WindowBox.ShowDialog<ImageCropping, bool>(model, options, App.TopLevel);

        if (!result)
            return;

        Cover = model.CroppedImage;
    }

    public MusicListModel CreateMusicListModel()
    {
        var model = new MusicListModel
        {
            IdStr = Guid.NewGuid().ToString(),
            CoverImage = Cover,
        };

        if (Name != null)
        {
            model.Name = Name;
        }

        if (Description != null)
        {
            model.Description = Description;
        }

        return model;
    }

    #region 数据校验

    private void ValidateName(string? value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
            errors.Add("名称不可以为空");

        SetErrors(nameof(Name), errors);
    }

    private void InitialValidate()
    {
        ValidateName(Name);
    }

    #endregion

    #region 接口实现

    [RelayCommand]
    private void Ok()
    {
        Close(CreateMusicListModel());
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
