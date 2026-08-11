// Copyright 2026 Entex Interactive

using System.Collections.Concurrent;

namespace Parallel.Core.Models
{
    /// <summary>
    /// Represents a read-only record of a file at a specific moment in time.
    /// </summary>
    public class SnapshotRecord
    {
        /// <summary>
        /// The path of the file on the local machine.
        /// </summary>
        public string Fullname { get; set; }

        /// <summary>
        /// The time the current file was last written to.
        /// </summary>
        public DateTime LastWrite { get; set; }

        /// <summary>
        /// The checksum used to check if the file has changed.
        /// </summary>
        public string? LocalCheckSum { get; set; }

        /// <summary>
        /// The checksum used to check if the file was fully uploaded.
        /// </summary>
        public string? RemoteCheckSum { get; set; }

        /// <summary>
        /// If the file is currently hidden.
        /// </summary>
        public bool Hidden { get; } = false;

        /// <summary>
        /// If the file is currently read-only.
        /// </summary>
        public bool ReadOnly { get; } = false;

        public SnapshotRecord(LocalFile file)
        {
            Fullname = file.Fullname;
            LastWrite = file.LastWrite.ToLocalTime();
            LocalCheckSum = file.LocalCheckSum;
            RemoteCheckSum = file.RemoteCheckSum;
            Hidden = file.Hidden;
            ReadOnly = file.ReadOnly;
        }
    }
}