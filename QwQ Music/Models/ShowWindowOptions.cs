using Avalonia.Controls;

namespace QwQ_Music.Models;

public class ShowWindowOptions
{
    public required string Title { get; set; }

    public bool IsRestoreButtonVisible { get; set; } = true;

    public bool IsCloseButtonVisible { get; set; } = true;

    public bool IsMinimizeButtonVisible { get; set; } = true;

    public bool IsFullScreenButtonVisible { get; set; } = true;

    public WindowStartupLocation StartupLocation { get; set; } = WindowStartupLocation.CenterOwner;

    public SizeToContent SizeToContent { get; set; } = SizeToContent.WidthAndHeight;

    public bool CanResize { get; set; }

    public double MinWidth { get; set; }

    public double MinHeight { get; set; }

    public double MaxWidth { get; set; } = double.PositiveInfinity;

    public double MaxHeight { get; set; } = double.PositiveInfinity;
}
