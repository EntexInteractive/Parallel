// Copyright 2026 Entex Interactive

using System.Data;
using System.Security.Cryptography;
using Parallel.Core.Data;
using Parallel.Core.Diagnostics;
using Parallel.Core.Security;
using Parallel.Core.Utils;
using ZstdSharp;

namespace Parallel.Core.Models
{
    /// <summary>
    /// Represents a file managed by Parallel.
    /// </summary>
    public class LocalFile
    {
        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The path of the file on the local machine.
        /// </summary>
        public string Fullname { get; set; } = string.Empty;

        /// <summary>
        /// The path of the parent directory in the backup file system.
        /// </summary>
        public string? ParentDirectory { get; set; } = null;

        /// <summary>
        /// The time the current file was last written to.
        /// </summary>
        public UnixTime LastWrite { get; set; } = UnixTime.Now;

        /// <summary>
        /// The time the file was either last saved or deleted.
        /// </summary>
        public UnixTime LastUpdate { get; set; } = UnixTime.Now;

        /// <summary>
        /// The size, in bytes, of the file on the local machine.
        /// </summary>
        public long LocalSize { get; set; } = 0;

        /// <summary>
        /// The size, in bytes, of the file in the remote backup.
        /// </summary>
        public long RemoteSize { get; set; } = 0;

        /// <summary>
        /// The category of the file.
        /// </summary>
        public FileCategory Type { get; } = FileCategory.Other;

        /// <summary>
        /// If the file is currently hidden on the local machine.
        /// </summary>
        public bool Hidden { get; } = false;

        /// <summary>
        /// If the file is currently read-only on the local machine.
        /// </summary>
        public bool ReadOnly { get; } = false;

        /// <summary>
        /// If the file is currently deleted on the local machine.
        /// </summary>
        public bool Deleted { get; set; } = false;

        /// <summary>
        /// The checksum used to check if the file has changed.
        /// </summary>
        public string? LocalCheckSum { get; set; } = string.Empty;

