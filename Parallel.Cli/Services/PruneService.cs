// Copyright 2026 Entex Interactive

using Microsoft.Extensions.Hosting;

namespace Parallel.Cli.Services
{
    public class PruneService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Log.Debug("Prune: {DateTimeOffset}", DateTimeOffset.Now);
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}