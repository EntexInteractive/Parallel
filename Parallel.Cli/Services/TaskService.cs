// Copyright 2026 Entex Interactive

using Microsoft.Extensions.Hosting;
using Parallel.Core.Tasks;

namespace Parallel.Cli.Services
{
    public class TaskService : BackgroundService
    {
        private readonly TaskQueueExecutor _executor;

        public TaskService(TaskQueueExecutor executor)
        {
            _executor = executor;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _executor.RunAsync(stoppingToken);
        }
    }
}