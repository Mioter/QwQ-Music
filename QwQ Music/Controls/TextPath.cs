using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace QwQ_Music.Controls;

/// <summary>
/// 将文本转换为几何图形路径的控件
/// </summary>
public class TextPath : Control
{
    private Geometry? _textGeometry;

    #region 依赖属性

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextBlock.FontFamilyProperty.AddOwner<TextPath>();

    public static readonly StyledProperty<double> FontSizeProperty = AvaloniaProperty.Register<TextPath, double>(
        nameof(FontSize),
        defaultValue: 12.0
    );

    public static readonly StyledProperty<FontStretch> FontStretchProperty =
        TextBlock.FontStretchProperty.AddOwner<TextPath>();

    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        TextBlock.FontStyleProperty.AddOwner<TextPath>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextBlock.FontWeightProperty.AddOwner<TextPath>();

    public static readonly StyledProperty<Point> OriginProperty = AvaloniaProperty.Register<TextPath, Point>(
        nameof(Origin)
    );

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<TextPath, string>(
        nameof(Text),
        string.Empty
    );

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextBlock.ForegroundProperty.AddOwner<TextPath>();

    public static readonly StyledProperty<IBrush?> StrokeProperty = AvaloniaProperty.Register<TextPath, IBrush?>(
        nameof(Stroke)
    );

    public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<TextPath, double>(
        nameof(StrokeThickness),
        1.0
    );

    public static readonly StyledProperty<double> LetterSpacingProperty = AvaloniaProperty.Register<TextPath, double>(
        nameof(LetterSpacing)
    );

    public static readonly StyledProperty<string?> PlaceholderProperty = AvaloniaProperty.Register<TextPath, string?>(
        nameof(Placeholder)
    );

    #endregion

    #region 属性

