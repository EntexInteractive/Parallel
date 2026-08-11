// Copyright 2026 Entex Interactive

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Parallel.Cli.Services
{
    public class RunCommand : Command
    {
        private readonly Option<bool> _cleanOpt = new("-clean", "If the service should auto clean directories.");
        private readonly Option<bool> _pruneOpt = new("-prune", "If the service should auto prune deleted files.");
        private readonly Option<bool> _syncOpt = new("-sync", "If the service should auto sync files.");
        
        public RunCommand() : base("run", "Starts the background services.")
        {
            this.AddOption(_cleanOpt);
            this.AddOption(_syncOpt);
            this.AddOption(_pruneOpt);
            this.SetHandler(HandleCommandAsync, _cleanOpt,  _pruneOpt, _syncOpt);
        }

        private async Task HandleCommandAsync(bool clean, bool prune, bool sync)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            if (OperatingSystem.IsWindows()) builder.Services.AddWindowsService();
            if (OperatingSystem.IsLinux()) builder.Services.AddSystemd();
            
            if (clean) builder.Services.AddHostedService<CleanService>();
            if (prune) builder.Services.AddHostedService<PruneService>();
            if (sync) builder.Services.AddHostedService<SyncService>();
            builder.Services.AddHostedService<LifetimeService>();
            
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();
            
            await builder.Build().RunAsync();
        }
    }
}