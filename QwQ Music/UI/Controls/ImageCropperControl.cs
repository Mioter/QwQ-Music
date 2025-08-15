using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace QwQ_Music.UI.Controls;

/// <summary>
///     图片裁剪控件，支持自由比例和固定比例裁剪，提供直观的交互体验
/// </summary>
public class ImageCropperControl : Control
{
    #region 构造函数

    /// <summary>
    ///     初始化图片裁剪控件
    /// </summary>
    public ImageCropperControl()
    {
        ClipToBounds = true;
        _cropRect = new Rect(0, 0, MIN_CROP_SIZE, MIN_CROP_SIZE);
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     获取裁剪后的图片
    /// </summary>
    /// <returns>裁剪后的图片，如果源图片为空则返回null</returns>
    public Bitmap? GetCroppedImage()
    {
        if (_sourceImage == null)
            return null;

        var cropRect = new PixelRect(
            (int)((_cropRect.X - _imageOffset.X) / _imageScale),
            (int)((_cropRect.Y - _imageOffset.Y) / _imageScale),
            (int)(_cropRect.Width / _imageScale),
            (int)(_cropRect.Height / _imageScale)
        );

        var croppedBitmap = new WriteableBitmap(
            cropRect.Size,
            _sourceImage.Dpi,
            _sourceImage.Format,
            _sourceImage.AlphaFormat
        );

        using var fb = croppedBitmap.Lock();
        _sourceImage.CopyPixels(cropRect, fb.Address, fb.RowBytes * cropRect.Height, fb.RowBytes);

        return croppedBitmap;
    }

    #endregion

    #region 内部类型

    /// <summary>
    ///     调整手柄类型
    /// </summary>
    private enum ResizeHandle
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    #endregion

    #region 常量定义

    /// <summary>
    ///     最小缩放比例
    /// </summary>
    private const double MIN_SCALE = 0.1;

    /// <summary>
    ///     最大缩放比例
    /// </summary>
    private const double MAX_SCALE = 10.0;

    /// <summary>
    ///     缩放因子
    /// </summary>
    private const double SCALE_FACTOR = 0.1;

    /// <summary>
    ///     调整手柄大小
    /// </summary>
    private const double RESIZE_HANDLE_SIZE = 10;

    /// <summary>
    ///     安全边距，确保手柄可见
    /// </summary>
    private const double SAFE_MARGIN = RESIZE_HANDLE_SIZE * 2;

    /// <summary>
    ///     最小裁剪框尺寸
    /// </summary>
    private const double MIN_CROP_SIZE = 50;

    /// <summary>
    ///     最小控件尺寸
    /// </summary>
    private const double MIN_CONTROL_SIZE = MIN_CROP_SIZE + SAFE_MARGIN * 2;

    /// <summary>
    ///     默认裁剪框占可用空间的比例
    /// </summary>
    private const double DEFAULT_CROP_RATIO = 0.8;

    #endregion

    #region 私有字段

    private Bitmap? _sourceImage;
    private Rect _cropRect;
    private Point _dragStart;
    private bool _isDragging;
    private double _aspectRatio = 1.0;
    private double _imageScale = 1.0;
    private Point _imageOffset;
    private ResizeHandle _activeHandle = ResizeHandle.None;
    private bool _isResizing;
    private Size _lastControlSize;
    private bool _isControlSizeValid;

    #endregion

    #region 依赖属性

    /// <summary>
    ///     源图片
    /// </summary>
    public static readonly StyledProperty<Bitmap?> SourceImageProperty = AvaloniaProperty.Register<
        ImageCropperControl,
        Bitmap?
    >(nameof(SourceImage));

    /// <summary>
    ///     裁剪框宽高比，0表示自由比例
    /// </summary>
    public static readonly StyledProperty<double> AspectRatioProperty = AvaloniaProperty.Register<
        ImageCropperControl,
        double
    >(nameof(AspectRatio), 1.0);

    /// <summary>
    ///     裁剪后的图片
    /// </summary>
    public static readonly DirectProperty<ImageCropperControl, Bitmap?> CroppedImageProperty =
        AvaloniaProperty.RegisterDirect<ImageCropperControl, Bitmap?>(
            nameof(CroppedImage),
            o => o.CroppedImage
        );

    #endregion

    #region 属性访问器

    /// <summary>
    ///     获取或设置源图片
    /// </summary>
    public Bitmap? SourceImage
    {
        get => GetValue(SourceImageProperty);
        set => SetValue(SourceImageProperty, value);
    }

    /// <summary>
    ///     获取或设置裁剪框宽高比，0表示自由比例
    /// </summary>
    public double AspectRatio
    {
        get => GetValue(AspectRatioProperty);
        set => SetValue(AspectRatioProperty, value);
    }

    /// <summary>
    ///     获取裁剪后的图片
    /// </summary>
    public Bitmap? CroppedImage
    {
        get;
        private set => SetAndRaise(CroppedImageProperty, ref field, value);
    }

    #endregion

    #region 私有方法

    /// <summary>
    ///     获取调整手柄类型
    /// </summary>
    private ResizeHandle GetResizeHandle(Point point)
    {
        if (_aspectRatio != 0)
            return ResizeHandle.None;

        double handleSize = RESIZE_HANDLE_SIZE * 2;

        var handles = new[]
        {
            (
                ResizeHandle.TopLeft,
                new Rect(
                    _cropRect.Left - RESIZE_HANDLE_SIZE,
                    _cropRect.Top - RESIZE_HANDLE_SIZE,
                    handleSize,
                    handleSize
                )
            ),
            (
                ResizeHandle.TopRight,
                new Rect(
                    _cropRect.Right - RESIZE_HANDLE_SIZE,
                    _cropRect.Top - RESIZE_HANDLE_SIZE,
                    handleSize,
                    handleSize
                )
            ),
            (
                ResizeHandle.BottomLeft,
                new Rect(
                    _cropRect.Left - RESIZE_HANDLE_SIZE,
                    _cropRect.Bottom - RESIZE_HANDLE_SIZE,
                    handleSize,
                    handleSize
                )
            ),
            (
                ResizeHandle.BottomRight,
                new Rect(
                    _cropRect.Right - RESIZE_HANDLE_SIZE,
                    _cropRect.Bottom - RESIZE_HANDLE_SIZE,
                    handleSize,
                    handleSize
                )
            ),
        };

        return handles.FirstOrDefault(h => h.Item2.Contains(point)).Item1;
    }

    /// <summary>
    ///     计算最佳初始缩放比例
    /// </summary>
    private double CalculateInitialScale()
    {
        if (_sourceImage == null)
            return MIN_SCALE;

        var availableSize = GetAvailableSize();
        var imageSize = new Size(_sourceImage.PixelSize.Width, _sourceImage.PixelSize.Height);

        // 计算缩放比例，使图片尽可能填满可用空间
        double scaleX = availableSize.Width / imageSize.Width;
        double scaleY = availableSize.Height / imageSize.Height;

        // 选择较小的缩放比例，确保图片完全显示
        double scale = Math.Min(scaleX, scaleY);

        // 限制在有效范围内
        return Math.Max(MIN_SCALE, Math.Min(MAX_SCALE, scale));
    }

    /// <summary>
    ///     计算最佳初始裁剪框尺寸和位置
    /// </summary>
    private Rect CalculateInitialCropRect()
    {
        if (_sourceImage == null)
            return new Rect(0, 0, MIN_CROP_SIZE, MIN_CROP_SIZE);

        var availableSize = GetAvailableSize();

        var imageSize = new Size(
            _sourceImage.PixelSize.Width * _imageScale,
            _sourceImage.PixelSize.Height * _imageScale
        );

        // 计算裁剪框尺寸
        var cropSize = CalculateCropSize(availableSize, imageSize);

        // 计算居中位置
        var position = CalculateCenteredPosition(cropSize);

        return new Rect(position, cropSize);
    }

    /// <summary>
    ///     计算裁剪框尺寸
    /// </summary>
    private Size CalculateCropSize(Size availableSize, Size imageSize)
    {
        double width,
            height;

        if (_aspectRatio == 0)
        {
            // 自由比例：使用可用空间的默认比例
            width = Math.Min(availableSize.Width * DEFAULT_CROP_RATIO, imageSize.Width);
            height = Math.Min(availableSize.Height * DEFAULT_CROP_RATIO, imageSize.Height);
        }
        else
        {
            // 固定比例：根据可用空间和比例计算
            if (_aspectRatio > 1)
            {
                width = Math.Min(availableSize.Width, imageSize.Width);
                height = width / _aspectRatio;

                if (height > availableSize.Height)
                {
                    height = availableSize.Height;
                    width = height * _aspectRatio;
                }
            }
            else
            {
                height = Math.Min(availableSize.Height, imageSize.Height);
                width = height * _aspectRatio;

                if (width > availableSize.Width)
                {
                    width = availableSize.Width;
                    height = width / _aspectRatio;
                }
            }
        }

        // 确保最小尺寸
        width = Math.Max(MIN_CROP_SIZE, width);
        height = Math.Max(MIN_CROP_SIZE, height);

        return new Size(width, height);
    }

    /// <summary>
    ///     计算居中位置
    /// </summary>
    private Point CalculateCenteredPosition(Size size)
    {
        double x = (Bounds.Width - size.Width) / 2;
        double y = (Bounds.Height - size.Height) / 2;

        // 确保在安全边距内
        x = Math.Max(SAFE_MARGIN, Math.Min(Bounds.Width - SAFE_MARGIN - size.Width, x));
        y = Math.Max(SAFE_MARGIN, Math.Min(Bounds.Height - SAFE_MARGIN - size.Height, y));

        return new Point(x, y);
    }

    /// <summary>
    ///     获取可用空间大小
    /// </summary>
    private Size GetAvailableSize()
    {
        return new Size(
            Math.Max(MIN_CONTROL_SIZE, Bounds.Width - SAFE_MARGIN * 2),
            Math.Max(MIN_CONTROL_SIZE, Bounds.Height - SAFE_MARGIN * 2)
        );
    }

    /// <summary>
    ///     调整图片位置，确保裁剪框始终在图片范围内
    /// </summary>
    private void AdjustImagePosition()
    {
        if (_sourceImage == null)
            return;

        var imageSize = new Size(
            _sourceImage.PixelSize.Width * _imageScale,
            _sourceImage.PixelSize.Height * _imageScale
        );

        // 计算裁剪框在图片坐标系中的位置
        var cropBounds = new Rect(
            _cropRect.Left - _imageOffset.X,
            _cropRect.Top - _imageOffset.Y,
            _cropRect.Width,
            _cropRect.Height
        );

        // 调整X偏移
        if (cropBounds.Left < 0)
            _imageOffset = _imageOffset.WithX(_cropRect.Left);
        else if (cropBounds.Right > imageSize.Width)
            _imageOffset = _imageOffset.WithX(_cropRect.Right - imageSize.Width);

        // 调整Y偏移
        if (cropBounds.Top < 0)
            _imageOffset = _imageOffset.WithY(_cropRect.Top);
        else if (cropBounds.Bottom > imageSize.Height)
            _imageOffset = _imageOffset.WithY(_cropRect.Bottom - imageSize.Height);
    }

    /// <summary>
    ///     更新裁剪后的图片
    /// </summary>
    private void UpdateCroppedImage()
    {
        if (_sourceImage == null)
        {
            CroppedImage = null;

            return;
        }

        try
        {
            CroppedImage = GetCroppedImage();
        }
        catch (Exception)
        {
            // 如果裁剪失败，设置为null
            CroppedImage = null;
        }
    }

    /// <summary>
    ///     初始化控件
    /// </summary>
    private void Initialize()
    {
        if (_sourceImage == null)
            return;

        // 检查控件尺寸是否有效
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            _isControlSizeValid = false;

            return;
        }

        _isControlSizeValid = true;

        // 1. 先用默认逻辑计算初步缩放比例和裁剪框
        _imageScale = CalculateInitialScale();
        _cropRect = CalculateInitialCropRect();

        // 2. 计算此时的最小缩放比例，确保裁剪框不会超出图片内容
        double minScale = CalculateMinScaleForCropBox();

        if (_imageScale < minScale)
        {
            _imageScale = minScale;

            // 用新的缩放比例重新计算裁剪框
            _cropRect = CalculateInitialCropRect();
        }

        // 3. 计算初始偏移量，确保图片居中
        var imageSize = new Size(
            _sourceImage.PixelSize.Width * _imageScale,
            _sourceImage.PixelSize.Height * _imageScale
        );

        _imageOffset = new Point(
            (Bounds.Width - imageSize.Width) / 2,
            (Bounds.Height - imageSize.Height) / 2
        );

        // 调整图片位置，确保裁剪框在图片范围内
        AdjustImagePosition();

        UpdateCroppedImage();
        InvalidateVisual();
    }

