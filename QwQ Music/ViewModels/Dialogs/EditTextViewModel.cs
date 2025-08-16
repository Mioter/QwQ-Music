using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using QwQ_Music.ViewModels.Bases;

namespace QwQ_Music.ViewModels.Dialogs;

public partial class EditTextViewModel : DataVerifyModelBase, IDialogContext
{
    public EditTextViewModel(string oldText, string title = "标题未设置", int maxLength = -1)
    {
        OldText = oldText;
        Title = title;
        _maxLength = maxLength;

        InitialValidate();
    }

    private readonly int _maxLength;

    public string Title { get; }

    public string OldText { get; }

    public string? NewText
    {
        get;
        set
        {
            field = value;
            ValidateName(value);
        }
    }

    #region 数据校验

    private void ValidateName(string? value)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("新文本不可以为空");
        }
        else if (NewText == OldText)
        {
            errors.Add("新文本不能与原内容相同！");
        }
        else if (_maxLength > 0 && NewText is { } s && s.Length > _maxLength)
        {
            errors.Add("文本长度不能大于64！");
        }

        SetErrors(nameof(NewText), errors);
    }

    private void InitialValidate()
    {
        ValidateName(NewText);
    }

    #endregion

    #region 接口实现

    [RelayCommand]
    private void Ok()
    {
        Close(NewText);
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
