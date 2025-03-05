using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;

namespace QwQ_Music.Behaviors;

public class ScrollToItemBehavior
{
    public static readonly AttachedProperty<object> ScrollToItemProperty = AvaloniaProperty.RegisterAttached<
        ScrollToItemBehavior,
        Control,
        object
    >("ScrollToItem", null!, false, BindingMode.TwoWay);

    static ScrollToItemBehavior()
    {
        ScrollToItemProperty.Changed.Subscribe(args =>
        {
            switch (args.Sender)
            {
                case DataGrid dataGrid:
                {
                    object item = args.NewValue.Value;
                    Dispatcher.UIThread.Post(() => dataGrid.ScrollIntoView(item, null));
                    break;
                }
                case ListBox listBox:
                {
                    object item = args.NewValue.Value;
                    Dispatcher.UIThread.Post(() => listBox.ScrollIntoView(item));
                    break;
                }
            }
        });
    }

    public static void SetScrollToItem(Control element, object value)
    {
        element.SetValue(ScrollToItemProperty, value);
    }

    public static object GetScrollToItem(Control element)
    {
        return element.GetValue(ScrollToItemProperty);
    }
}
