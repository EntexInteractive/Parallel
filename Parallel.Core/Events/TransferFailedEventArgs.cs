// Copyright 2026 Entex Interactive

using System.Security.Cryptography.X509Certificates;
using Parallel.Core.IO;
using Parallel.Core.Models;

namespace Parallel.Core.Events
{
    public class TransferFailedEventArgs : EventArgs
    {
        public LocalFile File { get; }
        public Exception Exception { get; }

        public TransferFailedEventArgs(LocalFile file, Exception exception)
        {
            File = file;
            Exception = exception;
        }
    }
}