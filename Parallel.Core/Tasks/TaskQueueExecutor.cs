using Microsoft.Extensions.Logging;

namespace Parallel.Core.Tasks
{
    public sealed class TaskQueueExecutor
    {
        private readonly TaskQueuer _queuer;
        private readonly ILogger<TaskQueueExecutor> _logger;
        private readonly int _maxConcurrency;

        public TaskQueueExecutor(TaskQueuer queuer, ILogger<TaskQueueExecutor> logger, int maxConcurrency)
        {
            _queuer = queuer;
            _logger = logger;
            _maxConcurrency = maxConcurrency;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Task> workers = Enumerable.Range(0, _maxConcurrency).Select(_ => WorkerAsync(cancellationToken));
            await Task.WhenAll(workers);
        }

        private async Task WorkerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Func<CancellationToken, Task> task = await _queuer.WaitAsync(cancellationToken);
                    await task(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Queued task failed.");
                }
            }
        }
    }
}