        /// <summary>
        /// The checksum used to check if the file was fully uploaded.
        /// </summary>
        public string? RemoteCheckSum { get; set; } = string.Empty;


        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFile"/> class with default properties.
        /// </summary>
        public LocalFile(string path)
        {
            Name = Path.GetFileName(path);
            Fullname = Path.GetFullPath(path);
            LastUpdate = UnixTime.Now;
            ParentDirectory = Path.GetDirectoryName(path);
            Type = FileTypes.GetFileCategory(Path.GetExtension(path));
            Deleted = !File.Exists(path);

            if (!File.Exists(path)) return;

            FileInfo fileInfo = new FileInfo(path);
            LocalSize = fileInfo.Length;
            RemoteSize = fileInfo.Length;
            LastWrite = new UnixTime(fileInfo.LastWriteTimeUtc);
            Hidden = fileInfo.Attributes.HasFlag(FileAttributes.Hidden);
            ReadOnly = fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFile"/> class.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="localpath"></param>
        /// <param name="remotepath"></param>
        /// <param name="lastwrite"></param>
        /// <param name="lastupdate"></param>
        /// <param name="localsize"></param>
        /// <param name="remotesize"></param>
        /// <param name="type"></param>
        /// <param name="hidden"></param>
        /// <param name="readOnly"></param>
        /// <param name="deleted"></param>
        /// <param name="encrypted"></param>
        /// <param name="salt"></param>
        /// <param name="iv"></param>
        /// <param name="checksum"></param>
        public LocalFile(string name, string fullname, string parentDir, long lastWrite, long lastUpdate, long localSize, long remoteSize, string type, long hidden, long readOnly, long deleted, string localCheckSum, string remoteCheckSum)
        {
            Name = name;
            Fullname = fullname;
            ParentDirectory = parentDir;
            LastWrite = UnixTime.FromMilliseconds(lastWrite);
            LastUpdate = UnixTime.FromMilliseconds(lastUpdate);
            LocalSize = localSize;
            RemoteSize = remoteSize;
            Hidden = Converter.ToBool(hidden);
            ReadOnly = Converter.ToBool(readOnly);
            Deleted = Converter.ToBool(deleted);
            LocalCheckSum = localCheckSum;
            RemoteCheckSum = remoteCheckSum;
        }

        public LocalFile(string name, long length, DateTime lastWriteTime)
        {
            Name = name;
            RemoteSize = length;
            LastWrite = new UnixTime(lastWriteTime);
        }

        //public LocalFile() { }

        /// <summary>
        /// Determines if this instance and another <see cref="LocalFile"/> have the same values.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True if equal, otherwise false.</returns>
        public bool Equals(LocalFile value)
        {
            bool?[] results =
            [
                this?.Name != null && value?.Name != null ? this.Name.Equals(value.Name) : (bool?)null,
                this?.Fullname != null && value?.Fullname != null ? this.Fullname.Equals(value.Fullname) : (bool?)null,
                this?.ParentDirectory != null && value?.ParentDirectory != null ? this.ParentDirectory.Equals(value.ParentDirectory) : (bool?)null,
                value?.LocalSize != null ? this.LocalSize.Equals(value.LocalSize) : (bool?)null,
                value?.RemoteSize != null ? this.RemoteSize.Equals(value.RemoteSize) : (bool?)null,
                value?.Type != null ? this.Type.Equals(value.Type) : (bool?)null,
                value?.Hidden != null ? this.Hidden.Equals(value.Hidden) : (bool?)null,
                value?.ReadOnly != null ? this.ReadOnly.Equals(value.ReadOnly) : (bool?)null,
                value?.Deleted != null ? this.Deleted.Equals(value.Deleted) : (bool?)null,
                this?.LocalCheckSum != null && value?.LocalCheckSum != null ? this.LocalCheckSum.Equals(value.LocalCheckSum) : (bool?)null,
            ];

            return results.All(b => b != null && (bool)b);
        }

        public bool TryGenerateLocalCheckSum()
        {
            if (!string.IsNullOrEmpty(LocalCheckSum)) return true;

            try
            {
                if (!File.Exists(Fullname)) return false;

                using SHA256 sha256 = SHA256.Create();
                using FileStream fs = File.OpenRead(Fullname);
                
                fs.Position = 0;
                LocalCheckSum = Convert.ToHexStringLower(sha256.ComputeHash(fs));
                return !string.IsNullOrEmpty(LocalCheckSum);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while generating checksum");
                return false;
            }
        }
        
        public bool TryGenerateRemoteCheckSum()
        {
            if (!string.IsNullOrEmpty(RemoteCheckSum)) return true;

            try
            {
                if (!File.Exists(Fullname)) return false;

                using SHA256 sha256 = SHA256.Create();
                using FileStream fs = File.OpenRead(Fullname);
                using HashStream hs = new(Stream.Null);
                using (ZstdStream zstd = new(hs, ZstdStreamMode.Compress))
                {
                    fs.CopyTo(zstd);
                }

                RemoteCheckSum = hs.GetHashHexString();
                return !string.IsNullOrEmpty(RemoteCheckSum);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while generating checksum");
                return false;
            }
        }

        private LocalFile(LocalFile file, long remoteSize, string? remoteCheckSum)
        {
            Name = file.Name;
            Fullname = file.Fullname;
            ParentDirectory = file.ParentDirectory;
            LastWrite = file.LastWrite;
            LastUpdate = file.LastUpdate;
            LocalSize = file.LocalSize;
            RemoteSize = remoteSize;
            Hidden = file.Hidden;
            ReadOnly = file.ReadOnly;
            Deleted = file.Deleted;
            LocalCheckSum = file.LocalCheckSum;
            RemoteCheckSum = remoteCheckSum;
        }

        public LocalFile AppendFile(RemoteFile file)
        {
            return new LocalFile(this, file.RemoteSize, file.RemoteCheckSum);
        }
    }
}