// Copyright 2026 Entex Interactive

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Settings;

namespace Parallel.Cli.Commands
{
    public class FetchCommand : Command
    {
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");

        public FetchCommand() : base("fetch", "Fetches information from a vault.")
        {
            this.AddOption(_configOpt);
            this.SetHandler(HandleFetchAsync, _configOpt);
        }

        private async Task HandleFetchAsync(string config)
        {
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                await FetchVaultAsync(localVault);
                return;
            }

            await Program.Settings.ForEachVaultAsync(FetchVaultAsync);
        }

        private async Task FetchVaultAsync(LocalVaultConfig localVault)
        {
            CommandLine.WriteLine($"Retrieving vault information...", ConsoleColor.DarkGray);
            ISyncManager? syncManager = SyncManager.CreateNew(localVault);
            if (syncManager == null || !await syncManager.ConnectAsync(true))
            {
                CommandLine.WriteLine(localVault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            CommandLine.WriteLine(syncManager.LocalVault, $"Successfully fetched vault data for: '{localVault.Name}'", ConsoleColor.Green);
            await syncManager.DisconnectAsync();
        }
    }
}