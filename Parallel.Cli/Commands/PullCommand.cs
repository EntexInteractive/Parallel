// Copyright 2026 Kyle Ebbinga

using System.Collections.Concurrent;
using System.CommandLine;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Parallel.Cli.Utils;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO;
using Parallel.Core.IO.Scanning;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class PullCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();

        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to restore.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<DateTime> _beforeOpt = new(["--before"], "Pulls files before a certain timestamp.");
        private readonly Option<string> _remapOpt = new(["--remap"], "The new directory to map restored files to.");
        private readonly Option<bool> _forceOpt = new(["--force", "-f"], "Forces restoring, bypassing safe guards.");
        private readonly Option<bool> _dryRunOpt = new(["--dry-run"], "Previews the command without executing it.");
        private readonly Option<bool> _verboseOpt = new(["--verbose", "-v"], "Shows verbose output.");

        public PullCommand() : base("restore", "Pulls files from the backup.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.AddOption(_beforeOpt);
            this.AddOption(_remapOpt);
            this.AddOption(_forceOpt);
            this.AddOption(_dryRunOpt);
            this.AddOption(_verboseOpt);
            this.SetHandler(HandlePullAsync, _sourceOpt, _configOpt, _beforeOpt, _remapOpt, _forceOpt, _verboseOpt, _dryRunOpt);
        }

        private async Task HandlePullAsync(string? path, string? config, DateTime before, string? remap, bool force, bool verbose, bool dryRun)
        {
            _sw = Stopwatch.StartNew();
            DateTime timestamp = before != DateTime.MinValue ? before.AddMinutes(1).AddTicks(-1) : DateTime.Now;
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await PullPathAsync(localVault, path, timestamp, remap, force, verbose, dryRun);
                }
                else
                {
                    await PullSystemAsync(localVault, timestamp, remap, force, verbose, dryRun);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await Program.Settings.ForEachVaultAsync(vault => PullPathAsync(vault, path, timestamp, remap, force, verbose, dryRun));
                }
                else
                {
                    await Program.Settings.ForEachVaultAsync(vault => PullSystemAsync(vault, timestamp, remap, force, verbose, dryRun));
                }
            }
        }

        private async Task PullSystemAsync(LocalVaultConfig vault, DateTime timestamp, string? output, bool force, bool verbose, bool dryRun)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            foreach (string path in syncManager.RemoteVault.BackupDirectories)
            {
                await PullInternalAsync(syncManager, path, timestamp, output, force, verbose, dryRun);
            }
        }

        private async Task PullPathAsync(LocalVaultConfig vault, string path, DateTime timestamp, string? output, bool force, bool verbose, bool dryRun)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            await PullInternalAsync(syncManager, path, timestamp, output, force, verbose, dryRun);
        }

        private async Task PullInternalAsync(ISyncManager syncManager, string path, DateTime timestamp, string? output, bool force, bool verbose, bool dryRun)
        {
            CommandLine.WriteLine(syncManager.RemoteVault, $"Scanning for files in {path}...", ConsoleColor.DarkGray);
            IReadOnlyList<LocalFile> files = await (syncManager.Database?.GetLatestFilesAsync(path, timestamp) ?? Task.FromResult<IReadOnlyList<LocalFile>>([]));
            Log.Debug($"GetLatestFilesAsync returned {files.Count} files for path '{path}'");

            ConcurrentBag<LocalFile> restoreFiles = new();
            System.Threading.Tasks.Parallel.ForEach(files, ParallelConfig.Options, (file) =>
            {
                //string sourcePath = file.Fullname;
                //string outputPath = string.IsNullOrEmpty(output) ? sourcePath : PathBuilder.ReplacePath(sourcePath, path, output);
                string outputPath = PathBuilder.ReplacePath(file.Fullname, path, output);
                if (File.Exists(outputPath) && !FileScanner.HasChanged(file, new LocalFile(outputPath)) && !force) return;
                
                file.Fullname = outputPath;
                restoreFiles.Add(file);
            });

            if (restoreFiles.Count == 0)
            {
                CommandLine.WriteLine(syncManager.RemoteVault, $"The provided {(PathBuilder.IsFile(path) ? "file" : "folder")} is already up to date.", ConsoleColor.Green);
                return;
            }

            if (dryRun)
            {
                string fileName = PathBuilder.TempFile;
                await File.WriteAllLinesAsync(fileName, restoreFiles.Select(f => f.Fullname).OrderBy(f => f));
                CommandLine.WriteLine($"This operation will restore {restoreFiles.Count:N0} files into: {(string.IsNullOrEmpty(output) ? path : output)}", ConsoleColor.Green);
                CommandLine.WriteLine($"A detailed list can be found here: {fileName}", ConsoleColor.DarkGray);
            }
            else
            {
                CommandLine.WriteLine(syncManager.RemoteVault, $"Pulling {restoreFiles.Count:N0} files...", ConsoleColor.DarkGray);
                IProgressReporter progressReporter = verbose ? new ProgressReporter(syncManager.RemoteVault, restoreFiles.Count) : new LoggingProgressReporter(syncManager.RemoteVault); 
                int pulledFiles = await syncManager.PullFilesAsync(restoreFiles.ToArray(), progressReporter);
                
                CommandLine.WriteLine(syncManager.RemoteVault, $"Successfully pulled {pulledFiles:N0} files in {_sw.Elapsed}.", ConsoleColor.Green);   
                await syncManager.DisconnectAsync();
            }
        }
    }
}