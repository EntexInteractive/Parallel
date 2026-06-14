// Copyright 2026 Entex Interactive

using System.Diagnostics;
using System.IO.Compression;
using System.IO.Pipelines;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Renci.SshNet.Sftp;
using ZstdSharp;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Represents the wrapper for S3 compatible object storage.
    /// </summary>
    public class S3StorageProvider : IStorageProvider
    {
        private readonly AmazonS3Client _client;
        private readonly string _bucket;

        /// <summary>
        /// Represents an <see cref="IStorageProvider"/> for interacting with an S3 provider.
        /// </summary>
        /// <param name="localVault">The credentials to log in with.</param>
        public S3StorageProvider(LocalVaultConfig localVault)
        {
            AmazonS3Config config = new AmazonS3Config()
            {
                ServiceURL = localVault.Credentials.Address,
                ForcePathStyle = localVault.Credentials.ForceStyle,
                AuthenticationRegion = localVault.Credentials.Region,
                DisableS3ExpressSessionAuth = true,
                UseHttp = localVault.Credentials.Address?.StartsWith("http", StringComparison.OrdinalIgnoreCase) ?? false
            };

            _client = new AmazonS3Client(localVault.Credentials.Username, Encryption.Decode(localVault.Credentials.Password), config);
            _bucket = localVault.Credentials.RootDirectory.ToLower();
        }

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                ListBucketsResponse? response = await _client.ListBucketsAsync();
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        public async Task CreateDirectoryAsync(string path)
        {
            if (!path.EndsWith("/")) path += "/";
            await _client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = path,
                ContentBody = string.Empty
            });
        }

        public Task DeleteDirectoryAsync(string path)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteFileAsync(string path)
        {
            if (!await ExistsAsync(path)) return;
            await _client.DeleteObjectAsync(_bucket, path);
        }

        public async Task<RemoteFile?> DownloadFileAsync(LocalFile file, string remotePath, CancellationToken ct = default)
        {
            if (!await ExistsAsync(remotePath)) return null;

            using GetObjectResponse? response = await _client.GetObjectAsync(_bucket, remotePath, ct);
            await using FileStream createStream = File.Create(file.Fullname);
            await using ZstdStream zstdStream = new(response.ResponseStream, ZstdStreamMode.Decompress);
            await zstdStream.CopyToAsync(createStream, ct);
            return await GetFileAsync(remotePath);
        }

        public async Task<bool> ExistsAsync(string path)
        {
            ListObjectsV2Response? response = await _client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                Prefix = path,
                MaxKeys = 1
            });

            return response.KeyCount > 0;
        }

        public Task<string> GetDirectoryName(string path)
        {
            int index = path.LastIndexOf('/');
            return Task.FromResult(index >= 0 ? path[..index] : string.Empty);
        }

        public async Task<RemoteFile?> GetFileAsync(string path)
        {
            try
            {
                if (!await ExistsAsync(path)) return null;
                string checksum256 = Path.GetFileName(path);
                GetObjectMetadataResponse metadata = await _client.GetObjectMetadataAsync(_bucket, path);
                return new RemoteFile(checksum256, path, metadata.LastModified.GetValueOrDefault(), metadata.ContentLength, checksum256);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public Task<string?> HashFileAsync(string remotePath, int bufferSize, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async Task<RemoteFile?> UploadFileAsync(LocalFile file, string remotePath, bool overwrite = false, CancellationToken ct = default)
        {
            Stopwatch sw = Stopwatch.StartNew();
            if (!overwrite && await ExistsAsync(remotePath))
            {
                Log.Debug("Skipping file: {RemotePath}", remotePath);
                return await GetFileAsync(remotePath);
            }

            Pipe pipe = new Pipe();
            TransferUtility utility = new TransferUtility(_client);
            Task uploadTask = utility.UploadAsync(new TransferUtilityUploadRequest
            {
                BucketName = _bucket,
                Key = remotePath,
                InputStream = pipe.Reader.AsStream(),
                ContentType = "application/octet-stream"
            }, ct);

            long totalBytes = 0;
            await using HashStream hashStream = new(pipe.Writer.AsStream(), b => totalBytes = b);
            await using FileStream openStream = File.OpenRead(file.Fullname);
            await using (ZstdStream zstdStream = new(hashStream, ZstdStreamMode.Compress))
            {
                await openStream.CopyToAsync(zstdStream, ct);
                await zstdStream.FlushAsync(ct);
            }

            await pipe.Writer.CompleteAsync();
            await uploadTask.ConfigureAwait(false);

            string remoteChecksum = hashStream.GetHashHexString();
            Log.Information("Uploaded file: {SourcePath} ({RemoteChecksum})", file.Fullname, remoteChecksum);
            return new RemoteFile(file.Name, remotePath, file.LastWrite, file.LastUpdate, totalBytes, remoteChecksum);
        }
    }
}