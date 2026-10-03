// SPDX-License-Identifier: GPL-3.0-or-later
using Dapper;
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

public class DatabaseMigratorTests : SqliteRepositoryTestBase
{
    [Fact]
    public void MigrateToLatest_CreatesAllExpectedTables()
    {
        using var connection = ConnectionFactory.CreateOpenConnection();
        var tableNames = connection.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;");

        Assert.Contains("Games", tableNames);
        Assert.Contains("Systems", tableNames);
        Assert.Contains("GameOwnership", tableNames);
        Assert.Contains("SchemaVersion", tableNames);
    }

    [Fact]
    public void MigrateToLatest_IsIdempotent_WhenCalledTwice()
    {
        var migrator = new DatabaseMigrator(ConnectionFactory, BackupDirectory);

        migrator.MigrateToLatest();

        using var connection = ConnectionFactory.CreateOpenConnection();
        var version = connection.QuerySingle<int>("SELECT MAX(Version) FROM SchemaVersion;");
        Assert.Equal(3, version);
    }

    [Fact]
    public void MigrateToLatest_CreatesBackupFile_WhenApplyingAPendingMigrationToExistingData()
    {
        using (var connection = ConnectionFactory.CreateOpenConnection())
        {
            connection.Execute("INSERT INTO Systems (Key, Name) VALUES ('snes', 'Super Nintendo');");
            // Roll the recorded schema version back to simulate a pending
            // migration on a database that already holds user data. The base
            // fixture (SqliteRepositoryTestBase) has already migrated to the
            // latest version, so the schema changes of every migration past
            // 0001 must be undone here too, or re-applying them below would
            // fail (e.g. a column that already exists).
            connection.Execute("ALTER TABLE Systems DROP COLUMN IconPath;");
            connection.Execute("ALTER TABLE Games DROP COLUMN PlayMode;");
            connection.Execute("ALTER TABLE Games DROP COLUMN IsPortable;");
            connection.Execute("DELETE FROM SchemaVersion;");
        }

        var backupCountBefore = Directory.Exists(BackupDirectory)
            ? Directory.GetFiles(BackupDirectory, "romscollect_*.db").Length
            : 0;

        new DatabaseMigrator(ConnectionFactory, BackupDirectory).MigrateToLatest();

        var backupCountAfter = Directory.GetFiles(BackupDirectory, "romscollect_*.db").Length;
        Assert.True(backupCountAfter > backupCountBefore);

        using var verifyConnection = ConnectionFactory.CreateOpenConnection();
        var systemCount = verifyConnection.QuerySingle<int>("SELECT COUNT(*) FROM Systems;");
        Assert.Equal(1, systemCount);
    }
}
