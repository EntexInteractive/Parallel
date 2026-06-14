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
        private readonly Command delCmd = new("delete", "Deletes a vault configuration.");

        public VaultsCommand() : base("vaults", "View or edit the vaults.")
        {
            this.SetHandler(() =>
            {
                CommandLine.WriteLine("Active vaults:");
                foreach (LocalVaultConfig vault in Program.Settings.Vaults.OrderBy(v => v.Name))
                {
                    CommandLine.WriteLine($"[{vault.Id}]: {vault.Name} {(vault.Enabled ? "(Syncing)" : string.Empty)}");
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
                CommandLine.WriteArray("Pull Directories", remoteVault.PullDirectories);
                CommandLine.WriteArray("Ignore Directories", remoteVault.IgnoreDirectories);
                CommandLine.WriteArray("Prune Directories", remoteVault.PruneDirectories);
                CommandLine.WriteLine($"Prune Period: {remoteVault.PrunePeriod} days");
            }, configArg);

            this.AddCommand(setCmd);
            setCmd.SetHandler(() =>
            {

            });
        }
    }
}