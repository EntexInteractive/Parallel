// Copyright 2026 Entex Interactive

using Parallel.Core.Models;

namespace Parallel.Core.Diagnostics
{
    public enum ProgressOperation
    {
        Archived,
        Restored,
        Synced,
        Pruned,
        Scrubbed
    }

    /// <summary>
    /// Defines a provider for progress updates.
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Reports a progress update.
        /// </summary>
        void Report(ProgressOperation operation, LocalFile file);

        /// <summary>
        /// Reports a failed update.
        /// </summary>
        void Failed(LocalFile file, string reason);
    }
}