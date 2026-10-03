// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace RomsCollect.Services.Database;

/// <summary>
/// Applies versioned, idempotent SQL migration scripts embedded as resources
/// under <c>Data/Migrations/NNNN_description.sql</c>. Before applying any
/// pending migration to an existing database file, the current file is
/// copied to <c>data/Backup/</c> so a failed migration never destroys data.
/// </summary>
public sealed class DatabaseMigrator
{
    private static readonly Regex MigrationFileNamePattern = new(
        @"(?<version>\d+)_[^.]+\.sql$", RegexOptions.Compiled);

    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly string _backupDirectory;
    private readonly Assembly _resourceAssembly;

    /// <param name="connectionFactory">Provides the database file to migrate.</param>
    /// <param name="backupDirectory">
    /// Directory to copy the database file into before applying a pending
    /// migration. Defaults to <c>data/Backup</c> relative to the executable,
    /// matching the portable directory convention; tests should pass an
    /// isolated temporary directory instead.
    /// </param>
    public DatabaseMigrator(SqliteConnectionFactory connectionFactory, string? backupDirectory = null)
    {
        _connectionFactory = connectionFactory;
        _backupDirectory = backupDirectory ?? Path.Combine(AppContext.BaseDirectory, "data", "Backup");
        _resourceAssembly = typeof(DatabaseMigrator).Assembly;
    }

    /// <summary>Applies every migration whose version is greater than the current schema version.</summary>
    public void MigrateToLatest()
    {
        var migrations = LoadEmbeddedMigrations();
        if (migrations.Count == 0)
        {
            return;
        }

        using var connection = _connectionFactory.CreateOpenConnection();
        var currentVersion = GetCurrentVersion(connection);
        var pending = migrations.Where(m => m.Version > currentVersion).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        BackupDatabaseFileIfExists();

        foreach (var migration in pending)
        {
            using var transaction = connection.BeginTransaction();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = migration.Sql;
                command.ExecuteNonQuery();
            }

            using (var versionCommand = connection.CreateCommand())
            {
                versionCommand.Transaction = transaction;
                versionCommand.CommandText =
                    "INSERT INTO SchemaVersion (Version, AppliedAt) VALUES ($version, $appliedAt);";
                versionCommand.Parameters.AddWithValue("$version", migration.Version);
                versionCommand.Parameters.AddWithValue("$appliedAt", DateTime.UtcNow.ToString("O"));
                versionCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }

    private static int GetCurrentVersion(SqliteConnection connection)
    {
        using var tableCheck = connection.CreateCommand();
        tableCheck.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'SchemaVersion';";
        var tableExists = Convert.ToInt64(tableCheck.ExecuteScalar()) > 0;
        if (!tableExists)
        {
            return 0;
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(Version), 0) FROM SchemaVersion;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private void BackupDatabaseFileIfExists()
    {
        var databasePath = _connectionFactory.DatabasePath;
        if (!File.Exists(databasePath))
        {
            return;
        }

        Directory.CreateDirectory(_backupDirectory);

        var backupFileName = $"romscollect_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.db";
        File.Copy(databasePath, Path.Combine(_backupDirectory, backupFileName), overwrite: false);
    }

    private List<Migration> LoadEmbeddedMigrations()
    {
        var migrations = new List<Migration>();

        foreach (var resourceName in _resourceAssembly.GetManifestResourceNames())
        {
            var match = MigrationFileNamePattern.Match(resourceName);
            if (!match.Success)
            {
                continue;
            }

            var version = int.Parse(match.Groups["version"].Value);

            using var stream = _resourceAssembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded migration resource '{resourceName}' could not be opened.");
            using var reader = new StreamReader(stream);
            migrations.Add(new Migration(version, reader.ReadToEnd()));
        }

        return migrations.OrderBy(m => m.Version).ToList();
    }

    private sealed record Migration(int Version, string Sql);
}
