using System;

namespace QwQ_Music.Definitions;

public record ExitReminderMessage(bool Success);

public record OperateCompletedMessage(string Name);

public record IsPageVisibleChangeMessage(bool IsVisible, Type PageType);

public record ThemeColorChangeMessage(string Theme, Type PageType);
