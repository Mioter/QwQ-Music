using System;

namespace QwQ_Music.Services;

public static class ExitReminderService
{
    public static event EventHandler? ExitReminder;

    public static void Exit() => ExitReminder?.Invoke(null, EventArgs.Empty);
}
