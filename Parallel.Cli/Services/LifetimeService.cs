// Copyright 2026 Entex Interactive

using Microsoft.Extensions.Hosting;
using Parallel.Core.IO;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Services
{
    public class LifetimeService : BackgroundService
    {
        //private readonly LogEventTracker _logEventTracker;
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Task loadTask = RunLoadTaskAsync(stoppingToken);
                Task logTask = RunLogTasksAsync(stoppingToken);
                await Task.WhenAll(loadTask, logTask);
            }
        }

        private async Task RunLoadTaskAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                Program.Settings = ParallelConfig.Load();
            }
        }

        private async Task RunLogTasksAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(GetNextDay(), stoppingToken);
                /*await Log.CloseAndFlushAsync();

                if (_logEventTracker.ErrorCount <= 0) continue;

                string logDir = Path.Combine(PathBuilder.ProgramData, "Logs");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                File.Move(PathBuilder.LogFile, Path.Combine(logDir, $"{DateTime.Now:MM-dd-yyyy hh-mm-ss}.log"));*/
            }
        }
        
        private static TimeSpan GetNextDay()
        {
            DateTime current = DateTime.UtcNow;
            DateTime nextMidnight = current.AddDays(1);
            return nextMidnight - current;
        }
    }
}