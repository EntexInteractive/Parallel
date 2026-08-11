// Copyright 2026 Entex Interactive

using Newtonsoft.Json;
using Parallel.Core.Extensions.Json;

namespace Parallel.Core.Utils
{
    /// <summary>
    /// Represents an instant in time, expressed as either the seconds, or milliseconds since the Unix epoch.
    /// </summary>
    [JsonConverter(typeof(UnixTimeConverter))]
    public readonly struct UnixTime
    {
        private readonly DateTime _timestamp;

        #region Constants

        /// <summary>
        /// Represents the largest possible value of <see cref="UnixTime"/>.
        /// </summary>
        public const long MaxValue = 9223372036854775807;

        /// <summary>
        /// Represents the smallest possible value of <see cref="UnixTime"/>.
        /// </summary>
        public const long MinValue = -9223372036854775808;

        /// <summary>
        /// A year in the corresponding milliseconds.
        /// </summary>
        public const long Year = 31104000000;

        /// <summary>
        /// A month in the corresponding milliseconds.
        /// </summary>
        public const long Month = 2592000000;

        /// <summary>
        /// A week in the corresponding milliseconds.
        /// </summary>
        public const long Week = 604800000;

        /// <summary>
        /// A day in the corresponding milliseconds.
        /// </summary>
        public const long Day = 86400000;

        /// <summary>
        /// An hour in the corresponding milliseconds.
        /// </summary>
        public const long Hour = 3600000;

        /// <summary>
        /// A minute in the corresponding milliseconds.
        /// </summary>
        public const long Minute = 60000;

        /// <summary>
        /// A second in the corresponding milliseconds.
        /// </summary>
        public const long Second = 1000;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a <see cref="UnixTime"/> object that is set to current time since epoch.
        /// </summary>
        public static UnixTime Now => new(DateTime.UtcNow);

        /// <summary>
        /// Gets the value of the current <see cref="UnixTime"/> structure expressed as whole milliseconds.
        /// </summary>
        public long TotalMilliseconds => (long)_timestamp.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

        /// <summary>
        /// Gets the value of the current <see cref="UnixTime"/> structure expressed as whole and fractional seconds.
        /// </summary>
        public double TotalSeconds => _timestamp.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;


        /// <summary>
        /// Gets the value of the current <see cref="UnixTime"/> structure expressed as whole and fractional minutes.
        /// </summary>
        public double TotalMinutes => _timestamp.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMinutes;

        /// <summary>
        /// Gets the value of the current <see cref="UnixTime"/> structure expressed as whole and fractional hours.
        /// </summary>
        public double TotalHours => _timestamp.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalHours;

        /// <summary>
        /// Gets the value of the current <see cref="UnixTime"/> structure expressed as whole and fractional days.
        /// </summary>
        public double TotalDays => _timestamp.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes new instance of the <see cref="UnixTime"/> structure with epoch time.
        /// </summary>
        public UnixTime()
        {
            _timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// Initializes new instance of the <see cref="UnixTime"/> structure to the specified <see cref="DateTime"/>.
        /// </summary>
        /// <param name="datetime">The specified <see cref="DateTime"/>.</param>
        public UnixTime(DateTime datetime)
        {
            _timestamp = datetime.ToUniversalTime();
        }

        /// <summary>
        /// Converts a string representation of a date and time to its <see cref="UnixTime"/> equivalent.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>An object equivalent to the date and time of the string.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FormatException"></exception>
        public static UnixTime Parse(string? s)
        {
            ArgumentException.ThrowIfNullOrEmpty(s);
            return new UnixTime(DateTime.Parse(s).ToUniversalTime());
        }

        /// <summary>
        /// Returns a <see cref="UnixTime"/> that represents the milliseconds since epoch.
        /// </summary>
        /// <param name="value"></param>
        public static UnixTime FromTicks(long value)
        {
            return new UnixTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(value));
        }

        /// <summary>
        /// Returns a <see cref="UnixTime"/> that represents the milliseconds since epoch.
        /// </summary>
        /// <param name="value"></param>
        public static UnixTime FromMilliseconds(double value)
        {
            return new UnixTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(value));
        }

