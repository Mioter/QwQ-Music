using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace QwQ_Music.Amusing;

public class Love {
    private readonly Color[] _colors = [
        Colors.Red, Colors.Pink, Colors.Orange, Colors.White, Colors.Purple, Colors.Gold, Colors.LimeGreen,
        Colors.DeepSkyBlue
    ];

    private readonly string[] _contents = [
        "❤️", "🎵", "🌟", "☀️", "🌈", "🌺", "🎉", "✨", "💖", "🎶", "💫", "🌸", "🎸", "🎹", "🥁", "🎧", "🎼", "📯"
    ];

    private static IEnumerable<PixelPoint> GenerateHeartPoints() {
        var points = new List<PixelPoint>();

        Window? mainWindow = GetMainWindow();

        if (mainWindow == null)
            return points;

        Screen? screen = mainWindow.Screens.Primary;

        if (screen == null)
            return points;

        PixelRect bounds = screen.Bounds;
        double centerX = bounds.Width / 2.0;
        double centerY = bounds.Height / 2.0 - bounds.Height * 0.1; // 上移 10%

        const int totalPoints = 80;                                         // 总点数
        double scale = Math.Min(bounds.Width * 0.02, bounds.Height * 0.05); // 屏幕宽度的一定比例，取较小值
        const double verticalScale = 0.85;                                  // 垂直方向比例系数
        const double minSpacing = 70;                                       // 最小窗口间距

        for (int i = 0; i < totalPoints; i++) {
            double t = 2 * Math.PI * i / totalPoints;

            // 标准爱心方程
            double x = 16 * Math.Pow(Math.Sin(t), 3);
            double y = 13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t);

            // 坐标变换
            double scaledX = x * scale;
            double scaledY = y * scale * verticalScale;

            var point = new PixelPoint(
                (int)(centerX + scaledX),
                (int)(centerY - scaledY) // Y轴翻转
            );

            // 检查与已有点的最小间距
            bool isValid = points
                           .Select(existingPoint => Math.Sqrt(
                                       Math.Pow(point.X - existingPoint.X, 2) + Math.Pow(point.Y - existingPoint.Y, 2)))
                           .All(distance => !(distance < minSpacing));

            if (isValid)
                points.Add(point);
        }

        // 添加中心点
        points.Add(new PixelPoint((int)centerX, (int)centerY));

        return points;
    }

    private static Window? GetMainWindow() {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }

    public async Task GenerateHeart() {
        var random = new Random();
        List<PixelPoint> points = GenerateHeartPoints().ToList();
        int lastIndex = points.Count - 1;

        if (lastIndex < 0)
            return;

        PixelPoint centerPoint = points[lastIndex];

        // 创建中心窗口
        var centerWindow = new LoveWindow(
            centerPoint,
            _colors[random.Next(_colors.Length)],
            _contents[random.Next(_contents.Length)],
            isCenter: true);

        // 创建小窗口并异步显示
        foreach (PixelPoint position in points.Take(lastIndex)) {
            int dx = position.X - centerPoint.X;
            int dy = position.Y - centerPoint.Y;
            Color color = _colors[random.Next(_colors.Length)];
            string content = _contents[random.Next(_contents.Length)];

            await Dispatcher.UIThread.InvokeAsync(() => {
                new LoveWindow(position, color, content, dx, dy, centerWindow).Show();

                return Task.CompletedTask;
            });
        }

        centerWindow.Show(); // 最后显示中心窗口
    }
}