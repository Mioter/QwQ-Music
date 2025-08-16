using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QwQ_Music.ViewModels.Bases;

/// <summary>
///     基础数据验证模型，提供数据验证支持。
///     继承自 <see cref="ObservableObject" /> 并实现 <see cref="INotifyDataErrorInfo" />。
/// </summary>
public abstract class DataVerifyModelBase : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    /// <summary>
    ///     错误变更事件。
    /// </summary>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <summary>
    ///     指示当前 ViewModel 是否包含任何验证错误。
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    ///     获取指定属性的验证错误。
    /// </summary>
    /// <param name="propertyName">属性名称，null 或空字符串返回空集合。</param>
    /// <returns>该属性的所有错误信息集合。</returns>
    public IEnumerable GetErrors(string? propertyName)
    {
        return string.IsNullOrEmpty(propertyName) || !_errors.TryGetValue(propertyName, out var errorList)
            ? Enumerable.Empty<string>()
            : errorList.ToList(); // 返回副本，防止外部修改
    }

    /// <summary>
    ///     设置指定属性的验证错误集合。
    /// </summary>
    /// <param name="propertyName">属性名称。</param>
    /// <param name="errors">错误信息集合。</param>
    protected void SetErrors(string propertyName, IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (string.IsNullOrEmpty(propertyName))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

        var errorList = errors.ToList();

        if (errorList.Count > 0)
        {
            _errors[propertyName] = errorList;
        }
        else
        {
            _errors.Remove(propertyName);
        }

        RaiseErrorsChanged(propertyName);
    }

    /// <summary>
    ///     设置指定属性的验证错误（params 参数形式）。
    /// </summary>
    /// <param name="propertyName">属性名称。</param>
    /// <param name="errors">错误信息列表。</param>
    protected void SetErrors(string propertyName, params string[] errors)
    {
        SetErrors(propertyName, (IEnumerable<string>)errors);
    }

    /// <summary>
    ///     为指定属性添加单个验证错误。
    ///     若错误为空白字符串，则忽略。
    /// </summary>
    /// <param name="propertyName">属性名称。</param>
    /// <param name="error">错误信息。</param>
    protected void AddError(string propertyName, string error)
    {
        if (string.IsNullOrEmpty(propertyName))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

        if (string.IsNullOrWhiteSpace(error)) return;

        if (!_errors.ContainsKey(propertyName))
            _errors[propertyName] = [];

        if (!_errors[propertyName].Contains(error)) // 避免重复添加相同错误
            _errors[propertyName].Add(error);

        RaiseErrorsChanged(propertyName);
    }

    /// <summary>
    ///     设置字段值并使用指定的 ValidationAttribute 进行验证。
    ///     若验证失败，将错误信息添加到该属性的错误集合中。
    /// </summary>
    /// <typeparam name="T">属性类型</typeparam>
    /// <param name="field">字段引用（backing field）</param>
    /// <param name="newValue">新值</param>
    /// <param name="propertyName">属性名称（自动填充）</param>
    /// <param name="validationAttributes">一个或多个验证特性</param>
    /// <returns>如果值已更改则返回 true；否则返回 false</returns>
    protected bool SetPropertyWithValidation<T>(
        [NotNullIfNotNull(nameof(newValue))] ref T field,
        T newValue,
        ValidationAttribute[] validationAttributes,
        [CallerMemberName] string? propertyName = null
        )
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }

        OnPropertyChanging(propertyName);

        field = newValue;

        OnPropertyChanged(propertyName);

        if (propertyName == null)
            return true;

        foreach (var attribute in validationAttributes)
        {
            if (attribute.IsValid(newValue))
                continue;

            AddError(propertyName, attribute.ErrorMessage ?? $"{nameof(attribute)}未设置错误消息！！！");
        }

        return true;
    }

    /// <summary>
    ///     清除指定属性的所有验证错误。
    /// </summary>
    /// <param name="propertyName">属性名称。</param>
    protected void ClearErrors(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || !_errors.Remove(propertyName)) return;

        RaiseErrorsChanged(propertyName);
    }

    /// <summary>
    ///     清除所有属性的验证错误。
    /// </summary>
    protected void ClearAllErrors()
    {
        var affectedProperties = _errors.Keys.ToList();
        _errors.Clear();

        foreach (string prop in affectedProperties)
        {
            RaiseErrorsChanged(prop);
        }
    }

    /// <summary>
    ///     检查指定属性是否具有验证错误。
    /// </summary>
    /// <param name="propertyName">属性名称。</param>
    /// <returns>是否存在错误。</returns>
    protected bool HasErrorFor(string propertyName)
    {
        return !string.IsNullOrEmpty(propertyName) &&
            _errors.TryGetValue(propertyName, out var list) &&
            list.Count > 0;
    }

    /// <summary>
    ///     触发 ErrorsChanged 事件并更新 HasErrors 状态。
    /// </summary>
    /// <param name="propertyName">发生错误变更的属性名。</param>
    private void RaiseErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
    }
}
