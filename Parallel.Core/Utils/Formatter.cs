// Copyright 2026 Entex Interactive

namespace Parallel.Core.Utils
{
    /// <summary>
    /// Converts a data type to a formatted string. This class cannot be inherited.
    /// </summary>
    public class Formatter
    {
        /// <summary>
        /// Formats a file size with the corresponding data volume.
        /// </summary>
        /// <param name="bytes">The bytes to convert.</param>
        /// <returns>The bytes formatted as a string.</returns>
        public static string FromBytes(double bytes)
        {
            string[] sizeSuffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int sizeIndex = 0;
            double size = Math.Abs(bytes); // use absolute for scaling

            while (size >= 1000 && sizeIndex < sizeSuffixes.Length - 1)
            {
                sizeIndex++;
                size /= 1000;
            }

            // add the sign back
            string sign = bytes < 0 ? "-" : "";
            return $"{sign}{size:N2} {sizeSuffixes[sizeIndex]}";
        }


        /// <summary>
        /// Formats a <see cref="DateTime"/> with the corresponding data volume.
        /// </summary>
        /// <param name="dateTime">The bytes to convert.</param>
        /// <returns>A <see cref="DateTime"/> formatted as an MM/DD/YYYY HH:MM TT.</returns>
        public static string FromDateTime(DateTime dateTime)
        {
            return dateTime.ToLocalTime().ToString("MM/dd/yyyy hh:mmtt");
        }

        public static string FromTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 365)
            {
                int years = (int)Math.Floor(timeSpan.TotalDays / 365);
                return years == 1 ? "1 year ago" : $"{years} years ago";
            }
            else if (timeSpan.TotalDays > 30)
            {
                int months = (int)Math.Floor(timeSpan.TotalDays / 30);
                return months == 1 ? "1 month ago" : $"{months} months ago";
            }
            else if (timeSpan.TotalHours > 24)
            {
                int days = (int)timeSpan.TotalDays;
                return days == 1 ? "1 day ago" : $"{days} days ago";
            }
            else if (timeSpan.TotalMinutes > 60)
            {
                int hours = (int)timeSpan.TotalHours;
                return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
            }
            else if (timeSpan.TotalSeconds > 60)
            {
                int minutes = (int)timeSpan.TotalMinutes;
                return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
            }
            else
            {
                return "Just now";
            }
        }

        public static string FromTimeSpan(DateTime dateTime)
        {
            return dateTime.ToString("dd:HH:mm:ss.fff");
        }

        public static string FromCheckSum(string? checkSum)
        {
            return checkSum == null ? string.Empty : $"{checkSum[..8]}...";
        }
    }
}