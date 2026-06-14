// Copyright 2026 Entex Interactive

using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Parallel.Core.Models;

namespace Parallel.Core.Database.Contexts
{
    /// <summary>
    /// Represents a way to limit database queries using a <see cref="SemaphoreSlim"/>.
    /// </summary>
    public sealed class SemaphoreContext
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemaphoreContext"/> class.
        /// </summary>
        /// <param name="connectionString"></param>
        public SemaphoreContext(string connectionString)
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        {
            await _semaphore.WaitAsync();

            try
            {
                using IDbConnection connection = CreateConnection();
                return await connection.ExecuteAsync(sql, parameters);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<T?> QuerySingleAsync<T>(string sql, object? parameters = null)
        {
            await _semaphore.WaitAsync();

            try
            {
                using IDbConnection connection = CreateConnection();
                return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null)
        {
            await _semaphore.WaitAsync();

            try
            {
                using IDbConnection connection = CreateConnection();
                return (await connection.QueryAsync<T>(sql, parameters)).AsList();
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}