using System;
using Avalonia;
using Avalonia.Controls;

namespace QwQ_Music.Helper.Behaviors;

/// <summary>
/// 为 TextBlock 提供占位符功能的行为
/// </summary>
public class PlaceholderBehavior
{
    /// <summary>
    /// 占位符文本附加属性
    /// </summary>
    public static readonly AttachedProperty<string> PlaceholderProperty = AvaloniaProperty.RegisterAttached<
        PlaceholderBehavior,
        TextBlock,
        string
    >("Placeholder", string.Empty);

    public static string GetPlaceholder(TextBlock element)
    {
        return element.GetValue(PlaceholderProperty);
    }

    public static void SetPlaceholder(TextBlock element, string value)
    {
        element.SetValue(PlaceholderProperty, value);
    }

    static PlaceholderBehavior()
    {
        TextBlock.TextProperty.Changed.Subscribe(OnTextChanged);
    }

    private static void OnTextChanged(AvaloniaPropertyChangedEventArgs args)
    {
        if (args is not { Sender: TextBlock textBlock, NewValue: string text })
            return;

        // 只有当文本为空时才应用占位符
        if (!string.IsNullOrWhiteSpace(text))
            return;

        string placeholder = textBlock.GetValue(PlaceholderProperty);
        textBlock.SetCurrentValue(TextBlock.TextProperty, placeholder);
    }
}
