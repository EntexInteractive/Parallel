// Copyright 2026 Entex Interactive

using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Parallel.Core.Utils
{
    /// <summary>
    /// Represents an event tracking sink for Serilog.
    /// </summary>
    public class LogEventTracker : ILogEventSink
    {
        /// <summary>
        /// Gets an array of log messages.
        /// </summary>
        public readonly ConcurrentBag<string> Logs = new ConcurrentBag<string>();

        /// <summary>
        /// Gets the number of errors in this log.
        /// </summary>
        public int ErrorCount = 0;

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level >= LogEventLevel.Error)
            {
                Logs.Add($"[{logEvent.Timestamp.LocalDateTime:HH:mm:ss.fff} {logEvent.Level.ToString()[..4].ToUpperInvariant()}]: {logEvent.Exception}");
                Interlocked.Increment(ref ErrorCount);
            }
            else
            {
                Logs.Add($"[{logEvent.Timestamp.LocalDateTime:HH:mm:ss.fff} {logEvent.Level.ToString()[..4].ToUpperInvariant()}]: {logEvent.RenderMessage()}");
            }
        }
    }
}