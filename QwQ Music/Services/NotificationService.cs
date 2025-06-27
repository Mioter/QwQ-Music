using System;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace QwQ_Music.Services;

public static class NotificationService
{
    public static WindowNotificationManager? NotificationManager { get; } =
        WindowNotificationManager.TryGetNotificationManager(App.TopLevel, out var manager)
            ? manager
            : new WindowNotificationManager(App.TopLevel) { Margin = new Thickness(0, 40, 0, 0) };

    public static void Show(
        string title,
        string message,
        NotificationType type,
        TimeSpan? expiration = null,
        bool showIcon = true,
        bool showClose = true,
        Action? onClick = null,
        Action? onClose = null,
        string[]? classes = null
    )
    {
        Dispatcher.UIThread.Post(() =>
        {
            NotificationManager?.Show(
                new Notification(title, message),
                type,
                expiration,
                showIcon,
                showClose,
                onClick,
                onClose,
                classes
            );
        });
    }

    public static void ShowLight(
        string title,
        string message,
        NotificationType type,
        TimeSpan? expiration = null,
        bool showIcon = true,
        bool showClose = true,
        Action? onClick = null,
        Action? onClose = null
    )
    {
        Dispatcher.UIThread.Post(() =>
        {
            NotificationManager?.Show(
                new Notification(title, message),
                type,
                expiration,
                showIcon,
                showClose,
                onClick,
                onClose,
                ["Light"]
            );
        });
    }
}
