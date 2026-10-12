// Copyright 2026 Entex Interactive

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Parallel.Core.Tasks
{ 
    /// <summary>
    /// Represents an asynchronous queue for running tasks.
    /// </summary>
    public class TaskQueuer
    {
        private readonly ILogger<TaskQueuer> _logger;
        private readonly ConcurrentQueue<EnqueuedTask> _queue = new();
        private readonly ConcurrentDictionary<string, EnqueuedTask> _tasks = new();
        private readonly SemaphoreSlim _semaphore = new(0);

        public TaskQueuer(ILogger<TaskQueuer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="TaskQueuer"/>.
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="TaskQueuer"/> is empty.
        /// </summary>
        public bool IsEmpty => _queue.IsEmpty;

        /// <summary>
        /// Adds a task to be executed by the <see cref="TaskQueueExecutor"/>.
        /// </summary>
        /// <param name="key">The unique key to prevent</param>
        /// <param name="task">The task to be executed.</param>
        public void Enqueue(string key, Func<CancellationToken, Task> task)
        {
            ArgumentNullException.ThrowIfNull(task);
            Func<CancellationToken, Task> safeTask = async (ct) =>
            {
                try
                {
                    await task(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception throw executing task: {key}", key);
                }
            };

            _tasks.AddOrUpdate(key, k =>
            {
                EnqueuedTask enqueue = new EnqueuedTask(key, safeTask);
                _queue.Enqueue(enqueue);
                _semaphore.Release();
                return enqueue;
            }, (k, existing) =>
            {
                Interlocked.Increment(ref existing.Priority);
                return existing;
            });
        }

        public async Task<Func<CancellationToken, Task>> WaitAsync(CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            while (true)
            {
                if (!_queue.TryDequeue(out EnqueuedTask? candidate)) continue;
                EnqueuedTask? next = _tasks.Values.MaxBy(t => t.Priority + (DateTime.UtcNow - t.EnqueuedAt).TotalSeconds * 0.1);
                if (ReferenceEquals(candidate, next))
                {
                    _logger.LogDebug($"Starting task with key: '{candidate.Key}', remaining: {_queue.Count - 1}");
                    _tasks.TryRemove(candidate.Key, out _);
                    return candidate.TaskFunc;
                }

                _queue.Enqueue(candidate);
            }
        }
    }
}