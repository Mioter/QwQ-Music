using Timer = System.Timers.Timer;

namespace QwQ_Music.Common.Helpers;

public static class TimeoutHelper {
    public static void Timeout(int timeoutMillisecond, Action action, Action callback) {
        // 注：不要使用 CancellationToken，如果出现死锁则该 Task无法检测 Token。
        Timer timer = new() { Interval = timeoutMillisecond, AutoReset = false };
        timer.Elapsed += (sender, _) => {
            (sender as Timer)?.Dispose();
            callback();
            throw new TimeoutException();
        };
        timer.Start();
        action();
        try {
            timer.Stop();
            timer.Dispose();
        } catch (Exception) {
            // ignored
        }
    }
}