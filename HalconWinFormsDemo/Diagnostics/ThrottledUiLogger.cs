using System;
using System.Collections.Concurrent;

namespace HalconWinFormsDemo.Diagnostics
{
    /// <summary>
    /// Simple in-process logger with:
    /// - levels
    /// - per-key throttling to prevent UI/log flooding
    /// - a UI sink (e.g., MainForm textbox) for formatted lines
    /// </summary>
    public sealed class ThrottledUiLogger
    {
        private readonly ConcurrentDictionary<string, long> _lastTicksByKey = new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
        private Action<string> _uiSink;

        /// <summary>Bind a UI sink to receive formatted log lines.</summary>
        public void BindUiSink(Action<string> sink) => _uiSink = sink;

        public void Debug(string category, string message, string throttleKey = null, int minIntervalMs = 0)
            => Log(LogLevel.Debug, category, message, throttleKey, minIntervalMs);

        public void Info(string category, string message, string throttleKey = null, int minIntervalMs = 0)
            => Log(LogLevel.Info, category, message, throttleKey, minIntervalMs);

        public void Warn(string category, string message, string throttleKey = null, int minIntervalMs = 2000)
            => Log(LogLevel.Warn, category, message, throttleKey, minIntervalMs);

        public void Error(string category, string message, string throttleKey = null, int minIntervalMs = 1000)
            => Log(LogLevel.Error, category, message, throttleKey, minIntervalMs);

        public void Log(LogLevel level, string category, string message, string throttleKey, int minIntervalMs)
        {
            // Throttling: if caller provides key OR we create a default key for noisy categories
            string key = throttleKey ?? $"{level}:{category}:{message}";
            if (minIntervalMs > 0)
            {
                var now = DateTime.UtcNow.Ticks;
                var last = _lastTicksByKey.GetOrAdd(key, 0);
                var minTicks = TimeSpan.FromMilliseconds(minIntervalMs).Ticks;
                if (last != 0 && (now - last) < minTicks)
                    return;
                _lastTicksByKey[key] = now;
            }

            var line = $"[{DateTime.Now:HH:mm:ss}][{level}][{category}] {message}";
            _uiSink?.Invoke(line);
        }
    }
}
