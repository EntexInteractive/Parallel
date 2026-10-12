// Copyright 2026 Entex Interactive

namespace Parallel.Core.Tasks
{
    public class TaskQueueOptions
    {
        public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    }
}