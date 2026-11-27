// Copyright 2026 Entex Interactive

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Parallel.Core.IO.Scanning;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Tasks;

namespace Parallel.Cli.Services
{
    public class WatcherSyncService : BaseSyncService
    {
        private readonly ILogger<WatcherSyncService> _logger;
        private readonly ConcurrentBag<FileSystemWatcher> _watchers = new ConcurrentBag<FileSystemWatcher>();
        
        public WatcherSyncService(ILogger<WatcherSyncService> logger, TaskQueuer queuer) : base(logger, queuer)
        {
            _logger = logger;
        }

        protected override async Task SyncVaultAsync(FileSyncManager syncManager, CancellationToken stoppingToken)
        {
            foreach (string path in syncManager.RemoteVault.SyncDirectories)
            {
                _logger.LogDebug("Watching directory: {Path}", path);
                FileSystemWatcher watcher = new FileSystemWatcher()
                {
                    Path = path,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                    InternalBufferSize = 32 * 1024,
                    EnableRaisingEvents = true
                };
                
                watcher.Created += OnCreated;
                watcher.Changed += OnChanged;
                watcher.Deleted += OnDeleted;
                watcher.Renamed += OnRenamed;
                watcher.Error += OnError;
                _watchers.Add(watcher);
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (FileScanner.IsDirectory(e.FullPath)) return;
            
            _logger.LogDebug("Created file: {Path}", e.FullPath);
        }
        
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (FileScanner.IsDirectory(e.FullPath)) return;
            _logger.LogDebug("Changed file: {Path}", e.FullPath);
        }
        
        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (FileScanner.IsDirectory(e.FullPath)) return;
            _logger.LogDebug("Deleted file: {Path}", e.FullPath);
        }
        
        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (FileScanner.IsDirectory(e.FullPath)) return;
            _logger.LogDebug("Renamed file: {Path}", e.FullPath);
        }
        
        private void OnError(object sender, ErrorEventArgs e)
        {
            _logger.LogError("Exception throw: {Exception}", e.GetException().ToString());
        }
    }
}