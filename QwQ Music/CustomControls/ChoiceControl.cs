using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;

namespace QwQ_Music.CustomControls;

public class ChoiceControl : Panel
{
    // 定义SelectName附加属性
    public static readonly AttachedProperty<object> SelectNameProperty =
        AvaloniaProperty.RegisterAttached<ChoiceControl, AvaloniaObject, object>("SelectName");

    // 定义Selected属性
    public static readonly StyledProperty<object> SelectedProperty =
        AvaloniaProperty.Register<ChoiceControl, object>(nameof(Selected));

    private readonly Dictionary<AvaloniaObject, IDisposable> _subscriptions = new();

    public ChoiceControl()
    {
        Children.CollectionChanged += OnChildrenCollectionChanged;
        this.GetObservable(SelectedProperty).Subscribe(_ => UpdateChildrenVisibility());
    }

    public object Selected
    {
        get => GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    public static object GetSelectName(AvaloniaObject obj)
    {
        return obj.GetValue(SelectNameProperty);
    }

    public static void SetSelectName(AvaloniaObject obj, object value)
    {
        obj.SetValue(SelectNameProperty, value);
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
        object selected = Selected;
        foreach (var child in Children)
        {
            if (child is not AvaloniaObject avaloniaChild) continue;

            object name = GetSelectName(avaloniaChild);
            child.IsVisible = Equals(name, selected); // 关键修改
        }
    }
}
