using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace QwQ_Music.CustomControls;

public class ChoiceControl : ItemsControl
{
    // 定义 SelectName 附加属性
    public static readonly AttachedProperty<object> SelectNameProperty =
        AvaloniaProperty.RegisterAttached<ChoiceControl, AvaloniaObject, object>("SelectName");

    // 定义 Selected 属性
    public static readonly StyledProperty<object?> SelectedProperty =
        AvaloniaProperty.Register<ChoiceControl, object?>(nameof(Selected));

    // 定义 TargetType 属性
    public static readonly StyledProperty<Type?> TargetTypeProperty =
        AvaloniaProperty.Register<ChoiceControl, Type?>(nameof(TargetType));

    // 定义 PageTransition 属性
    public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
        AvaloniaProperty.Register<ChoiceControl, IPageTransition?>(nameof(PageTransition));

    private TransitioningContentControl? _transitioningContent;
    private readonly Dictionary<AvaloniaObject, IDisposable> _subscriptions = new();

    public ChoiceControl()
    {
        Items.CollectionChanged += OnChildrenCollectionChanged;
        this.GetObservable(SelectedProperty).Subscribe(_ => UpdateChildrenVisibility());
    }

    public object? Selected
    {
        get => GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    public Type? TargetType
    {
        get => GetValue(TargetTypeProperty);
        set => SetValue(TargetTypeProperty, value);
    }

    public IPageTransition? PageTransition
    {
        get => GetValue(PageTransitionProperty);
        set => SetValue(PageTransitionProperty, value);
    }

    public static object GetSelectName(AvaloniaObject obj)
    {
        return obj.GetValue(SelectNameProperty);
    }

    public static void SetSelectName(AvaloniaObject obj, object value)
    {
        obj.SetValue(SelectNameProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // 查找模板中的 TransitioningContentControl
        _transitioningContent = e.NameScope.Find<TransitioningContentControl>("PART_TransitioningContent");

        if (_transitioningContent != null)
        {
            // 绑定 PageTransition 属性
            _transitioningContent.Bind(TransitioningContentControl.PageTransitionProperty,
                this.GetObservable(PageTransitionProperty));
        }

        UpdateChildrenVisibility();
    }

    private void OnChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 处理新增子项
        if (e.NewItems != null)
        {
            foreach (object? item in e.NewItems)
            {
                if (item is not AvaloniaObject avaloniaObj) continue;

                var subscription = avaloniaObj.GetObservable(SelectNameProperty)
                    .Subscribe(_ => UpdateChildrenVisibility());
                _subscriptions[avaloniaObj] = subscription;
            }
        }

        // 处理移除子项
        if (e.OldItems != null)
        {
            foreach (object? item in e.OldItems)
            {
                if (item is not AvaloniaObject avaloniaObj || !_subscriptions.TryGetValue(avaloniaObj, out var sub)) continue;

                sub.Dispose();
                _subscriptions.Remove(avaloniaObj);
            }
        }

        UpdateChildrenVisibility();
    }

    private void UpdateChildrenVisibility()
    {
        if (_transitioningContent == null) return;

        object? selected = ConvertValueToTargetType(Selected);

        // 找到与 Selected 匹配的子项
        foreach (object? item in Items)
        {
            if (item is not AvaloniaObject avaloniaChild) continue;

            object? name = ConvertValueToTargetType(GetSelectName(avaloniaChild));
            if (!Equals(name, selected)) continue;
            // 更新 TransitioningContentControl 的内容
            _transitioningContent.Content = item;
            return;
        }

        // 如果没有匹配项，则清空内容
        _transitioningContent.Content = null;
    }

    private object? ConvertValueToTargetType(object? value)
    {
        if (TargetType == null) return value;

        try
        {
            // 尝试将值转换为目标类型
            if (value is string stringValue)
            {
                return TypeDescriptor.GetConverter(TargetType).ConvertFrom(stringValue);
            }
            if (value != null) 
                return TargetType.IsInstanceOfType(value) ? value : TypeDescriptor.GetConverter(TargetType).ConvertFrom(value);
            return value;
        }
        catch
        {
            return value; // 转换失败时返回原始值
        }
    }
}