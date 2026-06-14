// Copyright 2026 Entex Interactive

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.Database.Contexts;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Storage;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Represents the base functionality for syncing files to an associated file system.
    /// </summary>
    public abstract class BaseSyncManager : ISyncManager
    {
        protected string TempDirectory = PathBuilder.TempDirectory;
        protected string TempConfigFile => Path.Combine(TempDirectory, $"{LocalVault.Id}.json");
        protected string TempDbFile => Path.Combine(TempDirectory, $"{LocalVault.Id}.db");

        /// <inheritdoc />
        public string Id => LocalVault.Id;

        /// <inheritdoc />
        public LocalVaultConfig LocalVault { get; private set; }

        /// <inheritdoc />
        public RemoteVaultConfig RemoteVault { get; private set; }

        /// <inheritdoc />
        public IDatabase? Database { get; set; }

        /// <inheritdoc />
        public IStorageProvider StorageProvider { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localVault"></param>
        protected BaseSyncManager(LocalVaultConfig localVault)
        {
            LocalVault = localVault;
            RemoteVault = new RemoteVaultConfig(localVault);
            StorageProvider = StorageConnection.CreateNew(localVault);
        }

        /// <inheritdoc />
        public async Task<bool> ConnectAsync(bool force = false)
        {
            // Checks if a valid connection can be made
            if (!await StorageProvider.CheckConnectionAsync())
            {
                Log.Error("[{LocalVaultId}] Failed to connect to vault!", LocalVault.Id);
                return false;
            }

            string root = PathBuilder.GetRootDirectory(LocalVault);
            if (!await StorageProvider.ExistsAsync(root))
            {
                await StorageProvider.CreateDirectoryAsync(root);
                Log.Debug("Created root directory: {Root}", root);
            }

            // Checks temp files for a local download of the config file
            if (!File.Exists(TempConfigFile) || File.GetLastWriteTimeUtc(TempConfigFile) <= DateTime.UtcNow.AddHours(-6) || force)
            {
                Log.Debug($"No local config file found. Fetching...");
                if (!await StorageProvider.ExistsAsync(PathBuilder.GetConfigurationFile(LocalVault)))
                {
                    RemoteVault = new RemoteVaultConfig(LocalVault);
                    RemoteVault.IgnoreDirectories.Add(PathBuilder.GetRootDirectory(LocalVault));
                    RemoteVault.Save(TempConfigFile);

                    Log.Debug("Created config file: {TempConfigFile}", TempConfigFile);
                }
                else
                {
                    await StorageProvider.DownloadFileAsync(new LocalFile(TempConfigFile), PathBuilder.GetConfigurationFile(LocalVault));
                    Log.Debug("Downloaded file: {TempConfigFile}", TempConfigFile);
                }
            }

            // Checks temp files for a local download of the database file
            if (!File.Exists(TempDbFile) || File.GetLastWriteTimeUtc(TempDbFile) <= DateTime.UtcNow.AddHours(-3) || force)
            {
                Log.Debug($"No local database file found. Fetching...");
                if (!await StorageProvider.ExistsAsync(PathBuilder.GetDatabaseFile(LocalVault)))
                {
                    Log.Debug("Create db file: {TempDbFile}", TempDbFile);
                    if (File.Exists(TempDbFile)) File.Delete(TempDbFile);
                    Database = new SqliteContext(TempDbFile);
                    await Database.InitializeAsync();
                }
                else
                {
                    string remoteDbFile = PathBuilder.GetDatabaseFile(LocalVault);
                    await StorageProvider.DownloadFileAsync(new LocalFile(TempDbFile), remoteDbFile);
                    Log.Debug("Downloaded file: {TempDbFile}", TempDbFile);
                }
            }

            // Load the temp files
            Database = new SqliteContext(TempDbFile);
            RemoteVaultConfig? config = RemoteVaultConfig.Load(TempConfigFile);
            if (config == null) return false;
            RemoteVault = config;
            LocalVault.Name = config.Name;

            Log.Information("[{LocalVaultId}] Connected", LocalVault.Id);
            return true;
        }

        /// <inheritdoc />
        public async Task DisconnectAsync()
        {
            Log.Debug("[{LocalVaultId}] Disconnecting...", LocalVault.Id);
            RemoteVault.Save(TempConfigFile);

            await StorageProvider.UploadFileAsync(new LocalFile(TempConfigFile), PathBuilder.GetConfigurationFile(LocalVault), true);
            await StorageProvider.UploadFileAsync(new LocalFile(TempDbFile), PathBuilder.GetDatabaseFile(LocalVault), true);

            StorageProvider?.Dispose();
            Log.Information("[{LocalVaultId}] Disconnected", LocalVault.Id);
        }

        /// <inheritdoc />
        public abstract Task<int> PushFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress, bool overwrite);

        /// <inheritdoc />
        public abstract Task<int> PullFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress);

        /// <inheritdoc />
        public abstract Task<int> PruneFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress);

        public abstract Task<int> ScrubFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress);
    }
}