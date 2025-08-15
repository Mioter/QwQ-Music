using System;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace QwQ_Music.Common.Services;

public static class NotificationService
{
    public static WindowNotificationManager? NotificationManager { get; } =
        WindowNotificationManager.TryGetNotificationManager(App.TopLevel, out var manager)
            ? manager
            : new WindowNotificationManager(App.TopLevel)
            {
                Margin = new Thickness(0, 40, 0, 0),
            };

    public static double CharactersPerMinute { get; set; } = 200.0;

    public static double MinimumSeconds { get; set; } = 3.0; // 最小显示时间3秒

    public static double MaximumSeconds { get; set; } = 15.0; // 最大显示时间15秒

    /// <summary>
    ///     根据消息文本长度自动计算合适的显示时间
    /// </summary>
    /// <param name="message">消息文本</param>
    /// <param name="customExpiration">自定义过期时间，如果为null则自动计算</param>
    /// <returns>计算得出的过期时间</returns>
    private static TimeSpan CalculateExpiration(string message, TimeSpan? customExpiration)
    {
        // 如果提供了自定义过期时间，优先使用
        if (customExpiration.HasValue)
            return customExpiration.Value;

        // 计算基于字符数的阅读时间（秒）
        double readingTimeSeconds = message.Length / CharactersPerMinute * 60.0;

        // 确保时间在合理范围内
        readingTimeSeconds = Math.Max(MinimumSeconds, Math.Min(MaximumSeconds, readingTimeSeconds));

        return TimeSpan.FromSeconds(readingTimeSeconds);
    }

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
            var calculatedExpiration = CalculateExpiration(message, expiration);

            NotificationManager?.Show(
                new Notification(title, message),
                type,
                calculatedExpiration,
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
        Show(
            title,
            message,
            type,
            expiration,
            showIcon,
            showClose,
            onClick,
            onClose,
            ["Light"]
        );
    }

    public static void Success(string message, string[]? classes = null)
    {
        Show("好欸！", message, NotificationType.Success, classes: classes);
    }

    public static void Error(string message, string[]? classes = null)
    {
        Show("坏欸！", message, NotificationType.Error, classes: classes);
    }

    public static void Info(string message, string[]? classes = null)
    {
        Show("提示！", message, NotificationType.Information, classes: classes);
    }

    public static void Warning(string message, string[]? classes = null)
    {
        Show("注意！", message, NotificationType.Warning, classes: classes);
    }

    public static void Success(string title, string message, string[]? classes = null)
    {
        Show(title, message, NotificationType.Success, classes: classes);
    }

    public static void Error(string title, string message, string[]? classes = null)
    {
        Show(title, message, NotificationType.Error, classes: classes);
    }

    public static void Info(string title, string message, string[]? classes = null)
    {
        Show(title, message, NotificationType.Information, classes: classes);
    }

    public static void Warning(string title, string message, string[]? classes = null)
    {
        Show(title, message, NotificationType.Warning, classes: classes);
    }
}
