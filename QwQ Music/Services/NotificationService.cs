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

    /// <summary>
    /// 根据消息文本长度自动计算合适的显示时间
    /// </summary>
    /// <param name="message">消息文本</param>
    /// <param name="customExpiration">自定义过期时间，如果为null则自动计算</param>
    /// <returns>计算得出的过期时间</returns>
    private static TimeSpan CalculateExpiration(string message, TimeSpan? customExpiration)
    {
        // 如果提供了自定义过期时间，优先使用
        if (customExpiration.HasValue)
            return customExpiration.Value;

        // 根据消息长度计算阅读时间
        // 假设平均阅读速度为每分钟200个字符
        const double charactersPerMinute = 200.0;
        const double minimumSeconds = 3.0; // 最小显示时间3秒
        const double maximumSeconds = 15.0; // 最大显示时间15秒

        // 计算基于字符数的阅读时间（秒）
        double readingTimeSeconds = message.Length / charactersPerMinute * 60.0;

        // 确保时间在合理范围内
        readingTimeSeconds = Math.Max(minimumSeconds, Math.Min(maximumSeconds, readingTimeSeconds));

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
                ["Light"]
            );
        });
    }
}
