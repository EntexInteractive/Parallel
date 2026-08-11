// Copyright 2026 Entex Interactive

using System.Data;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using Parallel.Core.Models;
using Parallel.Core.Utils;

namespace Parallel.Core.Database.Contexts
{
    /// <inheritdoc />
    public class SqliteContext : IDatabase
    {
        private readonly SemaphoreContext _semaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteContext"/> class.
        /// </summary>
        /// <param name="filePath"></param>
        public SqliteContext(string filePath)
        {
            _semaphore = new SemaphoreContext($"Data Source={filePath};Cache=Shared;Mode=ReadWriteCreate;Pooling=false;");
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            Log.Information("Creating index database...");
            await _semaphore.ExecuteAsync("CREATE TABLE IF NOT EXISTS `objects` (`name` TEXT NOT NULL, `fullname` TEXT NOT NULL, `parentDir` TEXT NOT NULL, `lastWrite` LONG INTEGER NOT NULL, `lastUpdate` LONG INTEGER NOT NULL, `localSize` LONG INTEGER NOT NULL, `remoteSize` LONG INTEGER NOT NULL, `type` TEXT NOT NULL DEFAULT Other CHECK(`type` IN ('Document', 'Photo', 'Music', 'Video', 'Other')), `hidden` INTEGER NOT NULL DEFAULT 0, `readOnly` INTEGER NOT NULL DEFAULT 0, `deleted` INTEGER NOT NULL DEFAULT 0, `localCheckSum` TEXT, `remoteCheckSum` TEXT, UNIQUE (fullname, localCheckSum));");
            await _semaphore.ExecuteAsync("CREATE TABLE IF NOT EXISTS `history` (`timestamp` LONG INTEGER NOT NULL, `fullname` TEXT NOT NULL, `type` INTEGER NOT NULL, PRIMARY KEY(`timestamp`));");
            await _semaphore.ExecuteAsync("CREATE TABLE IF NOT EXISTS `snapshots` (`timestamp` LONG INTEGER NOT NULL, `name` TEXT NOT NULL, PRIMARY KEY(`timestamp`));");
            await _semaphore.ExecuteAsync("CREATE INDEX idx_objects_path_update ON objects(fullname, lastupdate DESC, deleted);");
        }

        #region Objects

