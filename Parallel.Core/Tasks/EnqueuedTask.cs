// Copyright 2026 Entex Interactive

namespace Parallel.Core.Tasks
{
    public class EnqueuedTask
    {
        /// <summary>
        /// The time the task was created.
        /// </summary>
        public DateTime EnqueuedAt { get; }

        /// <summary>
        /// The actual <see cref="Task"/> to execute.
        /// </summary>
        public Func<CancellationToken, Task> TaskFunc { get; }

        /// <summary>
        /// The unique key to use deduplication.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The priority of the task.
        /// </summary>
        public int Priority;

        /// <summary>
        /// Creates a new instance of the <see cref="EnqueuedTask"/> class.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="taskFunc"></param>
        /// <param name="priority"></param>
        public EnqueuedTask(string key, Func<CancellationToken, Task> taskFunc, int priority = 0)
        {
            Key = key;
            EnqueuedAt = DateTime.UtcNow;
            TaskFunc = taskFunc;
            Priority = priority;
        }
    }
}