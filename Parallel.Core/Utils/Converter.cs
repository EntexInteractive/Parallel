// Copyright 2026 Entex Interactive

using System.Net;

namespace Parallel.Core.Utils
{
    /// <summary>
    /// Converts a data type to another data type. This class cannot be inherited.
    /// </summary>
    public static class Converter
    {
        /// <summary>
        /// Converts a int32 into a boolean.
        /// </summary>
        /// <param name="value">The int32 value.</param>
        /// <returns>If 1 true, false otherwise.</returns>
        public static bool ToBool(double value)
        {
            return value.Equals(1);
        }

        /// <summary>
        /// Converts a boolean into an int32.
        /// </summary>
        /// <param name="value">The int32 value.</param>
        /// <returns>If 1 true, false otherwise.</returns>
        public static int ToInt32(bool value)
        {
            return value ? 1 : 0;
        }

        /// <summary>
        /// Converts two double values into a percent.
        /// </summary>
        /// <param name="part"></param>
        /// <param name="whole"></param>
        public static double ToPercent(double part, double whole)
        {
            return part * 100 / whole;
        }

        public static TimeSpan ToRemainingTimeSpan(TimeSpan elapsed, double current, double total)
        {
            if (current <= 0 || total <= 0 || current >= total) return TimeSpan.Zero;
            double remainingMs = elapsed.TotalMilliseconds * (total - current) / current;

            if (double.IsNaN(remainingMs) || remainingMs <= 0) return TimeSpan.Zero;
            if (double.IsInfinity(remainingMs) || remainingMs >= TimeSpan.MaxValue.TotalMilliseconds) return TimeSpan.MaxValue;
            return TimeSpan.FromMilliseconds(remainingMs);
        }
    }
}