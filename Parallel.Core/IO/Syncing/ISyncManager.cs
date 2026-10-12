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
        /// Synchronizes a list of local files with the remote storage. Files will be uploaded if they differ from their remote counterpart or if they do not exist in the remote storage.
        /// </summary>
        /// <param name="files">A read-only list of local files to be considered for synchronization.</param>
        /// <param name="progress">An <see cref="IProgressReporter"/> instance to report the progress of the sync operation.</param>
        /// <param name="overwrite">A boolean flag indicating whether existing files on the remote storage should be overwritten.</param>
        /// <returns>Returns the number of files that were successfully synchronized.</returns>
        Task<int> SyncFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress, bool overwrite);

        /// <summary>
        /// Restores the specified list of local files from a remote storage, updating the progress through the provided progress reporter.
        /// </summary>
        /// <param name="files">A read-only list of local files to be considered for restoration.</param>
        /// <param name="progress">An <see cref="IProgressReporter"/> instance to report the progress of the restore operation.</param>
        /// <returns>Returns the number of files that were successfully restored.</returns>
        Task<int> RestoreFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress);

        /// <summary>
        /// Removes unnecessary or transient files from the provided list of local files to clean up storage, retaining only essential data.
        /// </summary>
        /// <param name="files">A read-only list of local files to be considered for pruning.</param>
        /// <param name="progress">An <see cref="IProgressReporter"/> instance to report the progress of the pruning operation.</param>
        /// <returns>Returns the number of files that were successfully pruned.</returns>
        Task<int> PruneFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress);

        /// <summary>
        /// Scrubs the specified files to ensure they are in a valid state.
        /// </summary>
        /// <param name="files">A read-only list of local files to be considered for scrubbing.</param>
        /// <param name="progress">An <see cref="IProgressReporter"/> instance to report the progress of the scrubbing operation.</param>
        /// <returns>Returns the number of files that were successfully scrubbed.</returns>
        Task<int> ScrubFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress);
    }
}