        /// <exception cref="ArgumentNullException"></exception>
        /// <inheritdoc />
        public async Task<bool> AddFileAsync(LocalFile file)
        {
            if (string.IsNullOrEmpty(file.Fullname)) throw new ArgumentNullException(nameof(file.Fullname));
            if (!file.TryGenerateLocalCheckSum()) throw new ArgumentNullException(nameof(file.LocalCheckSum));

            string sql = "INSERT OR REPLACE INTO objects (name, fullname, parentDir, lastWrite, lastUpdate, localSize, remoteSize, type, hidden, readOnly, deleted, localCheckSum, remoteCheckSum) VALUES (@Name, @Fullname, @ParentDirectory, @LastWrite, @LastUpdate, @LocalSize, @RemoteSize, @Type, @Hidden, @ReadOnly, @Deleted, @LocalCheckSum, @RemoteCheckSum);";
            return await _semaphore.ExecuteAsync(sql, new { file.Name, file.Fullname, file.ParentDirectory, LastWrite = file.LastWrite.TotalMilliseconds, LastUpdate = UnixTime.Now.TotalMilliseconds, file.LocalSize, file.RemoteSize, Type = file.Type.ToString(), file.Hidden, file.ReadOnly, file.Deleted, file.LocalCheckSum, file.RemoteCheckSum }) > 0;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveFileAsync(LocalFile file)
        {
            string sql = $"DELETE FROM objects WHERE fullname = @Fullname AND localCheckSum = @LocalCheckSum;";
            return await _semaphore.ExecuteAsync(sql, new { file.Fullname, file.LocalCheckSum }) > 0;
        }

        /// <inheritdoc />
        public async Task<long> GetLocalSizeAsync()
        {
            string sql = "SELECT COALESCE(SUM(f.localsize), 0) FROM objects f JOIN (SELECT fullname, MAX(lastupdate) AS max_lastupdate FROM objects WHERE deleted = 0 GROUP BY fullname) latest ON f.fullname = latest.fullname AND f.lastupdate = latest.max_lastupdate;";
            return await _semaphore.QuerySingleAsync<long>(sql);
        }

        /// <inheritdoc />
        public async Task<long> GetTotalSizeAsync()
        {
            string sql = $"SELECT COALESCE(SUM(remotesize), 0) FROM objects;";
            return await _semaphore.QuerySingleAsync<long>(sql);
        }

        /// <inheritdoc />
        public async Task<long> GetTotalFilesAsync()
        {
            string sql = $"SELECT COUNT(*) FROM objects;";
            return await _semaphore.QuerySingleAsync<long>(sql);
        }

        /// <inheritdoc />
        public async Task<long> GetTotalFilesAsync(bool deleted)
        {
            string sql = $"SELECT COUNT(DISTINCT fullname) FROM objects WHERE deleted = @deleted;";
            return await _semaphore.QuerySingleAsync<long>(sql, new { deleted });
        }

        public async Task<long> GetTotalRevisedFilesAsync()
        {
            string sql = $"SELECT COUNT(*) FROM objects WHERE lastupdate NOT IN (SELECT MAX(lastupdate) FROM objects GROUP BY fullname);";
            return await _semaphore.QuerySingleAsync<long>(sql);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<LocalFile>> GetLatestFilesAsync(string path, DateTime timestamp)
        {
            string sql = "SELECT * FROM (SELECT * FROM objects WHERE fullname LIKE @Path AND lastupdate <= @Time ORDER BY lastwrite DESC) GROUP BY fullname;";
            return await _semaphore.QueryAsync<LocalFile>(sql, new { Path = $"{path}%", Time = new UnixTime(timestamp).TotalMilliseconds });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<LocalFile>> GetLatestFilesAsync(string path, DateTime timestamp, bool deleted)
        {
            string sql = "SELECT * FROM (SELECT * FROM objects WHERE fullname LIKE @Path AND lastupdate <= @Time AND deleted = @deleted ORDER BY lastwrite DESC) GROUP BY fullname;";
            return await _semaphore.QueryAsync<LocalFile>(sql, new { Path = $"{path}%", Time = new UnixTime(timestamp).TotalMilliseconds, deleted });
        }

        public async Task<IReadOnlyList<LocalFile>> GetRevisedFilesAsync(string path)
        {
            string sql = $"SELECT * FROM objects WHERE fullname LIKE '{path}%' AND lastupdate NOT IN (SELECT MAX(lastupdate) FROM objects GROUP BY fullname) ORDER BY lastupdate DESC;";
            return await _semaphore.QueryAsync<LocalFile>(sql, new { Path = $"{path}%" });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<LocalFile>> GetFilesAsync(string path, DateTime timestamp)
        {
            string sql = "SELECT * FROM objects WHERE fullname LIKE @Path AND lastupdate <= @Time ORDER BY lastupdate ASC";
            return await _semaphore.QueryAsync<LocalFile>(sql, new { Path = $"{path}%", Time = new UnixTime(timestamp).TotalMilliseconds });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<LocalFile>> GetFilesAsync(string path, DateTime timestamp, bool deleted)
        {
            string sql = "SELECT * FROM objects WHERE fullname LIKE @Path AND lastupdate <= @Time AND deleted = @deleted ORDER BY lastupdate ASC";
            return await _semaphore.QueryAsync<LocalFile>(sql, new { Path = $"{path}%", Time = new UnixTime(timestamp).TotalMilliseconds, deleted });
        }

        /// <inheritdoc />
        public async Task<LocalFile?> GetFileAsync(string path)
        {
            string sql = $"SELECT (name, fullname, remotepath, lastwrite, lastupdate, localsize, remotesize, type, hidden, readonly, deleted, checksum) FROM objects WHERE fullname LIKE \"%@path%\" OR remotepath LIKE \"%@path%\" ORDER BY lastupdate DESC";
            return await _semaphore.QuerySingleAsync<LocalFile>(sql, new { path });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> ListDirectoriesAsync(string path)
        {
            string sql = "SELECT DISTINCT parentdir FROM objects WHERE parentdir LIKE @Path ORDER BY parentdir ASC";
            return await _semaphore.QueryAsync<string>(sql, new { Path = $"{path}%" });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<LocalFile>> ListFilesAsync(string path)
        {
            string sql = "SELECT * FROM objects WHERE parentdir = @Path ORDER BY fullname ASC, lastupdate DESC";
            return await _semaphore.QueryAsync<LocalFile>(sql, new { Path = path });
        }

        #endregion

        #region History

        /// <inheritdoc />
        public async Task<bool> AddHistoryAsync(HistoryType type, LocalFile file)
        {
            if (string.IsNullOrEmpty(file.Fullname)) throw new ArgumentNullException(nameof(file.Fullname));
            string sql = @"INSERT OR REPLACE INTO history (timestamp, fullname, type) VALUES(@Timestamp, @Fullname, @Type);";
            return await _semaphore.ExecuteAsync(sql, new { Timestamp = file.LastUpdate.TotalMilliseconds, file.Fullname, Type = type }) > 0;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<HistoryEvent>> GetHistoryAsync(string path)
        {
            string sql = "SELECT * FROM history WHERE fullname LIKE @Fullname ORDER BY timestamp DESC;";
            return await _semaphore.QueryAsync<HistoryEvent>(sql, new { Fullname = $"%{path}%" });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<HistoryEvent>> GetHistoryAsync(string path, int limit)
        {
            string sql = "SELECT * FROM history WHERE fullname LIKE @Fullname ORDER BY timestamp DESC LIMIT @limit;";
            return await _semaphore.QueryAsync<HistoryEvent>(sql, new { Fullname = $"%{path}%", limit });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<HistoryEvent>> GetHistoryAsync(string path, HistoryType? type)
        {
            string sql = "SELECT * FROM history WHERE fullname LIKE @Fullname AND type = @type ORDER BY timestamp DESC;";
            return await _semaphore.QueryAsync<HistoryEvent>(sql, new { Fullname = $"%{path}%", type });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<HistoryEvent>> GetHistoryAsync(string path, HistoryType? type, int limit)
        {
            string sql = "SELECT * FROM history WHERE fullname LIKE @Fullname AND type = @type ORDER BY timestamp DESC LIMIT @limit;";
            return await _semaphore.QueryAsync<HistoryEvent>(sql, new { Fullname = $"%{path}%", type, limit });
        }

        #endregion

        public async Task<DateTime> GetLastSyncTimeAsync()
        {
            string sql = "SELECT lastupdate FROM objects ORDER BY lastupdate DESC LIMIT 1;";
            return UnixTime.FromMilliseconds(await _semaphore.QuerySingleAsync<long>(sql)).ToLocalTime();
        }

        /// <inheritdoc />
        public async Task<bool> AddSnapshotAsync(string snapshot)
        {
            if (string.IsNullOrEmpty(snapshot)) throw new ArgumentNullException(nameof(snapshot));
            string sql = "INSERT OR REPLACE INTO snapshots (timestamp, name) VALUES(@Timestamp, @Name);";
            return await _semaphore.ExecuteAsync(sql, new { Timestamp = UnixTime.Now.TotalMilliseconds, Name = snapshot }) > 0;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> GetSnapshotsAsync()
        {
            string sql = "SELECT DISTINCT name FROM snapshots ORDER BY timestamp DESC;";
            return await _semaphore.QueryAsync<string>(sql);
        }

        public async Task<string?> GetSnapshotAsync(string? name)
        {
            string sql = "SELECT DISTINCT name FROM snapshots WHERE name LIKE @Name ORDER BY timestamp DESC LIMIT 1;";
            return await _semaphore.QuerySingleAsync<string>(sql, new { Name = $"%{name}%" });
        }

        /// <inheritdoc />
        public async Task RemoveSnapshotAsync(string snapshot)
        {
            string sql = $"DELETE FROM snapshots WHERE name = @Name;";
            await _semaphore.ExecuteAsync(sql, new { Name = snapshot });
        }
    }
}