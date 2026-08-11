// Copyright 2026 Entex Interactive

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class HistoryCommand : Command
    {
        private const int _limit = 25;

        private readonly Command _archiveCmd = new("archive", "Shows the history related to file deletions.");
        private readonly Command _cleanCmd = new("clean", "Shows the history related to file cleaning.");
        private readonly Command _cloneCmd = new("clone", "Shows the history related to file cloning.");
        private readonly Command _syncCmd = new("sync", "Shows the history related to file syncing.");
        private readonly Command _restoreCmd = new("pull", "Shows the history related to file restoring.");
        private readonly Command _pruneCmd = new("prune", "Shows the history related to file pruning.");

        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to push.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<int> _limitOpt = new(["--limit", "-l"], "The number of entries to show.");

        public HistoryCommand() : base("history", "Displays the history of a vault.")
        {
            this.AddCommand(_archiveCmd);
            this.AddCommand(_cleanCmd);
            this.AddCommand(_cloneCmd);
            this.AddCommand(_syncCmd);
            this.AddCommand(_restoreCmd);
            this.AddCommand(_pruneCmd);
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.AddOption(_limitOpt);
            this.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(null, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _archiveCmd.AddOption(_sourceOpt);
            _archiveCmd.AddOption(_configOpt);
            _archiveCmd.AddOption(_limitOpt);
            _archiveCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Archived, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _cleanCmd.AddOption(_sourceOpt);
            _cleanCmd.AddOption(_configOpt);
            _cleanCmd.AddOption(_limitOpt);
            _cleanCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Cleaned, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _cloneCmd.AddOption(_sourceOpt);
            _cloneCmd.AddOption(_configOpt);
            _cloneCmd.AddOption(_limitOpt);
            _cloneCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Cloned, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _syncCmd.AddOption(_sourceOpt);
            _syncCmd.AddOption(_configOpt);
            _syncCmd.AddOption(_limitOpt);
            _syncCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Synced, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _restoreCmd.AddOption(_sourceOpt);
            _restoreCmd.AddOption(_configOpt);
            _restoreCmd.AddOption(_limitOpt);
            _restoreCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Restored, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _pruneCmd.AddOption(_sourceOpt);
            _pruneCmd.AddOption(_configOpt);
            _pruneCmd.AddOption(_limitOpt);
            _pruneCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Pruned, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);
        }

        private async Task DisplayHistoryAsync(HistoryType? type, string path, string config, int limit)
        {
            if (limit == 0) limit = _limit;
            LocalVaultConfig? vault = ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled);
            if (!string.IsNullOrEmpty(config)) vault = ParallelConfig.GetVault(config);
            if (vault == null)
            {
                CommandLine.WriteLine($"No vault was found!", ConsoleColor.Yellow);
                return;
            }

            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, $"Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            Task<IReadOnlyList<HistoryEvent>>? historyTask = type == null ? syncManager.Database?.GetHistoryAsync(path) : syncManager.Database?.GetHistoryAsync(path, type);
            IReadOnlyList<HistoryEvent> historyList = await (historyTask ?? Task.FromResult<IReadOnlyList<HistoryEvent>>([]));
            if (!historyList.Any()) CommandLine.WriteLine("No history was found!", ConsoleColor.Yellow);

            int display = Math.Min(limit, historyList.Count);
            for (int i = 0; i < display; i++)
            {
                HistoryEvent historyEvent = historyList[i];
                CommandLine.WriteLine($"[{Formatter.FromDateTime(historyEvent.CreatedAt.ToLocalTime())}] {historyEvent.Type}: {historyEvent.Fullname}");
            }

            int remaining = historyList.Count - display;
            if (remaining > 0)
            {
                CommandLine.WriteLine($"{remaining:N0} entr{(remaining == 1 ? "y" : "ies")} remaining...", ConsoleColor.DarkGray);
            }
        }
    }
}