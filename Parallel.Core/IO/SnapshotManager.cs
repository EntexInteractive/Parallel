// Copyright 2026 Entex Interactive

using System.Collections.Concurrent;
using Parallel.Core.Models;
using Parallel.Core.Utils;

namespace Parallel.Core.IO
{
    public class SnapshotManager
    {
        public static async Task<string> CreateSnapshotAsync(IEnumerable<SnapshotRecord> snapshots)
        {
            string filename = $"snapshot_{UnixTime.Now.TotalMilliseconds}";
            string filePath = Path.Combine(PathBuilder.TempDirectory, filename + ".json");
            await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(snapshots));
            return filePath;
        }

        public static Task<IEnumerable<SnapshotRecord>> LoadSnapshotsAsync(string filePath)
        {
            throw new NotImplementedException();
        }
    }
}