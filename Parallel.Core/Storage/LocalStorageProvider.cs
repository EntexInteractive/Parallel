// Copyright 2026 Entex Interactive

using System.IO.Compression;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using ZstdSharp;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Represents the wrapper for a default dotnet file system.
    /// </summary>
    public class LocalStorageProvider : IStorageProvider
    {
        private readonly LocalVaultConfig _vaultConfig;

        /// <summary>
        /// Represents an <see cref="IStorageProvider"/> for interacting with physical machine hardware.
        /// </summary>
        /// <param name="vaultConfig">The vault to use.</param>
        public LocalStorageProvider(LocalVaultConfig vaultConfig)
        {
            _vaultConfig = vaultConfig;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public Task<bool> CheckConnectionAsync()
        {
            return Task.FromResult(Directory.Exists(_vaultConfig.Credentials.RootDirectory));
        }

        /// <inheritdoc/>
        public Task CreateDirectoryAsync(string path)
        {
            Directory.CreateDirectory(path);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteDirectoryAsync(string path)
        {
            //Directory.Delete(path);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFileAsync(string path)
        {
            if (!File.Exists(path)) return Task.CompletedTask;
            File.SetAttributes(path, ~FileAttributes.ReadOnly & File.GetAttributes(path));
            File.Delete(path);
            return Task.CompletedTask;
        }

        public async Task<RemoteFile?> DownloadFileAsync(LocalFile file, string remotePath, CancellationToken ct = default)
        {
            if (!File.Exists(remotePath)) return null;

            long totalBytes = 0;
            string remoteChecksum;

            await using FileStream openStream = File.OpenRead(remotePath);
            await using FileStream createStream = File.Create(file.Fullname);
            await using (ZstdStream zstdStream = new(openStream, ZstdStreamMode.Decompress))
            await using (HashStream hashStream = new(zstdStream, b => totalBytes = b))
            {
                await hashStream.CopyToAsync(createStream, ct);
                await hashStream.FlushAsync(ct);

                remoteChecksum = hashStream.GetHashHexString();
            }

            Log.Information("Downloaded file: {SourcePath} ({RemoteChecksum})", file.Fullname, remoteChecksum);
            return new RemoteFile(file.Name, file.Fullname, file.LastWrite, file.LastUpdate, totalBytes, remoteChecksum);
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string path)
        {
            return Task.FromResult(Directory.Exists(path) || File.Exists(path));
        }

        /// <inheritdoc />
        public Task<string> GetDirectoryName(string path)
        {
            return Task.FromResult(Path.GetDirectoryName(path) ?? string.Empty);
        }

        /// <inheritdoc/>
        public Task<RemoteFile?> GetFileAsync(string path)
        {
            if (!File.Exists(path)) return Task.FromResult<RemoteFile?>(null);

            FileInfo fi = new(path);
            RemoteFile file = new(fi.Name, path, fi.LastWriteTimeUtc, fi.Length, fi.Name);
            return Task.FromResult<RemoteFile?>(file);
        }

        /// <inheritdoc/>
        public async Task<string?> HashFileAsync(string remotePath, int bufferSize = 81920, CancellationToken ct = default)
        {
            if (!await ExistsAsync(remotePath))
            {
                Log.Debug("Skipping file: {RemotePath}", remotePath);
                return null;
            }

            await using FileStream stream = File.OpenRead(remotePath);
            using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            int bytesRead;
            byte[] buffer = new byte[bufferSize];
            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                sha.AppendData(buffer.AsSpan(0, bytesRead));
            }

            return Convert.ToHexStringLower(sha.GetHashAndReset());
        }

        private void InternalRenameFile(string sourcePath, string destPath)
        {
            if (!File.Exists(sourcePath)) return;

            FileAttributes sourceAttrs = File.GetAttributes(sourcePath);
            bool isReadOnly = (sourceAttrs & FileAttributes.ReadOnly) != 0;

            if (File.Exists(destPath))
            {
                FileAttributes destAttrs = File.GetAttributes(destPath);
                if ((destAttrs & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(destPath, destAttrs & ~FileAttributes.ReadOnly);
                }
            }

            File.Move(sourcePath, destPath, true);

            if (isReadOnly)
            {
                FileAttributes destAttrs = File.GetAttributes(destPath);
                File.SetAttributes(destPath, destAttrs | FileAttributes.ReadOnly);
            }
        }

        /// <inheritdoc />
        public async Task<RemoteFile?> UploadFileAsync(LocalFile file, string remotePath, bool overwrite = false, CancellationToken ct = default)
        {
            if (!overwrite && await ExistsAsync(remotePath))
            {
                Log.Debug("Skipping file: {RemotePath}", remotePath);
                return await GetFileAsync(remotePath);
            }

            string tempPath = remotePath + ".tmp";
            await CreateDirectoryAsync(await GetDirectoryName(remotePath));

            long totalBytes = 0;
            string remoteChecksum;

            await using (FileStream createStream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            await using (FileStream openStream = new(file.Fullname, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (HashStream hashStream = new(createStream, b => totalBytes = b))
            {
                await using (ZstdStream zstdStream = new(hashStream, ZstdStreamMode.Compress))
                {
                    await openStream.CopyToAsync(zstdStream, ct);
                    await zstdStream.FlushAsync(ct);
                }

                remoteChecksum = hashStream.GetHashHexString();
            }

            InternalRenameFile(tempPath, remotePath);

            Log.Information("Uploaded file: {SourcePath} ({RemoteChecksum})", file.Fullname, remoteChecksum);
            return new RemoteFile(file.Name, file.Fullname, file.LastWrite, file.LastUpdate, totalBytes, remoteChecksum);
        }
    }
}