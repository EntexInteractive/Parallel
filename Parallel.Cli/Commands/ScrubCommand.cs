// Copyright 2026 Entex Interactive

using System.CommandLine;
using System.Diagnostics;
using Parallel.Cli.Utils;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO;
using Parallel.Core.IO.Scanning;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Commands
{
    public class ScrubCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();
        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to scrub.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<bool> _verboseOpt = new(["--verbose", "-v"], "Shows verbose output.");

        public ScrubCommand() : base("scrub", "Verifies file integrity within a vault.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.AddOption(_verboseOpt);
            this.SetHandler(HandleScrubAsync, _sourceOpt, _configOpt, _verboseOpt);
        }

        private async Task HandleScrubAsync(string? path, string? config, bool verbose)
        {
            _sw = Stopwatch.StartNew();
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await ScrubPathAsync(localVault, path, verbose);
                }
                else
                {
                    await ScrubSystemAsync(localVault, verbose);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await Program.Settings.ForEachVaultAsync(vault => ScrubPathAsync(vault, path, verbose));
                }
                else
                {
                    await Program.Settings.ForEachVaultAsync(vault => ScrubSystemAsync(vault, verbose));
                }
            }
        }

        private async Task ScrubSystemAsync(LocalVaultConfig vault, bool verbose)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            foreach (string path in syncManager.RemoteVault.PushDirectories)
            {
                await ScrubInternalAsync(syncManager, path, verbose);
            }
        }

        private async Task ScrubPathAsync(LocalVaultConfig vault, string path, bool verbose)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            await ScrubInternalAsync(syncManager, path, verbose);
        }

        private async Task ScrubInternalAsync(ISyncManager syncManager, string path, bool verbose)
        {
            CommandLine.WriteLine(syncManager.RemoteVault, $"Scrubbing files in {path}...", ConsoleColor.DarkGray);
            IReadOnlyList<LocalFile> files = await (syncManager.Database?.GetFilesAsync(path, DateTime.Now) ?? Task.FromResult<IReadOnlyList<LocalFile>>([]));
            if (files.Count == 0)
            {
                CommandLine.WriteLine($"No prunable files were found!", ConsoleColor.Yellow);
                return;
            }

            CommandLine.WriteLine(syncManager.RemoteVault, $"Scrubbing {files.Count:N0} files...", ConsoleColor.DarkGray);
            IProgressReporter progressReporter = verbose ? new ProgressReporter(syncManager.RemoteVault, files.Count) : new LoggingProgressReporter(syncManager.RemoteVault);
            int scrubbedFiles = await syncManager.ScrubFilesAsync(files, progressReporter);

            CommandLine.WriteLine(syncManager.RemoteVault, $"Successfully scrubbed {scrubbedFiles:N0} files in {_sw.Elapsed}.", ConsoleColor.Green);
            await syncManager.DisconnectAsync();
        }
    }
}