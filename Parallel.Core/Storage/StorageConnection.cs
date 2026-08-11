// Copyright 2026 Entex Interactive

using Parallel.Core.Settings;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// The supported file service types.
    /// </summary>
    public enum FileService
    {
        /// <summary>
        /// A local file system either through external or networked drives.
        /// </summary>
        Local,

        /// <summary>
        /// A remote file storage server through secure shell.
        /// </summary>
        Remote,

        /// <summary>
        /// A cloud storage server hosted through Amazon simple storage service.
        /// </summary>
        Cloud,
    };

    /// <summary>
    /// Represents the way to connect to different file system associations. This class cannot be inherited.
    /// </summary>
    public static class StorageConnection
    {
        /// <summary>
        /// Creates a new file system association.
        /// </summary>
        /// <param name="vaultConfig">The vault needed for the associated file system.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static IStorageProvider CreateNew(LocalVaultConfig vaultConfig)
        {
            return vaultConfig.Credentials.Service switch
            {
                FileService.Local => new LocalStorageProvider(vaultConfig),
                FileService.Remote => new SshStorageProvider(vaultConfig),
                FileService.Cloud => new S3StorageProvider(vaultConfig),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}