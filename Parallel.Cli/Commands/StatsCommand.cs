// Copyright 2026 Entex Interactive

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Settings;
using Parallel.Core.Storage;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class StatsCommand : Command
    {
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");

        public StatsCommand() : base("stats", "Displays various vault statistics.")
        {
            this.AddOption(_configOpt);
            this.SetHandler(async (config) =>
            {
                LocalVaultConfig? vault = ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled);
                if (!string.IsNullOrEmpty(config)) vault = ParallelConfig.GetVault(config);
                if (vault == null)
                {
                    CommandLine.WriteLine($"No vault was found!", ConsoleColor.Yellow);
                    return;
                }

                await DisplayDiskInformationAsync(vault);
            }, _configOpt);
        }

        private async Task DisplayDiskInformationAsync(LocalVaultConfig vault)
        {
            CommandLine.WriteLine($"Retrieving vault information...", ConsoleColor.DarkGray);
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, $"Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            IDatabase? db = syncManager.Database;
            long localSize = await (db?.GetCurrentSizeAsync() ?? Task.FromResult(0L));
            long totalSize = await (db?.GetTotalSizeAsync() ?? Task.FromResult(0L));
            long totalFiles = await (db?.GetTotalFilesAsync() ?? Task.FromResult(0L));
            long totalLocalFiles = await (db?.GetTotalFilesAsync(false) ?? Task.FromResult(0L));
            long totalDeletedFiles = await (db?.GetTotalFilesAsync(true) ?? Task.FromResult(0L));
            long totalRevisedFiles = await (db?.GetTotalRevisedFilesAsync() ?? Task.FromResult(0L));

            CommandLine.WriteLine($"Using vault '{syncManager.RemoteVault.Name}' ({vault.Id}):");
            CommandLine.WriteLine($"Service Type:   {vault.Credentials.Service}");
            CommandLine.WriteLine($"Root Directory: {vault.Credentials.RootDirectory}");
            CommandLine.WriteLine($"Managed Files:  {totalFiles:N0}");
            CommandLine.WriteLine($"Local Files:    {totalLocalFiles:N0}");
            CommandLine.WriteLine($"Deleted Files:  {totalDeletedFiles:N0}");
            CommandLine.WriteLine($"Revisions:      {totalRevisedFiles:N0}");
            CommandLine.WriteLine($"Local Size:     {Formatter.FromBytes(localSize)}");
            CommandLine.WriteLine($"Remote Size:    {Formatter.FromBytes(totalSize)}");

            if (vault.Credentials.Service.Equals(FileService.Local))
            {
                DriveInfo drive = new(vault.Credentials.RootDirectory);
                long diskUsage = drive.TotalSize - drive.TotalFreeSpace;
                CommandLine.WriteLine($"Total Usage:    {Formatter.FromBytes(diskUsage)} ({Math.Round(diskUsage / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Usage:     {Formatter.FromBytes(diskUsage - totalSize)} ({Math.Round((diskUsage - totalSize) / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Free:      {Formatter.FromBytes(drive.TotalFreeSpace)} ({Math.Round(drive.TotalFreeSpace / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Total:     {Formatter.FromBytes(drive.TotalSize)}");
            }
        }
    }
}