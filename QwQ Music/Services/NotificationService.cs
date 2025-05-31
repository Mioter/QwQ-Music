using System;
using Avalonia;
using Avalonia.Controls.Notifications;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace QwQ_Music.Services;

public static class NotificationService
{
    public static WindowNotificationManager? NotificationManager { get; } =
        WindowNotificationManager.TryGetNotificationManager(App.TopLevel, out var manager)
            ? manager
            : new WindowNotificationManager(App.TopLevel) { Margin = new Thickness(0, 40, 0, 0) };

    public static void Show(
        object content,
        NotificationType type,
        TimeSpan? expiration = null,
        bool showIcon = true,
        bool showClose = true,
        Action? onClick = null,
        Action? onClose = null,
        string[]? classes = null
    )
    {
        NotificationManager?.Show(content, type, expiration, showIcon, showClose, onClick, onClose, classes);
    }
}