    /// <summary>
    ///     计算当前裁剪框下图片的最小缩放比例，保证裁剪框不会超出图片内容
    /// </summary>
    private double CalculateMinScaleForCropBox()
    {
        if (_sourceImage == null)
            return MIN_SCALE;

        // 裁剪框尺寸（像素）
        double cropW = _cropRect.Width;
        double cropH = _cropRect.Height;
        double imgW = _sourceImage.PixelSize.Width;
        double imgH = _sourceImage.PixelSize.Height;

        // 固定比例时，裁剪框宽高比与图片宽高比可能不同
        // 取短边填满裁剪框
        double minScaleW = cropW / imgW;
        double minScaleH = cropH / imgH;
        double minScale = Math.Max(minScaleW, minScaleH);

        // 不能小于MIN_SCALE
        return Math.Max(MIN_SCALE, minScale);
    }

    #endregion

    #region 重写方法

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (_sourceImage == null)
            return;

        // 计算图片的实际大小和位置
        var imageSize = new Size(
            _sourceImage.PixelSize.Width * _imageScale,
            _sourceImage.PixelSize.Height * _imageScale
        );

        var imageRect = new Rect(_imageOffset, imageSize);

        // 绘制源图片
        context.DrawImage(_sourceImage, imageRect);

        // 绘制半透明遮罩
        var overlayBrush = new SolidColorBrush(Colors.Black, 0.5);
        var leftRect = new Rect(0, 0, _cropRect.X, Bounds.Height);
        var rightRect = new Rect(_cropRect.Right, 0, Bounds.Width - _cropRect.Right, Bounds.Height);
        var topRect = new Rect(_cropRect.X, 0, _cropRect.Width, _cropRect.Y);

