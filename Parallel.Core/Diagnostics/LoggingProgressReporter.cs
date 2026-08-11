// Copyright 2026 Entex Interactive

using System.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.Diagnostics
{
    /// <summary>
    /// Represents a null <see cref="IProgressReporter"/>.
    /// </summary>
    public class LoggingProgressReporter : IProgressReporter
    {
        private readonly LocalVaultConfig _localVault;

        public LoggingProgressReporter(LocalVaultConfig localVault)
        {
            _localVault = localVault;
        }

        /// <inheritdoc />
        public void Report(ProgressOperation operation, LocalFile file)
        {
            Log.Information($"[{_localVault.Id}] {operation}: {file.Fullname}");
        }

        /// <inheritdoc />
        public void Failed(LocalFile file, string message)
        {
            Log.Error(message);
        }
    }
}