// Copyright 2026 Entex Interactive

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parallel.Core.Models;

namespace Parallel.Core.IO
{
    public struct ScanFileSystemResult
    {
        public LocalFile[] BackupFiles { get; }
        public LocalFile[] DeletedFiles { get; }
        public LocalFile[] IgnoredFiles { get; }

        public ScanFileSystemResult(IEnumerable<LocalFile> localFiles, IEnumerable<LocalFile> deletedFiles, IEnumerable<LocalFile> ignoredFiles)
        {
            BackupFiles = localFiles.ToArray();
            DeletedFiles = deletedFiles.ToArray();
            IgnoredFiles = ignoredFiles.ToArray();
        }
    }
}