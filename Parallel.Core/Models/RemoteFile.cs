// Copyright 2026 Entex Interactive

using Parallel.Core.Utils;

namespace Parallel.Core.Models
{
    public class RemoteFile
    {
        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The path of the file on the local machine.
        /// </summary>
        public string Fullname { get; set; }

        /// <summary>
        /// The time the current file was last written to.
        /// </summary>
        public UnixTime LastWrite { get; set; }

        /// <summary>
        /// The time the file was either last saved or deleted.
        /// </summary>
        public UnixTime LastUpdate { get; set; }

        /// <summary>
        /// The size, in bytes, of the file in the remote backup.
        /// </summary>
        public long RemoteSize { get; set; }

        /// <summary>
        /// The checksum used to check if the file was fully uploaded.
        /// </summary>
        public string? RemoteCheckSum { get; set; }

        public RemoteFile(string name, string fullname, DateTime lastWriteTime, long remoteSize, string? remoteChecksum)
        {
            Name = name;
            Fullname = fullname;
            LastWrite = new UnixTime(lastWriteTime);
            LastUpdate = UnixTime.Now;
            RemoteSize = remoteSize;
            RemoteCheckSum = remoteChecksum?.Contains("tmp") == true ? null : remoteChecksum;
        }

        public RemoteFile(string name, string fullname, UnixTime lastWrite, UnixTime lastUpdate, long remoteSize, string remoteChecksum)
        {
            Name = name;
            Fullname = fullname;
            LastWrite = lastWrite;
            LastUpdate = lastUpdate;
            RemoteSize = remoteSize;
            RemoteCheckSum = remoteChecksum;
        }
    }
}