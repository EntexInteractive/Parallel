// Copyright 2026 Entex Interactive

using Parallel.Core.IO;
using System;
using System.Data;
using Parallel.Core.Models;

namespace Parallel.Core.Database
{
    /// <summary>
    /// The type of history to log.
    /// </summary>
    public enum HistoryType
    {
        /// <summary>
        /// A file that has been deleted locally but is still backed up. 
        /// </summary>
        Archived,

        /// <summary>
        /// A file that has been deleted locally.
        /// </summary>
        Cleaned,

        /// <summary>
        /// A file that has been copied to another location.
        /// </summary>
        Cloned,

        /// <summary>
        /// A file that has been deleted from the vault.
        /// </summary>
        Pruned,

        /// <summary>
        /// A file that was restored from the vault.
        /// </summary>
        Restored,

        /// <summary>
        /// A file that was synced to the vault.
        /// </summary>
        Synced
    }

    /// <summary>
    /// An interface for interacting with client data storage.
    /// </summary>
    public interface IDatabase
    {
        #region Base

        /// <summary>
        /// Called when initializing a new <see cref="IDatabase"/> instance.
        /// <para>Used for creating new table schemas.</para>
        /// </summary>
        Task InitializeAsync();

        #endregion

        #region Files

        /// <summary>
        /// Adds a new file or updates an existing one.
        /// </summary>
        /// <param name="file"></param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> AddFileAsync(LocalFile file);

        Task<bool> RemoveFileAsync(LocalFile file);

        /// <summary>
        /// Gets a list of files by newest revision.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        Task<IReadOnlyList<LocalFile>> GetLatestFilesAsync(string path, DateTime timestamp);

        /// <summary>
        /// Gets a list of files by newest revision.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="timestamp"></param>
        /// <param name="deleted"></param>
        /// <returns></returns>
        Task<IReadOnlyList<LocalFile>> GetLatestFilesAsync(string path, DateTime timestamp, bool deleted);

        /// <summary>
        /// Gets a list of files.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="timestamp"></param>
        /// <param name="deleted"></param>
        /// <returns></returns>
        Task<IReadOnlyList<LocalFile>> GetFilesAsync(string path, DateTime timestamp);

        /// <summary>
        /// Gets a list of files.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="timestamp"></param>
        /// <param name="deleted"></param>
        /// <returns></returns>
        Task<IReadOnlyList<LocalFile>> GetFilesAsync(string path, DateTime timestamp, bool deleted);

        /// <summary>
        /// Gets a specific file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<LocalFile?> GetFileAsync(string path);

        Task<long> GetCurrentSizeAsync();
        Task<long> GetRemoteSizeAsync();
        Task<long> GetTotalSizeAsync();
        Task<long> GetTotalFilesAsync();
        Task<long> GetTotalFilesAsync(bool deleted);
        Task<long> GetTotalRevisedFilesAsync();

        /// <summary>
        /// Gets a list of directories in ascending order.
        /// </summary>
        /// <param name="path"></param>
        Task<IReadOnlyList<string>> ListDirectoriesAsync(string path);

        /// <summary>
        /// Gets a list of files in a directory in ascending order. If multiple files exist, those will be in order of most recent.
        /// </summary>
        /// <param name="path"></param>
        Task<IReadOnlyList<LocalFile>> ListFilesAsync(string path);

        #endregion

        Task<DateTime> GetLastSyncTimeAsync();

        #region History

        /// <summary>
        /// Adds a new history.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="file"></param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> AddHistoryAsync(HistoryType type, LocalFile file);

        Task<IReadOnlyList<HistoryEvent>> GetHistoryAsync(string path);

        Task<IReadOnlyList<HistoryEvent>> GetHistoryAsync(string path, int limit);

        Task<IReadOnlyList<HistoryEvent>> GetHistoryAsync(string path, HistoryType? type);

        Task<IReadOnlyList<HistoryEvent>> GetHistoryAsync(string path, HistoryType? type, int limit);

        #endregion

        #region Snapshots

        Task<bool> AddSnapshotAsync(string snapshot);

        Task<IReadOnlyList<string>> GetSnapshotsAsync();

        Task<string?> GetSnapshotAsync(string? name);

        Task RemoveSnapshotAsync(string snapshot);

        #endregion
    }
}