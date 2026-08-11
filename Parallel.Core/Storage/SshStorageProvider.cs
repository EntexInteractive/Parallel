// Copyright 2026 Entex Interactive

using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using ZstdSharp;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Represents the wrapper for an SFTP file system through SSH.
    /// </summary>
    public class SshStorageProvider : IStorageProvider
    {
        private readonly ConnectionInfo _connectionInfo;
        private readonly SftpClient _client;

        /// <summary>
        /// Represents an <see cref="IStorageProvider"/> for interacting with an SSH server.
        /// </summary>
        /// <param name="localVault">The credentials to log in with.</param>
        public SshStorageProvider(LocalVaultConfig localVault)
        {
            _connectionInfo = new ConnectionInfo(localVault.Credentials.Address, localVault.Credentials.Username, new PasswordAuthenticationMethod(localVault.Credentials.Username, Encryption.Decode(localVault.Credentials.Password)));
            _client = new SftpClient(_connectionInfo);
        }

        private void InsureConnection()
        {
            if (!_client.IsConnected) _client.Connect();
            if (!_client.IsConnected) throw new SshConnectionException("Unable to connect to SSH server");
        }


        /// <inheritdoc />
        public void Dispose()
        {
            if (_client.IsConnected) _client.Disconnect();
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task<bool> CheckConnectionAsync()
        {
            InsureConnection();
            return Task.FromResult(_client.IsConnected);
        }

        /// <inheritdoc/>
        public async Task CreateDirectoryAsync(string path)
        {
            InsureConnection();
            string parentDir = string.Empty;
            foreach (string subPath in path.Split('/'))
            {
                parentDir += $"/{subPath}";
                if (!await _client.ExistsAsync(parentDir))
                {
                    await _client.CreateDirectoryAsync(parentDir);
                }
            }
        }

        /// <inheritdoc/>
        public async Task DeleteDirectoryAsync(string path)
        {
            InsureConnection();
            if (await ExistsAsync(path))
            {
                await _client.DeleteDirectoryAsync(path);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteFileAsync(string path)
        {
            InsureConnection();
            if (!await ExistsAsync(path)) return;
            await _client.DeleteAsync(path);
        }

        /// <inheritdoc />
        public async Task<RemoteFile?> DownloadFileAsync(LocalFile file, string remotePath, CancellationToken ct = default)
        {
            InsureConnection();
            if (!await ExistsAsync(remotePath)) return null;

            await using SftpFileStream openStream = _client.OpenRead(remotePath);
            await using FileStream createStream = File.Create(file.Fullname);
            await using ZstdStream zstdStream = new(openStream, ZstdStreamMode.Decompress);
            await zstdStream.CopyToAsync(createStream, ct);
            return await GetFileAsync(remotePath);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string path)
        {
            InsureConnection();
            return await _client.ExistsAsync(path);
        }

        /// <inheritdoc/>
        public Task<string> GetDirectoryName(string path)
        {
            InsureConnection();
            string[] subDirs = path.Split('/');
            return Task.FromResult(string.Join("/", subDirs.Take(subDirs.Length - 1)));
        }

        /// <inheritdoc/>
        public async Task<RemoteFile?> GetFileAsync(string path)
        {
            InsureConnection();
            if (!await ExistsAsync(path)) return null;

            ISftpFile sf = _client.Get(path);
            return new RemoteFile(sf.Name, sf.FullName, sf.LastWriteTimeUtc, sf.Length, sf.Name);
        }

        public Task<string?> HashFileAsync(string remotePath, int bufferSize, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async Task<RemoteFile?> UploadFileAsync(LocalFile file, string remotePath, bool overwrite = false, CancellationToken ct = default)
        {
            InsureConnection();
            if (!overwrite && await ExistsAsync(remotePath))
            {
                Log.Debug("Skipping file: {RemotePath}", remotePath);
                return await GetFileAsync(remotePath);
                //_client.ChangePermissions(remotePath, 644);
            }

            string tempPath = remotePath + ".tmp";
            await CreateDirectoryAsync(await GetDirectoryName(remotePath));

            long totalBytes = 0;
            await using SftpFileStream createStream = _client.Create(tempPath);
            await using HashStream hashStream = new(createStream, b => totalBytes = b);
            await using FileStream openStream = new(file.Fullname, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using (ZstdStream zstdStream = new(hashStream, ZstdStreamMode.Compress))
            {
                await openStream.CopyToAsync(zstdStream, 81920, ct);
                await zstdStream.FlushAsync(ct);
            }

            if (overwrite && await ExistsAsync(remotePath)) await _client.DeleteFileAsync(remotePath, ct);
            await _client.RenameFileAsync(tempPath, remotePath, ct);
            _client.ChangePermissions(remotePath, 444);

            string remoteChecksum = hashStream.GetHashHexString();
            Log.Information("Uploaded file: {SourcePath} ({RemoteChecksum})", file.Fullname, remoteChecksum);
            return new RemoteFile(file.Name, file.Fullname, file.LastWrite, file.LastUpdate, totalBytes, remoteChecksum);
        }
    }
}