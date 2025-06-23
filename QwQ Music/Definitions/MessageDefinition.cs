using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

namespace QwQ_Music.Definitions;

public record ExitReminderMessage(bool Success);

public record OperateCompletedMessage(string Name);

public record IsPageVisibleChangeMessage(bool IsVisible, Type PageType);

public record ThemeColorChangeMessage(ThemeVariant Theme, Type PageType);

public record ViewChangeMessage(string Id, string ViewTitle, Bitmap ViewIcon, Control? View, bool IsRemove = false);
