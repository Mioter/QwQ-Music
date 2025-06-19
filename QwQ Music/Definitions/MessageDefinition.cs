using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace QwQ_Music.Definitions;

public record ExitReminderMessage(bool Success);

public record LoadCompletedMessage(string Name);

public record ViewChangeMessage(string Id, string ViewTitle, Bitmap ViewIcon, Control? View, bool IsRemove = false);
