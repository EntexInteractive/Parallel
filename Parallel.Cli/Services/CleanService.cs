// Copyright 2026 Entex Interactive

using Microsoft.Extensions.Hosting;

namespace Parallel.Cli.Services
{
    public class CleanService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Log.Debug("Clean: {DateTimeOffset}", DateTimeOffset.Now);
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}