    [Category("Appearance")]
    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    [Category("Appearance")]
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    [Category("Appearance")]
    public FontStretch FontStretch
    {
        get => GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    [Category("Appearance")]
    public FontStyle FontStyle
    {
        get => GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    [Category("Appearance")]
    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    [Category("Appearance")]
    public Point Origin
    {
        get => GetValue(OriginProperty);
        set => SetValue(OriginProperty, value);
    }

    [Category("Appearance")]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    [Category("Appearance")]
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    [Category("Appearance")]
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    [Category("Appearance")]
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    [Category("Appearance")]
    public double LetterSpacing
    {
        get => GetValue(LetterSpacingProperty);
        set => SetValue(LetterSpacingProperty, value);
    }

    [Category("Appearance")]
    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    #endregion

    static TextPath()
    {
        AffectsRender<TextPath>(
            FontFamilyProperty,
            FontSizeProperty,
            FontStretchProperty,
            FontStyleProperty,
            FontWeightProperty,
            OriginProperty,
            TextProperty,
            ForegroundProperty,
            StrokeProperty,
            StrokeThicknessProperty,
            LetterSpacingProperty,
            PlaceholderProperty
        );

        AffectsMeasure<TextPath>(
            FontFamilyProperty,
            FontSizeProperty,
            FontStretchProperty,
            FontStyleProperty,
            FontWeightProperty,
            OriginProperty,
            TextProperty,
            StrokeThicknessProperty,
            LetterSpacingProperty,
            PlaceholderProperty
        );

        // 添加 FontSize 属性变化时的处理
        FontSizeProperty.Changed.AddClassHandler<TextPath>(
            (x, _) =>
            {
                x._textGeometry = null; // 强制重新创建几何图形
                x.InvalidateVisual();
            }
        );

        // 当其他影响几何图形的属性改变时，也需要重新创建几何图形
        FontFamilyProperty.Changed.AddClassHandler<TextPath>(
            (x, _) =>
            {
                x._textGeometry = null;
                x.InvalidateVisual();
            }
        );
        FontStretchProperty.Changed.AddClassHandler<TextPath>(
            (x, _) =>
            {
                x._textGeometry = null;
                x.InvalidateVisual();
            }
        );
        FontStyleProperty.Changed.AddClassHandler<TextPath>(
            (x, _) =>
            {
                x._textGeometry = null;
                x.InvalidateVisual();
            }
        );
        FontWeightProperty.Changed.AddClassHandler<TextPath>(
            (x, _) =>
            {
                x._textGeometry = null;
                x.InvalidateVisual();
            }
        );
        OriginProperty.Changed.AddClassHandler<TextPath>(
            (x, _) =>
            {
                x._textGeometry = null;
                x.InvalidateVisual();
            }
        );
        TextProperty.Changed.AddClassHandler<TextPath>(
            (x, _) =>
            {
                x._textGeometry = null;
                x.InvalidateVisual();
            }
        );
        LetterSpacingProperty.Changed.AddClassHandler<TextPath>(
            (x, _) =>
            {
                x._textGeometry = null;
                x.InvalidateVisual();
            }
        );
        PlaceholderProperty.Changed.AddClassHandler<TextPath>(
            (x, _) =>
            {
                x._textGeometry = null;
                x.InvalidateVisual();
            }
        );
        StrokeThicknessProperty.Changed.AddClassHandler<TextPath>(
            (x, _) =>
            {
                x._textGeometry = null;
                x.InvalidateVisual();
            }
        );
    }

    public override void Render(DrawingContext context)
    {
        if (_textGeometry == null)
        {
            CreateTextGeometry();
        }

        if (_textGeometry == null)
            return;

        // 先绘制描边，使用更平滑的描边效果
        if (Stroke != null && StrokeThickness > 0)
        {
            var pen = new Pen(Stroke, StrokeThickness)
            {
                LineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                MiterLimit = 1.0,
            };
            context.DrawGeometry(Stroke, pen, _textGeometry);
        }
        // 再绘制填充
        if (Foreground != null)
        {
            context.DrawGeometry(Foreground, null, _textGeometry);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_textGeometry == null)
        {
            CreateTextGeometry();
        }

        if (_textGeometry?.Bounds == null || _textGeometry.Bounds.Width == 0 || _textGeometry.Bounds.Height == 0)
        {
            return new Size(0, 0);
        }

        // 考虑描边宽度的影响
        double strokeOffset = Stroke != null && StrokeThickness > 0 ? StrokeThickness : 0;
        var bounds = _textGeometry.Bounds;
        double width = bounds.Width + strokeOffset * 2;
        double height = bounds.Height + strokeOffset * 2;

        return new Size(Math.Min(availableSize.Width, width), Math.Min(availableSize.Height, height));
    }

    private void CreateTextGeometry()
    {
        string? displayText = string.IsNullOrEmpty(Text) ? Placeholder : Text;
        if (string.IsNullOrEmpty(displayText))
        {
            _textGeometry = null;
            return;
        }

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        var geometries = new List<Geometry>();
        double currentX = 0.0;

        // 为每个字符创建单独的几何图形
        foreach (char c in displayText)
        {
            if (char.IsWhiteSpace(c))
            {
                currentX += 5 + LetterSpacing;
                continue;
            }

            var formattedText = new FormattedText(
                c.ToString(),
                Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                FontSize,
                Brushes.Black
            );

            // 初始创建时，Y坐标为0，后续通过整体变换调整
            var geometry = formattedText.BuildGeometry(new Point(currentX, 0));
            if (geometry != null)
            {
                geometries.Add(geometry);
            }

            // 更新下一个字符的X坐标
            currentX += formattedText.Width + LetterSpacing;
        }

        // 合并所有几何图形
        if (geometries.Count > 0)
        {
            var combinedGeometry = geometries[0];
            for (int i = 1; i < geometries.Count; i++)
            {
                combinedGeometry = new CombinedGeometry(GeometryCombineMode.Union, combinedGeometry, geometries[i]);
            }
            _textGeometry = combinedGeometry;
        }
        else
        {
            _textGeometry = null;
        }

        // 考虑描边宽度，调整原点位置
        double strokeOffset = Stroke != null && StrokeThickness > 0 ? StrokeThickness : 0;

        if (_textGeometry == null)
            return;

        // 获取当前几何图形的实际边界的左上角
        var currentTopLeft = _textGeometry.Bounds.TopLeft;

        // 计算所需的平移量，使文本的视觉左上角（包括描边）与 Origin 对齐
        double offsetX = Origin.X + strokeOffset - currentTopLeft.X;
        double offsetY = Origin.Y + strokeOffset - currentTopLeft.Y;

        var transform = new MatrixTransform(Matrix.CreateTranslation(offsetX, offsetY));
        _textGeometry = _textGeometry.Clone(); // 克隆以确保原始几何图形不被修改（如果它被共享）
        _textGeometry.Transform = transform;
    }
}
