// Copyright 2026 Entex Interactive

using Parallel.Core.Settings;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Represents the way manage <see cref="ISyncManager"/>s.
    /// </summary>
    public static class SyncManager
    {
        /// <summary>
        /// Creates a new instance of an <see cref="ISyncManager"/>.
        /// </summary>
        /// <param name="localVault"></param>
        /// <returns></returns>
        public static ISyncManager? CreateNew(LocalVaultConfig? localVault)
        {
            return localVault?.Credentials is null ? null : new FileSyncManager(localVault);
        }
    }
}