        /// <summary>
        /// Returns a <see cref="UnixTime"/> that represents the seconds since epoch.
        /// </summary>
        /// <param name="value"></param>
        public static UnixTime FromSeconds(double value)
        {
            return new UnixTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(value));
        }

        /// <summary>
        /// Returns a <see cref="UnixTime"/> that represents the minutes since epoch.
        /// </summary>
        /// <param name="value"></param>
        public static UnixTime FromMinutes(double value)
        {
            return new UnixTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(value));
        }

        /// <summary>
        /// Returns a <see cref="UnixTime"/> that represents the hours since epoch.
        /// </summary>
        /// <param name="value"></param>
        public static UnixTime FromHours(double value)
        {
            return new UnixTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(value));
        }

        /// <summary>
        /// Returns a <see cref="UnixTime"/> that represents the days since epoch.
        /// </summary>
        /// <param name="value"></param>
        public static UnixTime FromDays(double value)
        {
            return new UnixTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(value));
        }

        /// <summary>
        /// Returns a <see cref="UnixTime"/> that represents the months since epoch.
        /// </summary>
        /// <param name="value"></param>
        public static UnixTime FromMonths(int value)
        {
            return new UnixTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(value));
        }

        /// <summary>
        /// Returns a <see cref="UnixTime"/> that represents the years since epoch.
        /// </summary>
        /// <param name="value"></param>
        public static UnixTime FromYears(int value)
        {
            return new UnixTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddYears(value));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a new <see cref="UnixTime"/> that adds the specified <see cref="TimeSpan"/> to the value of this instance.
        /// </summary>
        /// <param name="time"></param>
        /// <returns>An object whose value is equal to the Unix time represented by this instance.</returns>
        public UnixTime Add(TimeSpan time)
        {
            return new UnixTime(_timestamp.Add(time));
        }

        /// <summary>
        /// Returns a new <see cref="UnixTime"/> that adds the specified <see cref="UnixTime"/> to the value of this instance.
        /// </summary>
        /// <param name="time"></param>
        /// <returns>An object whose value is equal to  the Unix time represented by this instance.</returns>
        public UnixTime Add(UnixTime time)
        {
            return Add(TimeSpan.FromMilliseconds(time.TotalMilliseconds));
        }

        /// <summary>
        /// Returns a new <see cref="UnixTime"/> that subtracts the specified <see cref="TimeSpan"/> to the value of this instance.
        /// </summary>
        /// <param name="time"></param>
        /// <returns>An object whose value is equal to  the Unix time represented by this instance.</returns>
        public UnixTime Subtract(TimeSpan time)
        {
            return new UnixTime(_timestamp.Subtract(time));
        }

        /// <summary>
        /// Returns a new <see cref="UnixTime"/> that subtracts the specified <see cref="UnixTime"/> to the value of this instance.
        /// </summary>
        /// <param name="time"></param>
        /// <returns>An object whose value is equal to  the Unix time represented by this instance.</returns>
        public UnixTime Subtract(UnixTime time)
        {
            return Subtract(TimeSpan.FromMilliseconds(time.TotalMilliseconds));
        }

        /// <summary>
        /// Converts the value of the current <see cref="UnixTime"/> to a <see cref="DateTime"/>.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> equivalent whose <see cref="DateTime.Kind"/> property is set to <see cref="DateTimeKind.Utc"/>.</returns>
        public DateTime ToUniversalTime()
        {
            return _timestamp.ToUniversalTime();
        }

        /// <summary>
        /// Converts the value of the current <see cref="UnixTime"/> to a <see cref="DateTime"/>.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> equivalent whose <see cref="DateTime.Kind"/> property is set to <see cref="DateTimeKind.Local"/>.</returns>
        public DateTime ToLocalTime()
        {
            return _timestamp.ToLocalTime();
        }

        /// <summary>
        /// Converts the value of the current <see cref="UnixTime"/> to a <see cref="TimeSpan"/> since January 1st, 1970.
        /// </summary>
        /// <returns>A <see cref="TimeSpan"/> structure equivalent to the current <see cref="UnixTime"/>.</returns>
        public TimeSpan ToTimeSpan()
        {
            return TimeSpan.FromMilliseconds(TotalMilliseconds);
        }

        /// <summary>
        /// Converts the current <see cref="UnixTime"/> to the equivalent milliseconds since epoch.
        /// </summary>
        /// <returns>The string representation of milliseconds since epoch.</returns>
        public override string ToString()
        {
            return TotalMilliseconds.ToString();
        }

        /// <summary>
        /// Converts the current <see cref="UnixTime"/> to the equivalent representation specified by format.
        /// </summary>
        /// <param name="format"></param>
        /// <returns>The string representation specified by the <paramref name="format"/>.</returns>
        public string ToString(string format)
        {
            return _timestamp.ToString(format);
        }

        /// <summary>
        /// Converts the current <see cref="UnixTime"/> to its equivalent representation specified by the <see href="https://en.wikipedia.org/wiki/ISO_8601">ISO 8601</see> format.
        /// </summary>
        /// <returns>The string representation specified by the <see href="https://en.wikipedia.org/wiki/ISO_8601">ISO 8601</see> format.</returns>
        public string ToISOString()
        {
            return _timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        #endregion
    }
}