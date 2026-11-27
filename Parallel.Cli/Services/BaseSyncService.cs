// Copyright 2026 Entex Interactive

using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Settings;
using Parallel.Core.Tasks;

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
    
    public abstract class BaseSyncService : BackgroundService
    {
        private readonly ILogger<BaseSyncService> _logger;
        
        protected readonly ConcurrentDictionary<string, SyncWorker> Vaults = new();
        protected readonly TaskQueuer _queuer;
        
        public BaseSyncService(ILogger<BaseSyncService> logger, TaskQueuer queuer)
        {
            _logger = logger;
            _queuer = queuer;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                LocalVaultConfig[] enabledVaults = ParallelConfig.GetEnabledVaults();
                string[] enabledVaultIds = enabledVaults.Select(v => v.Id).ToArray();
                string[] disabledVaults = Vaults.Keys.Where(v => !enabledVaultIds.Contains(v)).ToArray();

                // Adds newly enabled vaults to be synced.
                foreach (LocalVaultConfig vault in enabledVaults)
                {
                    if (Vaults.ContainsKey(vault.Id)) continue;

                    _logger.LogDebug("Adding vault to syncing...");
                    CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    Task workerTask = SyncVaultAsync(new FileSyncManager(vault), cts.Token);
                    SyncWorker worker = new SyncWorker(workerTask, cts);

                    if (!Vaults.TryAdd(vault.Id, worker)) continue;
                    _logger.LogInformation("Added vault to be synced: {VaultId}", vault.Id);
                }

                // Removes disabled vaults from being synced
                foreach (string id in disabledVaults)
                {
                    _logger.LogDebug("Removing vault from syncing...");
                    if (!Vaults.TryGetValue(id, out SyncWorker? worker)) continue;
                    await worker.Cts.CancelAsync();
                    await worker.WorkerTask;
                    Vaults.TryRemove(id, out _);

                    _logger.LogInformation("Removed vault from syncing: {VaultId}", id);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        protected abstract Task SyncVaultAsync(FileSyncManager syncManager, CancellationToken stoppingToken);
    }
}