// Copyright 2026 Entex Interactive

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Storage;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class VaultsCommand : Command
    {
        private readonly Argument<string> configArg = new("config", "The vault configuration to use.");
        private readonly Option<string> configOpt = new(["--config", "-c"], "The vault configuration to use.");

        private readonly Command addCmd = new("add", "Adds a new vault configuration.");
        private readonly Command editCmd = new("edit", "Edits a vault configuration.");
        private readonly Command findCmd = new("find", "Finds vault configurations in a location.");
        private readonly Command viewCmd = new("view", "Shows the vault configuration.");
        private readonly Command setCmd = new("set", "Sets a new vault configuration.");
        private readonly Command statsCmd = new("stats", "Displays various vault statistics.");
        private readonly Command delCmd = new("delete", "Deletes a vault configuration.");

        public VaultsCommand() : base("vaults", "View or edit the vaults.")
        {
            this.SetHandler(() =>
            {
                CommandLine.WriteLine("Active vaults:");
                foreach (LocalVaultConfig vault in Program.Settings.Vaults.OrderBy(v => v.Name))
                {
                    CommandLine.WriteLine($"[{vault.Id}]: {vault.Name} {(vault.Enabled ? "(Active)" : string.Empty)}");
                }
            });

            this.AddCommand(addCmd);
            addCmd.SetHandler(() =>
            {
                CommandLine.WriteLine("Creating new storage vault...", ConsoleColor.DarkGray);
                StorageCredentials spc = new StorageCredentials
                {
                    Service = Enum.Parse<FileService>(CommandLine.ReadString($"Service ({string.Join(", ", Enum.GetNames(typeof(FileService)))})") ?? string.Empty, true)
                };

                if (spc.Service == FileService.Local)
                {
                    CommandLine.WriteLine("Local vaults are NOT cross-platform!", ConsoleColor.Yellow);
                    spc.RootDirectory = CommandLine.ReadString("Root") ?? string.Empty;
                }
                else if (spc.Service == FileService.Cloud)
                {
                    string? bucketInput = CommandLine.ReadString("Bucket Name (Leave empty for default)");
                    string bucketName = string.IsNullOrEmpty(bucketInput) ? "parallel" : bucketInput;
                    spc.RootDirectory = bucketName;

                    string? regionInput = CommandLine.ReadString("Region Name (Leave empty for default)");
                    string regionName = string.IsNullOrEmpty(regionInput) ? "us-east-1" : regionInput;
                    spc.Region = regionName;

                    spc.Address = CommandLine.ReadString("Endpoint");
                    spc.Username = CommandLine.ReadString("Access Key");
                    spc.Password = CommandLine.ReadPassword("Secret Key");
                    spc.ForceStyle = CommandLine.ReadBool("Force Path Style? (y/n)", true);
                }
                else
                {
                    spc.RootDirectory = CommandLine.ReadString("Root") ?? string.Empty;
                    spc.Address = CommandLine.ReadString("Address");
                    spc.Username = CommandLine.ReadString("Username");
                    spc.Password = CommandLine.ReadPassword("Password");
                }

                string? inputId = CommandLine.ReadString("Id (Leave empty for random)");
                string profileId = string.IsNullOrEmpty(inputId) ? HashGenerator.GenerateHash(8, true) : inputId;

                string? inputName = CommandLine.ReadString("Name (Leave empty for machine name)");
                string profileName = string.IsNullOrEmpty(inputName) ? Environment.MachineName : inputName;

                LocalVaultConfig localVault = new(profileId, profileName, spc);
                localVault.Enabled = CommandLine.ReadBool("Enabled? (y/n)", true);
                Program.Settings.Vaults.Add(localVault);
                Program.Settings.Save();

                CommandLine.WriteLine($"Saved new storage vault: '{localVault.Name}' ({localVault.Id})");
            });

            this.AddCommand(findCmd);
            findCmd.SetHandler(() =>
            {

            });

            this.AddCommand(viewCmd);
            viewCmd.AddArgument(configArg);
            viewCmd.SetHandler(async (config) =>
            {
                CommandLine.WriteLine($"Retrieving vault information...", ConsoleColor.DarkGray);
                LocalVaultConfig? vault = ParallelConfig.GetVault(config);
                if (vault == null)
                {
                    CommandLine.WriteLine($"No vault was found!", ConsoleColor.Yellow);
                    return;
                }

                ISyncManager? syncManager = SyncManager.CreateNew(vault);
                if (syncManager == null || !await syncManager.ConnectAsync())
                {
                    CommandLine.WriteLine(vault, $"Failed to connect to vault '{vault.Name}'!", ConsoleColor.Red);
                    return;
                }

                RemoteVaultConfig remoteVault = syncManager.RemoteVault;
                CommandLine.WriteLine($"'{remoteVault.Name}' ({remoteVault.Id}):");
                CommandLine.WriteArray("Push Directories", remoteVault.PushDirectories);
                CommandLine.WriteArray("Pull Directories", remoteVault.PullDirectories.Select(d => d.Source));
                CommandLine.WriteArray("Ignore Directories", remoteVault.IgnoreDirectories);
                CommandLine.WriteArray("Prune Directories", remoteVault.PruneDirectories);
                CommandLine.WriteLine($"Prune Period: {remoteVault.PrunePeriod} days");
            }, configArg);

            this.AddCommand(setCmd);
            setCmd.SetHandler(() =>
            {

            });
            
            this.AddCommand(statsCmd);
            statsCmd.AddOption(configOpt);
            statsCmd.SetHandler(async (config) =>
            {
                LocalVaultConfig? vault = ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled);
                if (!string.IsNullOrEmpty(config)) vault = ParallelConfig.GetVault(config);
                if (vault == null)
                {
                    CommandLine.WriteLine($"No vault was found!", ConsoleColor.Yellow);
                    return;
                }

                await DisplayDiskInformationAsync(vault);
            }, configOpt);
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
            long localSize = await (db?.GetLocalSizeAsync() ?? Task.FromResult(0L));
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