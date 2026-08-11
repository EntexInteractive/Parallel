// Copyright 2026 Entex Interactive

using Parallel.Core.Security;
using Parallel.Core.Storage;

namespace Parallel.Core.Settings
{
    /// <summary>
    /// Represents a localized vault connection configuration.
    /// </summary>
    public class LocalVaultConfig
    {
        /// <summary>
        /// A unique hash used to identify the vault.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The name of the vault.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// If the current vault config is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The credentials needed to log in to the associated <see cref="IStorageProvider"/>.
        /// </summary>
        public StorageCredentials Credentials { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalVaultConfig"/> class.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="credentials"></param>
        [JsonConstructor]
        public LocalVaultConfig(string id, string name, StorageCredentials credentials)
        {
            Id = id;
            Name = name;
            Credentials = credentials;
        }

        /// <summary>
        /// Loads settings from a file.
        /// </summary>
        public static LocalVaultConfig? Load(string path)
        {
            return !File.Exists(path) ? null : JsonConvert.DeserializeObject<LocalVaultConfig>(File.ReadAllText(path));
        }

        /// <summary>
        /// Saves credentials to a file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="localVault"></param>
        public static void Save(ParallelConfig config, LocalVaultConfig localVault)
        {
            config.Vaults.Add(localVault);
            config.Save();
        }
    }
}