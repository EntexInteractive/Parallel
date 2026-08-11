// Copyright 2026 Entex Interactive

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
    public class RestoreCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();

        private readonly Argument<string> _sourceArg = new("path", "The path to add or remove.");
        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to pull.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<DateTime> _beforeOpt = new(["--before"], "Pulls files before a certain timestamp.");
        private readonly Option<string> _destOpt = new(["--destination"], "The new directory to map pulled files to.");
        private readonly Option<bool> _archiveOpt = new(["--archive", "-a"], "Pulls only archived files.");
        private readonly Option<bool> _forceOpt = new(["--force", "-f"], "Forces pulling, bypassing safe guards.");
        private readonly Option<bool> _dryRunOpt = new(["--dry-run"], "Previews the command without executing it.");
        private readonly Option<bool> _verboseOpt = new(["--verbose", "-v"], "Shows verbose output.");
        
        private readonly Command addCmd = new("add", "Adds a new directory to the backup list.");
        private readonly Command listCmd = new("list", "Shows all directories in the backup list.");
        private readonly Command removeCmd = new("remove", "Removes a directory from the backup list.");

        public RestoreCommand() : base("restore", "Pulls files a vault.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.AddOption(_beforeOpt);
            this.AddOption(_destOpt);
            this.AddOption(_archiveOpt);
            this.AddOption(_forceOpt);
            this.AddOption(_dryRunOpt);
            this.AddOption(_verboseOpt);
            this.SetHandler(HandlePullAsync, _sourceOpt, _configOpt, _beforeOpt, _destOpt, _archiveOpt, _forceOpt, _verboseOpt, _dryRunOpt);
            
            this.AddCommand(addCmd);
            addCmd.AddArgument(_sourceArg);
            addCmd.AddOption(_configOpt);
            addCmd.AddOption(_destOpt);
            addCmd.SetHandler(HandleAddAsync, _sourceArg, _configOpt, _destOpt);

            this.AddCommand(removeCmd);
            removeCmd.AddArgument(_sourceArg);
            removeCmd.AddOption(_configOpt);
            removeCmd.SetHandler(HandleRemoveAsync, _sourceArg, _configOpt);
        }

        private async Task HandlePullAsync(string? path, string? config, DateTime before, string? destination, bool archive, bool force, bool verbose, bool dryRun)
        {
            _sw = Stopwatch.StartNew();
            DateTime timestamp = before != DateTime.MinValue ? before.AddMinutes(1).AddTicks(-1) : DateTime.Now;
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await PullPathAsync(localVault, path, timestamp, destination, archive, force, verbose, dryRun);
                }
                else
                {
                    await PullSystemAsync(localVault, timestamp, destination, archive, force, verbose, dryRun);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await Program.Settings.ForEachVaultAsync(vault => PullPathAsync(vault, path, timestamp, destination, archive, force, verbose, dryRun));
                }
                else
                {
                    await Program.Settings.ForEachVaultAsync(vault => PullSystemAsync(vault, timestamp, destination, archive, force, verbose, dryRun));
                }
            }
        }

        private async Task PullSystemAsync(LocalVaultConfig vault, DateTime timestamp, string? output, bool archive, bool force, bool verbose, bool dryRun)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            foreach (PullRecord record in syncManager.RemoteVault.PullDirectories.Where(r => r.Machine == Environment.MachineName))
            {
                await PullInternalAsync(syncManager, record.Source, timestamp, (output ?? record.Destination), archive, force, verbose, dryRun);
            }
        }

        private async Task PullPathAsync(LocalVaultConfig vault, string path, DateTime timestamp, string? destination, bool archive, bool force, bool verbose, bool dryRun)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            await PullInternalAsync(syncManager, path, timestamp, destination, archive, force, verbose, dryRun);
        }

        private async Task PullInternalAsync(ISyncManager syncManager, string path, DateTime timestamp, string? destination, bool archive, bool force, bool verbose, bool dryRun)
        {
            CommandLine.WriteLine(syncManager.RemoteVault, $"Scanning for files in {path}...", ConsoleColor.DarkGray);
            IReadOnlyList<LocalFile> files = await (syncManager.Database?.GetLatestFilesAsync(path, timestamp, archive) ?? Task.FromResult<IReadOnlyList<LocalFile>>([]));
            Log.Debug($"GetLatestFilesAsync returned {files.Count} files for path '{path}'");

            ConcurrentBag<LocalFile> restoreFiles = new();
            System.Threading.Tasks.Parallel.ForEach(files, ParallelConfig.Options, (file) =>
            {
                //string sourcePath = file.Fullname;
                //string outputPath = string.IsNullOrEmpty(output) ? sourcePath : PathBuilder.ReplacePath(sourcePath, path, output);
                string outputPath = PathBuilder.ReplacePath(file.Fullname, path, destination);
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
                CommandLine.WriteLine($"This operation will pull {restoreFiles.Count:N0} files into: {(string.IsNullOrEmpty(destination) ? path : destination)}", ConsoleColor.Green);
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

        #region Add

        private async Task HandleAddAsync(string path, string? config, string? destination)
        {
            LocalVaultConfig? localVault = string.IsNullOrEmpty(config) ? ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled) : ParallelConfig.GetVault(config);
            if (!string.IsNullOrEmpty(config) && localVault != null)
            {
                await AddPathAsync(localVault, path, destination);
            }
            else
            {
                await Program.Settings.ForEachVaultAsync(vault => AddPathAsync(vault, path, destination));
            }
        }

        private async Task AddPathAsync(LocalVaultConfig vault, string path, string? destination)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            PullRecord record = new(path, destination);
            if (!syncManager.RemoteVault.PullDirectories.Add(record))
            {
                CommandLine.WriteLine(vault, $"Unable to add path: '{path}'", ConsoleColor.Yellow);
                return;
            }

            CommandLine.WriteLine(vault, $"Successfully added '{path}'", ConsoleColor.Green);
            await syncManager.DisconnectAsync();
        }

        #endregion

        #region Remove

        private async Task HandleRemoveAsync(string path, string? config)
        {
            LocalVaultConfig? localVault = string.IsNullOrEmpty(config) ? ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled) : ParallelConfig.GetVault(config);
            if (!string.IsNullOrEmpty(config) && localVault != null)
            {
                await RemovePathAsync(localVault, path);
            }
            else
            {
                await Program.Settings.ForEachVaultAsync(vault => RemovePathAsync(vault, path));
            }
        }

        private async Task RemovePathAsync(LocalVaultConfig vault, string path)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            IEnumerable<PullRecord> records = syncManager.RemoteVault.PullDirectories.Where(r => r.Machine == Environment.MachineName && r.Source == path);
            foreach (PullRecord record in records)
            {
                if (syncManager.RemoteVault.PullDirectories.Remove(record)) continue;
                CommandLine.WriteLine(vault, $"Unable to remove path: '{path}'", ConsoleColor.Yellow);
            }

            CommandLine.WriteLine(vault, $"Successfully removed '{path}'", ConsoleColor.Green);
            await syncManager.DisconnectAsync();
        }

        #endregion
    }
}