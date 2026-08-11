// Copyright 2026 Entex Interactive

using System.CommandLine;
using System.Diagnostics;
using Parallel.Cli.Utils;
using Parallel.Core.IO;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class PruneCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();

        private readonly Argument<string> _sourceArg = new("path", "The path to add or remove.");
        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to prune.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<DateTime> _beforeOpt = new(["--before"], "Specified timestamp as the pruning reference point.");
        private readonly Option<int> _daysOpt = new(["--days", "-d"], "Specified number of days before the reference point.");
        private readonly Option<bool> _forceOpt = new(["--force", "-f"], "Forces pruning, bypassing safe guards.");
        private readonly Option<bool> _dryRunOpt = new(["--dry-run"], "Previews the command without executing it.");
        private readonly Option<bool> _verboseOpt = new(["--verbose", "-v"], "Shows verbose output.");

        private readonly Command addCmd = new("add", "Adds a new path to the prune list.");
        private readonly Command listCmd = new("list", "Shows all directories in the prune list.");
        private readonly Command removeCmd = new("remove", "Removes a directory from the prune list.");

        public PruneCommand() : base("prune", "Prunes the oldest files from vaults.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.AddOption(_beforeOpt);
            this.AddOption(_daysOpt);
            this.AddOption(_forceOpt);
            this.AddOption(_dryRunOpt);
            this.SetHandler(HandlePruneAsync, _sourceOpt, _configOpt, _beforeOpt, _daysOpt, _forceOpt, _dryRunOpt);

            this.AddCommand(addCmd);
            addCmd.AddArgument(_sourceArg);
            addCmd.AddOption(_configOpt);
            addCmd.SetHandler(HandleAddAsync, _sourceArg, _configOpt);

            this.AddCommand(removeCmd);
            removeCmd.AddArgument(_sourceArg);
            removeCmd.AddOption(_configOpt);
            removeCmd.SetHandler(HandleRemoveAsync, _sourceArg, _configOpt);
        }

        #region Pruning

        private async Task HandlePruneAsync(string? path, string? config, DateTime before, int days, bool force, bool dryRun)
        {
            _sw = Stopwatch.StartNew();
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await PrunePathAsync(localVault, path, before, days, force, dryRun);
                }
                else
                {
                    await PruneSystemAsync(localVault, before, days, force, dryRun);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await Program.Settings.ForEachVaultAsync(vault => PrunePathAsync(vault, path, before, days, force, dryRun));
                }
                else
                {
                    await Program.Settings.ForEachVaultAsync(vault => PruneSystemAsync(vault, before, days, force, dryRun));
                }
            }
        }

        private DateTime GetPruneDateTime(int prunePeriod, DateTime before, int days)
        {
            DateTime timestamp = before > DateTime.MinValue ? before : DateTime.Now;
            return days > 0 ? timestamp.AddDays(-days) : timestamp.AddDays(-prunePeriod);
        }

        private async Task PruneSystemAsync(LocalVaultConfig vault, DateTime before, int days, bool force, bool dryRun)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            DateTime timestamp = GetPruneDateTime(syncManager.RemoteVault.PrunePeriod, before, days);
            HashSet<string> directories = syncManager.RemoteVault.PruneDirectories;
            if (directories.Count == 0)
            {
                CommandLine.WriteLine($"No prunable directories have been set!", ConsoleColor.Yellow);
                return;
            }

            foreach (string path in directories)
            {
                await PruneInternalAsync(syncManager, path, timestamp, force, dryRun);
            }
        }

        private async Task PrunePathAsync(LocalVaultConfig vault, string path, DateTime before, int days, bool force, bool dryRun)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            DateTime timestamp = GetPruneDateTime(syncManager.RemoteVault.PrunePeriod, before, days);
            Log.Debug($"Pruning files before {timestamp}");

            await PruneInternalAsync(syncManager, path, timestamp, force, dryRun);
        }

        private async Task PruneInternalAsync(ISyncManager syncManager, string path, DateTime timestamp, bool force, bool dryRun)
        {
            CommandLine.WriteLine(syncManager.RemoteVault, $"Scanning for files in {path}...", ConsoleColor.DarkGray);
            IReadOnlyList<LocalFile> files;
            if (force)
            {
                files = await (syncManager.Database?.GetFilesAsync(path, timestamp) ?? Task.FromResult<IReadOnlyList<LocalFile>>([]));
            }
            else
            {
                files = await (syncManager.Database?.GetFilesAsync(path, timestamp, true) ?? Task.FromResult<IReadOnlyList<LocalFile>>([]));
            }

            if (files.Count == 0)
            {
                CommandLine.WriteLine($"No prunable files were found!", ConsoleColor.Yellow);
                return;
            }

            if (!force && !dryRun)
            {
                CommandLine.WriteLine($"This will permanently delete {files.Count:N0} files!", ConsoleColor.Yellow);
                if (!CommandLine.ReadBool("Do you wish to continue? [yes/no]", false))
                {
                    CommandLine.WriteLine($"Successfully cancelled operation.", ConsoleColor.Green);
                    return;
                }
            }

            if (dryRun)
            {
                string fileName = PathBuilder.TempFile;
                await File.WriteAllLinesAsync(fileName, files.Select(f => f.Fullname).OrderBy(f => f));
                CommandLine.WriteLine($"This operation will prune {files.Count:N0} files ({Formatter.FromBytes(files.Sum(f => f.LocalSize))})", ConsoleColor.Green);
                CommandLine.WriteLine($"A detailed list can be found here: {fileName}", ConsoleColor.DarkGray);
            }
            else
            {
                CommandLine.WriteLine(syncManager.RemoteVault, $"Pruning {files.Count:N0} files...", ConsoleColor.DarkGray);
                int prunedFiles = await syncManager.PruneFilesAsync(files, new ProgressReporter(syncManager.RemoteVault, files.Count));
                CommandLine.WriteLine(syncManager.RemoteVault, $"Successfully pruned {prunedFiles:N0} files in {_sw.Elapsed}.", ConsoleColor.Green);
                await syncManager.DisconnectAsync();
            }
        }

        #endregion

        #region Add

        private async Task HandleAddAsync(string path, string? config)
        {
            LocalVaultConfig? localVault = string.IsNullOrEmpty(config) ? ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled) : ParallelConfig.GetVault(config);
            if (!string.IsNullOrEmpty(config) && localVault != null)
            {
                await AddPathAsync(localVault, path);
            }
            else
            {
                await Program.Settings.ForEachVaultAsync(vault => AddPathAsync(vault, path));
            }
        }

        private async Task AddPathAsync(LocalVaultConfig vault, string path)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            if (!syncManager.RemoteVault.PruneDirectories.Add(path))
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

            if (!syncManager.RemoteVault.PruneDirectories.Remove(path))
            {
                CommandLine.WriteLine(vault, $"Unable to remove path: '{path}'", ConsoleColor.Yellow);
                return;
            }

            CommandLine.WriteLine(vault, $"Successfully removed '{path}'", ConsoleColor.Green);
            await syncManager.DisconnectAsync();
        }

        #endregion
    }
}