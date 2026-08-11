// Copyright 2026 Entex Interactive

using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Settings;

namespace Parallel.Cli.Services
{
    public class SyncWorker
    {
        public CancellationTokenSource Cts { get; }
        public Task WorkerTask { get; }

        public SyncWorker(Task task, CancellationTokenSource cts)
        {
            WorkerTask = task;
            Cts = cts;
        }
    }
    
    public class SyncService : BackgroundService
    {
        private readonly ConcurrentDictionary<string, SyncWorker> _vaults = new();
        private readonly ILogger<SyncService> _logger;
        //private readonly TaskQueuer _queuer;
        
        public SyncService(ILogger<SyncService> logger)
        {
            _logger = logger;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                LocalVaultConfig[] enabledVaults = ParallelConfig.GetEnabledVaults();
                string[] enabledVaultIds = enabledVaults.Select(v => v.Id).ToArray();
                string[] disabledVaults = _vaults.Keys.Where(v => !enabledVaultIds.Contains(v)).ToArray();

                // Adds newly enabled vaults to be synced.
                foreach (LocalVaultConfig vault in enabledVaults)
                {
                    if (_vaults.ContainsKey(vault.Id)) continue;

                    _logger.LogDebug("Adding vault to syncing...");
                    CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    Task workerTask = SyncVaultAsync(new FileSyncManager(vault), cts.Token);
                    SyncWorker worker = new SyncWorker(workerTask, cts);

                    if (!_vaults.TryAdd(vault.Id, worker)) continue;
                    _logger.LogInformation("Added vault to be synced: {VaultId}", vault.Id);
                }

                // Removes disabled vaults from being synced
                foreach (string id in disabledVaults)
                {
                    _logger.LogDebug("Removing vault from syncing...");
                    if (!_vaults.TryGetValue(id, out SyncWorker? worker)) continue;
                    await worker.Cts.CancelAsync();
                    await worker.WorkerTask;
                    _vaults.TryRemove(id, out _);

                    _logger.LogInformation("Removed vault from syncing: {VaultId}", id);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task SyncVaultAsync(FileSyncManager syncManager, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Log.Debug($"{syncManager.Id} will sync every {syncManager.RemoteVault.SyncInterval} minutes");
                //await Task.Delay(TimeSpan.FromMinutes(syncManager.RemoteVault.SyncInterval), stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}