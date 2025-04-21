using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace QwQ_Music.Controls;

/// <summary>
/// 歌词行控件
/// </summary>
public class LyricLineControl : TemplatedControl
{
    #region 依赖属性

    // 歌词文本
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<
        LyricLineControl,
        string
    >(nameof(Text), string.Empty);

    // 翻译文本
    public static readonly StyledProperty<string?> TranslationProperty = AvaloniaProperty.Register<
        LyricLineControl,
        string?
    >(nameof(Translation));

    // 是否显示翻译
    public static readonly StyledProperty<bool> ShowTranslationProperty = AvaloniaProperty.Register<
        LyricLineControl,
        bool
    >(nameof(ShowTranslation));

    // 时间点
    public static readonly StyledProperty<double> TimePointProperty = AvaloniaProperty.Register<
        LyricLineControl,
        double
    >(nameof(TimePoint));

    // 文本对齐方式
    public static readonly StyledProperty<HorizontalAlignment> TextAlignmentProperty = AvaloniaProperty.Register<
        LyricLineControl,
        HorizontalAlignment
    >(nameof(TextAlignment), HorizontalAlignment.Center);

    // 翻译间距
    public static readonly StyledProperty<double> TranslationSpacingProperty = AvaloniaProperty.Register<
        LyricLineControl,
        double
    >(nameof(TranslationSpacing), 2.0);
    
    // 歌词文本边距
    public static readonly StyledProperty<Thickness> TextMarginProperty = AvaloniaProperty.Register<
        LyricLineControl,
        Thickness
    >(nameof(TextMargin), new Thickness(10, 0, 10, 0));
    
    #endregion

    #region 属性

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Translation
    {
        get => GetValue(TranslationProperty);
        set => SetValue(TranslationProperty, value);
    }

    public bool ShowTranslation
    {
        get => GetValue(ShowTranslationProperty);
        set => SetValue(ShowTranslationProperty, value);
    }

    public double TimePoint
    {
        get => GetValue(TimePointProperty);
        set => SetValue(TimePointProperty, value);
    }

    public HorizontalAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }
    
    public Thickness TextMargin
    {
        get => GetValue(TextMarginProperty);
        set => SetValue(TextMarginProperty, value);
    }

    public double TranslationSpacing
    {
        get => GetValue(TranslationSpacingProperty);
        set => SetValue(TranslationSpacingProperty, value);
    }

    #endregion

    static LyricLineControl()
    {
        AffectsRender<LyricLineControl>(
            TextProperty,
            TranslationProperty,
            ShowTranslationProperty,
            TextAlignmentProperty,
            TextMarginProperty
        );
    }
}