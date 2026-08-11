// Copyright 2026 Entex Interactive

using System.Diagnostics;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Utils
{
    public class ProgressReporter : IProgressReporter
    {
        private Stopwatch _sw;
        private readonly LocalVaultConfig _localVault;
        private int _current;
        private readonly int _total;

        public ProgressReporter(LocalVaultConfig localVault, int totalFiles)
        {
            _sw = Stopwatch.StartNew();
            _localVault = localVault;
            _current = 0;
            _total = totalFiles;
        }

        public void Report(ProgressOperation operation, LocalFile file)
        {
            Interlocked.Increment(ref _current);
            double percent = _current * 100.0 / _total;
            CommandLine.WriteLine($"[{_localVault.Id}] ({percent:N1}%) {operation}: {file.Fullname}");
        }

        /// <inheritdoc />
        public void Reset()
        {
            _current = 0;
        }

        /// <inheritdoc />
        public void Failed(LocalFile file, string message)
        {
            CommandLine.WriteLine(_localVault, $"Operation failed: '{file.Fullname}' (Reason: {message})", ConsoleColor.Red);
        }
    }
}