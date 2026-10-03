// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.Data.Sqlite;
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

/// <summary>
/// Base class that gives each test a fresh, migrated SQLite database file in
/// a temporary directory, disposed and deleted after the test runs.
/// </summary>
public abstract class SqliteRepositoryTestBase : IDisposable
{
    private readonly string _tempDirectory;

    protected SqliteRepositoryTestBase()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "RomsCollectTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);

        ConnectionFactory = new SqliteConnectionFactory(Path.Combine(_tempDirectory, "test.db"));
        BackupDirectory = Path.Combine(_tempDirectory, "Backup");
        new DatabaseMigrator(ConnectionFactory, BackupDirectory).MigrateToLatest();
    }

    protected SqliteConnectionFactory ConnectionFactory { get; }
    protected string BackupDirectory { get; }

    public virtual void Dispose()
    {
        // Microsoft.Data.Sqlite pools connections per connection string, which
        // keeps the database file locked even after each connection's own
        // Dispose(). Clearing the pool here is required for the temp
        // directory below to be deletable.
        SqliteConnection.ClearAllPools();

        Directory.Delete(_tempDirectory, recursive: true);
    }
}
