// Copyright 2026 Entex Interactive

using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Storage;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Defines the methods needed for backing up a file system.
    /// </summary>
    public interface ISyncManager
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the local vault configuration.
        /// </summary>
        LocalVaultConfig LocalVault { get; }

        /// <summary>
        /// Gets the remote vault configuration.
        /// </summary>
        RemoteVaultConfig RemoteVault { get; }

        /// <summary>
        /// The associated database connection.
        /// </summary>
        IDatabase? Database { get; set; }

        /// <summary>
        /// The associated file system connection.
        /// </summary>
        IStorageProvider StorageProvider { get; set; }

        /// <summary>
        /// Establishes a connection to the associated <see cref="IStorageProvider"/> and downloads the needed files.
        /// </summary>
        Task<bool> ConnectAsync(bool force = false);

        /// <summary>
        /// Closes the current connection and releases its resources.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Backs up an array of files to a vault.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        /// <param name="overwrite"></param>
        Task<int> PushFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress, bool overwrite);

        /// <summary>
        /// Restores an array of files from a vault.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        Task<int> PullFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress);

        /// <summary>
        /// Deletes files from a vault.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        Task<int> PruneFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress);

        Task<int> ScrubFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress);
    }
}