        var bottomRect = new Rect(
            _cropRect.X,
            _cropRect.Bottom,
            _cropRect.Width,
            Bounds.Height - _cropRect.Bottom
        );

        context.FillRectangle(overlayBrush, leftRect);
        context.FillRectangle(overlayBrush, rightRect);
        context.FillRectangle(overlayBrush, topRect);
        context.FillRectangle(overlayBrush, bottomRect);

        // 绘制裁剪框
        var cropPen = new Pen(new SolidColorBrush(Colors.White));
        context.DrawRectangle(cropPen, _cropRect);

        // 绘制调整大小的手柄
        if (_aspectRatio == 0) // 只在自由比例模式下显示调整手柄
        {
            var handleBrush = new SolidColorBrush(Colors.White);
            double handleSize = RESIZE_HANDLE_SIZE;

            // 绘制四个角的手柄
            var handles = new[]
            {
                new Rect(
                    _cropRect.Left - handleSize / 2,
                    _cropRect.Top - handleSize / 2,
                    handleSize,
                    handleSize
                ),
                new Rect(
                    _cropRect.Right - handleSize / 2,
                    _cropRect.Top - handleSize / 2,
                    handleSize,
                    handleSize
                ),
                new Rect(
                    _cropRect.Left - handleSize / 2,
                    _cropRect.Bottom - handleSize / 2,
                    handleSize,
                    handleSize
                ),
                new Rect(
                    _cropRect.Right - handleSize / 2,
                    _cropRect.Bottom - handleSize / 2,
                    handleSize,
                    handleSize
                ),
            };

            foreach (var handle in handles)
            {
                context.FillRectangle(handleBrush, handle);
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceImageProperty)
        {
            _sourceImage = change.GetNewValue<Bitmap?>();
            Initialize();
        }
        else if (change.Property == AspectRatioProperty)
        {
            _aspectRatio = change.GetNewValue<double>();

            if (_sourceImage == null || !_isControlSizeValid)
                return;

            // 重新计算裁剪框
            _cropRect = CalculateInitialCropRect();

            // 调整图片位置
            AdjustImagePosition();

            UpdateCroppedImage();
            InvalidateVisual();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        if (_sourceImage == null || e.NewSize == _lastControlSize)
            return;

        _lastControlSize = e.NewSize;

        // 如果控件尺寸变得太小，调整缩放比例
        if (e.NewSize.Width < MIN_CONTROL_SIZE || e.NewSize.Height < MIN_CONTROL_SIZE)
        {
            _imageScale = CalculateInitialScale();
        }

        // 如果控件尺寸变为有效，且之前未初始化，则进行初始化
        if (!_isControlSizeValid && e.NewSize is { Width: > 0, Height: > 0 })
        {
            Initialize();
        }
        else if (_isControlSizeValid)
        {
            // 重新计算裁剪框
            _cropRect = CalculateInitialCropRect();

            // 调整图片位置
            AdjustImagePosition();

            UpdateCroppedImage();
            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (_sourceImage == null)
            return;

        var point = e.GetPosition(this);
        _activeHandle = GetResizeHandle(point);

        if (_activeHandle != ResizeHandle.None)
        {
            _isResizing = true;
        }
        else
        {
            _isDragging = true;
        }

        _dragStart = point;
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_sourceImage == null)
            return;

        var point = e.GetPosition(this);
        var delta = point - _dragStart;
        _dragStart = point;

        bool needsUpdate = false;

        if (_isResizing && _aspectRatio == 0)
        {
            // 调整裁剪框大小
            var newRect = CalculateNewCropRect(point);

            if (newRect is { Width: >= MIN_CROP_SIZE, Height: >= MIN_CROP_SIZE })
            {
                _cropRect = newRect;
                needsUpdate = true;
            }
        }
        else if (_isDragging)
        {
            // 计算新的偏移量
            var newOffset = _imageOffset + delta;

            var imageSize = new Size(
                _sourceImage.PixelSize.Width * _imageScale,
                _sourceImage.PixelSize.Height * _imageScale
            );

            // 限制偏移量，确保裁剪框不会超出图片范围
            if (_cropRect.Left - newOffset.X < 0)
                newOffset = newOffset.WithX(_cropRect.Left);
            else if (_cropRect.Right - newOffset.X > imageSize.Width)
                newOffset = newOffset.WithX(_cropRect.Right - imageSize.Width);

            if (_cropRect.Top - newOffset.Y < 0)
                newOffset = newOffset.WithY(_cropRect.Top);
            else if (_cropRect.Bottom - newOffset.Y > imageSize.Height)
                newOffset = newOffset.WithY(_cropRect.Bottom - imageSize.Height);

            _imageOffset = newOffset;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            UpdateCroppedImage();
            InvalidateVisual();
        }
    }

    /// <summary>
    ///     计算新的裁剪框
    /// </summary>
    private Rect CalculateNewCropRect(Point point)
    {
        if (_sourceImage == null)
            return default;

        var imageSize = new Size(
            _sourceImage.PixelSize.Width * _imageScale,
            _sourceImage.PixelSize.Height * _imageScale
        );

        double newWidth = _cropRect.Width;
        double newHeight = _cropRect.Height;
        double newX = _cropRect.X;
        double newY = _cropRect.Y;

        // 计算图片在控件中的实际位置
        double imageLeft = _imageOffset.X;
        double imageTop = _imageOffset.Y;
        double imageRight = imageLeft + imageSize.Width;
        double imageBottom = imageTop + imageSize.Height;

        switch (_activeHandle)
        {
            case ResizeHandle.TopLeft:
                newWidth = _cropRect.Right - point.X;
                newHeight = _cropRect.Bottom - point.Y;
                newX = point.X;
                newY = point.Y;

                break;
            case ResizeHandle.TopRight:
                newWidth = point.X - _cropRect.Left;
                newHeight = _cropRect.Bottom - point.Y;
                newY = point.Y;

                break;
            case ResizeHandle.BottomLeft:
                newWidth = _cropRect.Right - point.X;
                newHeight = point.Y - _cropRect.Top;
                newX = point.X;

                break;
            case ResizeHandle.BottomRight:
                newWidth = point.X - _cropRect.Left;
                newHeight = point.Y - _cropRect.Top;

                break;
        }

        // 限制裁剪框在图片范围内
        if (newX < imageLeft)
        {
            newWidth += newX - imageLeft;
            newX = imageLeft;
        }

        if (newY < imageTop)
        {
            newHeight += newY - imageTop;
            newY = imageTop;
        }

        if (newX + newWidth > imageRight)
        {
            newWidth = imageRight - newX;
        }

        if (newY + newHeight > imageBottom)
        {
            newHeight = imageBottom - newY;
        }

        // 限制裁剪框在控件安全范围内
        if (newX < SAFE_MARGIN)
        {
            newWidth += newX - SAFE_MARGIN;
            newX = SAFE_MARGIN;
        }

        if (newY < SAFE_MARGIN)
        {
            newHeight += newY - SAFE_MARGIN;
            newY = SAFE_MARGIN;
        }

        if (newX + newWidth > Bounds.Width - SAFE_MARGIN)
        {
            newWidth = Bounds.Width - SAFE_MARGIN - newX;
        }

        if (newY + newHeight > Bounds.Height - SAFE_MARGIN)
        {
            newHeight = Bounds.Height - SAFE_MARGIN - newY;
        }

        return new Rect(newX, newY, newWidth, newHeight);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (!_isDragging && !_isResizing)
            return;

        _isDragging = false;
        _isResizing = false;
        _activeHandle = ResizeHandle.None;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (_sourceImage == null)
            return;

        var point = e.GetPosition(this);
        double delta = e.Delta.Y;

        // 计算缩放中心点相对于图片的偏移
        double relativeX =
            (point.X - _imageOffset.X) / (_sourceImage.PixelSize.Width * _imageScale);

        double relativeY =
            (point.Y - _imageOffset.Y) / (_sourceImage.PixelSize.Height * _imageScale);

        // 计算新的缩放比例
        double newScale = _imageScale * (1 + delta * SCALE_FACTOR);

        // 限制缩放范围，最小缩放比例为当前裁剪框下图片能覆盖裁剪框的最小比例
        double minScale = CalculateMinScaleForCropBox();
        newScale = Math.Max(minScale, Math.Min(MAX_SCALE, newScale));

        // 如果缩放比例没有变化，直接返回
        if (Math.Abs(newScale - _imageScale) < 0.001)
            return;

        // 计算新的图片偏移，保持鼠标指向的点不变
        var newSize = new Size(
            _sourceImage.PixelSize.Width * newScale,
            _sourceImage.PixelSize.Height * newScale
        );

        var newOffset = new Point(
            point.X - relativeX * newSize.Width,
            point.Y - relativeY * newSize.Height
        );

        // 计算裁剪框在图片坐标系中的位置
        var cropBounds = new Rect(
            _cropRect.Left - newOffset.X,
            _cropRect.Top - newOffset.Y,
            _cropRect.Width,
            _cropRect.Height
        );

        // 限制偏移量，确保裁剪框不会超出图片范围
        if (cropBounds.Left < 0)
            newOffset = newOffset.WithX(_cropRect.Left);
        else if (cropBounds.Right > newSize.Width)
            newOffset = newOffset.WithX(_cropRect.Right - newSize.Width);

        if (cropBounds.Top < 0)
            newOffset = newOffset.WithY(_cropRect.Top);
        else if (cropBounds.Bottom > newSize.Height)
            newOffset = newOffset.WithY(_cropRect.Bottom - newSize.Height);

        _imageScale = newScale;
        _imageOffset = newOffset;

        UpdateCroppedImage();
        InvalidateVisual();
    }

    #endregion
}
