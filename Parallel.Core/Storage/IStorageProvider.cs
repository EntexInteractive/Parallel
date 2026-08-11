// Copyright 2026 Entex Interactive

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Defines the way for communicating with a storage provider.
    /// </summary>
    public interface IStorageProvider : IDisposable
    {
        /// <summary>
        /// Checks the connection to the storage provider.
        /// </summary>
        /// <returns></returns>
        Task<bool> CheckConnectionAsync();

        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        Task CreateDirectoryAsync(string path);

        /// <summary>
        /// Deletes the specified directory.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        Task DeleteDirectoryAsync(string path);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        Task DeleteFileAsync(string path);

        /// <summary>
        /// Downloads a file from the associated storage provider.
        /// </summary>
        /// <param name="file">The source path in the <see cref="IStorageProvider"/>.</param>
        /// <param name="remotePath">The destination path where to download the file to.</param>
        /// <param name="ct"></param>
        Task<RemoteFile?> DownloadFileAsync(LocalFile file, string remotePath, CancellationToken ct = default);

        /// <summary>
        /// Checks if a path exists on the associated storage provider.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns>True if path exists, otherwise false.</returns>
        Task<bool> ExistsAsync(string path);

        /// <summary>
        /// Gets the specified directory path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string> GetDirectoryName(string path);

        /// <summary>
        /// Gets a file on the associated storage provider.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<RemoteFile?> GetFileAsync(string path);

        Task<string?> HashFileAsync(string remotePath, int bufferSize, CancellationToken ct);

        /// <summary>
        /// Uploads a file to the associated storage provider.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <param name="remotePath">The remote path in the <see cref="IStorageProvider"/>.</param>
        /// <param name="overwrite"></param>
        /// <param name="ct"></param>
        /// <returns>The size, in bytes, of the amount of data transferred. Otherwise, 0.</returns>
        Task<RemoteFile?> UploadFileAsync(LocalFile file, string remotePath, bool overwrite = false, CancellationToken ct = default);
    }
}