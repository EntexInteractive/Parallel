// Copyright 2026 Entex Interactive

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parallel.Core.Tasks;

namespace Parallel.Cli.Services
{
    public class RunCommand : Command
    {
        private readonly Option<bool> _cleanOpt = new("-clean", "If the service should auto clean directories.");
        private readonly Option<bool> _pruneOpt = new("-prune", "If the service should auto prune deleted files.");
        private readonly Option<bool> _syncOpt = new("-sync", "If the service should auto sync files.");
        
        public RunCommand() : base("run", "Starts the background services.")
        {
            this.SetHandler(HandleCommandAsync);
        }

        private async Task HandleCommandAsync()
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            if (OperatingSystem.IsWindows()) builder.Services.AddWindowsService();
            if (OperatingSystem.IsLinux()) builder.Services.AddSystemd();
            
            builder.Services.AddHostedService<LifetimeService>();
            builder.Services.AddHostedService<TaskService>();
            
            builder.Services.AddSingleton<TaskQueueExecutor>();
            builder.Services.AddSingleton<TaskQueuer>();
            
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();
            
            await builder.Build().RunAsync();
        }
    }
}