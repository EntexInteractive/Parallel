// Copyright 2026 Kyle Ebbinga

using System.Collections.Concurrent;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Represents the way to sync whole files to an associated file system.
    /// </summary>
    public class FileSyncManager : BaseSyncManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSyncManager"/> class.
        /// </summary>
        /// <param name="localVault"></param>
        public FileSyncManager(LocalVaultConfig localVault) : base(localVault) { }

        /// <inheritdoc/>
        public override async Task<int> PushFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress, bool overwrite)
        {
            if (!files.Any()) return 0;
            int completed = 0;

            ConcurrentBag<string> cleanupFiles = [];
            ConcurrentDictionary<string, SemaphoreSlim> threadPool = new ConcurrentDictionary<string, SemaphoreSlim>();
            LocalFile[] uploadFiles = files.Where(f => !f.Deleted).ToArray();
            LocalFile[] deletedFiles = files.Except(uploadFiles).ToArray();

            Log.Information("Uploading {UploadFilesLength:N0} files...", uploadFiles.Length);
            await System.Threading.Tasks.Parallel.ForEachAsync(uploadFiles, ParallelConfig.Options, async (file, ct) =>
            {
                if (!file.TryGenerateCheckSums()) return;

                SemaphoreSlim threadLock = threadPool.GetOrAdd(file.RemoteCheckSum!, _ => new SemaphoreSlim(1, 1));
                string remotePath = PathBuilder.GetObjectFile(RemoteVault, file.RemoteCheckSum!);
                await threadLock.WaitAsync(ct);

                try
                {
                    RemoteFile? remoteFile = await StorageProvider.UploadFileAsync(file, remotePath, overwrite, ct);
                    if (remoteFile != null && file.RemoteCheckSum == remoteFile.RemoteCheckSum)
                    {
                        LocalFile localFile = file.AppendFile(remoteFile);
                        if (!await (Database?.AddHistoryAsync(HistoryType.Synced, localFile) ?? Task.FromResult(false))) Log.Error("Failed to add history: {Fullname}", localFile.Fullname);
                        if (!await (Database?.AddFileAsync(localFile) ?? Task.FromResult(false))) Log.Error("Failed to add file: {Fullname}", localFile.Fullname);
                        progress.Report(ProgressOperation.Synced, localFile);
                        Interlocked.Increment(ref completed);
                    }
                    else
                    {
                        Log.Warning("File failed during upload: {Fullname}", file.Fullname);
                        cleanupFiles.Add(remotePath + ".tmp");
                    }
                }
                catch (Exception ex)
                {
                    progress.Failed(file, ex.GetBaseException().ToString());
                    cleanupFiles.Add(remotePath);
                }
                finally
                {
                    threadLock.Release();
                }
            });

            Log.Information("Archiving {DeletedFilesLength:N0} files...", deletedFiles.Length);
            await System.Threading.Tasks.Parallel.ForEachAsync(deletedFiles, ParallelConfig.Options, async (file, ct) =>
            {
                if (!await (Database?.AddHistoryAsync(HistoryType.Archived, file) ?? Task.FromResult(false))) Log.Error("Failed to add history: {Fullname}", file.Fullname);
                if (!await (Database?.AddFileAsync(file) ?? Task.FromResult(false))) Log.Error("Failed to add file: {Fullname}", file.Fullname);
                progress.Report(ProgressOperation.Archived, file);
                Interlocked.Increment(ref completed);
            });

            Log.Information("Cleaning up {CleanFilesLength:N0} files...", cleanupFiles.Count);
            foreach (string path in cleanupFiles) await StorageProvider.DeleteFileAsync(path);
            return completed;
        }

        /// <inheritdoc/>
        public override async Task<int> PullFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress)
        {
            if (!files.Any()) return 0;
            int completed = 0;

            ConcurrentBag<string> cleanupFiles = [];
            ConcurrentDictionary<string, SemaphoreSlim> threadPool = new ConcurrentDictionary<string, SemaphoreSlim>();
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                SemaphoreSlim threadLock = threadPool.GetOrAdd(file.LocalCheckSum!, _ => new SemaphoreSlim(1, 1));
                await threadLock.WaitAsync(ct);

                try
                {
                    string? parentDir = Path.GetDirectoryName(file.Fullname);
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

                    Log.Debug($"Restoring file: {file.Fullname} ({file.RemoteCheckSum})");
                    RemoteFile? remoteFile = await StorageProvider.DownloadFileAsync(file, PathBuilder.GetObjectFile(RemoteVault, file.RemoteCheckSum!), ct);
                    if (remoteFile == null || file.LocalCheckSum != remoteFile.RemoteCheckSum)
                    {
                        progress.Failed(file, "File not found!");
                        cleanupFiles.Add(file.Fullname);
                        return;
                    }

                    FileInfo fileInfo = new(file.Fullname);
                    FileAttributes attributes = fileInfo.Attributes;
                    if (file.ReadOnly) attributes |= FileAttributes.ReadOnly;
                    if (file.Hidden) attributes |= FileAttributes.Hidden;
                    fileInfo.LastWriteTime = file.LastWrite.ToLocalTime();
                    fileInfo.Attributes = attributes;

                    if (!await (Database?.AddHistoryAsync(HistoryType.Restored, file) ?? Task.FromResult(false))) Log.Error("Failed to add history: {Fullname}", file.Fullname);
                    progress.Report(ProgressOperation.Restored, file);
                    Interlocked.Increment(ref completed);
                }
                catch (Exception ex)
                {
                    progress.Failed(file, ex.GetBaseException().ToString());
                    cleanupFiles.Add(file.Fullname);
                }
                finally
                {
                    threadLock.Release();
                }
            });
            
            Log.Information("Cleaning up {CleanFilesLength:N0} files...", cleanupFiles.Count);
            foreach (string path in cleanupFiles) if(File.Exists(path)) File.Delete(path);
            return completed;
        }

        /// <inheritdoc/>
        public override async Task<int> PruneFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress)
        {
            if (!files.Any()) return 0;
            int completed = 0;

            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                try
                {
                    if (!await (Database?.RemoveFileAsync(file) ?? Task.FromResult(false))) return;
                    if (!await (Database?.AddHistoryAsync(HistoryType.Pruned, file) ?? Task.FromResult(false))) Log.Error("Failed to add history: {Fullname}", file.Fullname);
                    await StorageProvider.DeleteFileAsync(PathBuilder.GetObjectFile(RemoteVault, file.RemoteCheckSum!));
                    progress.Report(ProgressOperation.Pruned, file);
                }
                catch (Exception ex)
                {
                    progress.Failed(file, ex.GetBaseException().ToString());
                }
                finally
                {
                    Interlocked.Increment(ref completed);
                }
            });
            
            return completed;
        }

        public override async Task<int> ScrubFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress)
        {
            if (!files.Any()) return 0;
            int completed = 0;
            
            ConcurrentDictionary<string, SemaphoreSlim> threadPool = new ConcurrentDictionary<string, SemaphoreSlim>();
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                SemaphoreSlim threadLock = threadPool.GetOrAdd(file.LocalCheckSum!, _ => new SemaphoreSlim(1, 1));
                await threadLock.WaitAsync(ct);

                try
                {
                    string? remoteHash = await StorageProvider.HashFileAsync(PathBuilder.GetObjectFile(RemoteVault, file.RemoteCheckSum!), 81920, ct);
                    if (string.IsNullOrEmpty(remoteHash))
                    {
                        // This is true when the remote file is not in the vault.
                        progress.Failed(file, "File not found!");
                        return;
                    }

                    if (file.RemoteCheckSum != remoteHash)
                    {
                        // Is it true is the file stored is corrupted
                        progress.Failed(file, "File corrupted!");
                        return;
                    }
                    
                    progress.Report(ProgressOperation.Scrubbed, file);
                    Interlocked.Increment(ref completed);
                }
                catch (Exception ex)
                {
                    progress.Failed(file, ex.GetBaseException().ToString());
                }
                finally
                {
                    threadLock.Release();
                }
            });
            
            return completed;
        }
    }
}