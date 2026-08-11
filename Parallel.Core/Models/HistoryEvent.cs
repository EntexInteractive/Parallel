// Copyright 2026 Entex Interactive

using Parallel.Core.Database;
using Parallel.Core.Utils;

namespace Parallel.Core.Models
{
    public class HistoryEvent
    {
        public HistoryType Type { get; set; }
        public UnixTime CreatedAt { get; set; }
        public string Fullname { get; set; }

        public HistoryEvent(long timestamp, string fullname, long type)
        {
            CreatedAt = UnixTime.FromMilliseconds(timestamp);
            Fullname = fullname;
            Type = (HistoryType)type;
        }
    }
}