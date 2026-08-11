// Copyright 2026 Entex Interactive

using System.Reflection;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Storage;
using Parallel.Core.Utils;

namespace Parallel.Core.Settings
{
    /// <summary>
    /// Represents the configuration for the vault.
    /// </summary>
    public class RemoteVaultConfig : LocalVaultConfig
    {
        /// <summary>
        /// The amount of time, in minutes, to sync the system.
        /// <para>Default: 60 minutes</para>
        /// </summary>
        public int SyncInterval { get; set; } = 60;

        /// <summary>
        /// The amount of time, in days, to hold a file before it can be pruned.
        /// <para>Default: 180 days (6 months)</para>
        /// </summary>
        public int PrunePeriod { get; set; } = 180;

        /// <summary>
        /// A collection of directories to be pushed to vaults.
        /// </summary>
        public HashSet<string> PushDirectories { get; } = CreatePushDirectories();

        /// <summary>
        /// A collection of directories to be pulled when reconciling.
        /// </summary>
        public HashSet<PullRecord> PullDirectories { get; } = [];

        /// <summary>
        /// A collection of directories to be ignored when archiving or cleaning.
        /// </summary>
        public HashSet<string> IgnoreDirectories { get; } = CreateIgnoreDirectories();

        /// <summary>
        /// A collection of directories allowed to be pruned.
        /// <para>Recommended when using a cloud-based <see cref="FileService"/> to save on storage costs.</para>
        /// <para>Default: Empty</para>
        /// </summary>
        public HashSet<string> PruneDirectories { get; } = [];

        public RemoteVaultConfig(LocalVaultConfig localVault) : base(localVault.Id, localVault.Name, localVault.Credentials) { }

        [JsonConstructor]
        public RemoteVaultConfig(string id, string name, StorageCredentials credentials, int prunePeriod, IEnumerable<string>? pushDirectories, IEnumerable<PullRecord>? pullDirectories, IEnumerable<string>? ignoreDirectories, IEnumerable<string>? pruneDirectories) : base(id, name, credentials)
        {
            PrunePeriod = prunePeriod;
            PushDirectories = new HashSet<string>(pushDirectories ?? []);
            PullDirectories = new HashSet<PullRecord>(pullDirectories ?? []);
            IgnoreDirectories = new HashSet<string>(ignoreDirectories ?? []);
            PruneDirectories = new HashSet<string>(pruneDirectories ?? []);
        }

        #region Privates

        private static HashSet<string> CreatePushDirectories()
        {
            return
            [
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
            ];
        }

        private static HashSet<string> CreateIgnoreDirectories()
        {
            HashSet<string> list = new HashSet<string>();

            // Ignore folders on Windows machines
            if (OperatingSystem.IsWindows())
            {
                list.Add("$RECYCLE.BIN/"); // For NTFS file systems
                list.Add("*.lnk"); // Shortcuts to other paths
                list.Add("desktop.ini");
            }

            // Ignore folders on Linux machines
            if (OperatingSystem.IsLinux())
            {
                list.Add("lost+found/"); // File system recovery directory
                list.Add(".Trash/"); // User's trash folder
                list.Add("*.desktop"); // Linux shortcuts
            }

            // Ignore folders on Apple machines
            if (OperatingSystem.IsMacOS())
            {
                list.Add(".Trash/"); // User's trash folder
                list.Add("*.DS_Store"); // macOS Finder metadata
            }

            return list;
        }

        #endregion

        /// <summary>
        /// Loads settings from a file.
        /// </summary>
        public new static RemoteVaultConfig? Load(string path)
        {
            return !File.Exists(path) ? null : JsonConvert.DeserializeObject<RemoteVaultConfig>(File.ReadAllText(path));
        }

        public void Save(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}