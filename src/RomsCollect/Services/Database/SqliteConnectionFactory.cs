// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using Microsoft.Data.Sqlite;

namespace RomsCollect.Services.Database;

/// <summary>
/// Creates ADO.NET connections to the single portable SQLite database at
/// <c>data/Database/romscollect.db</c>, relative to the executable.
/// </summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string? databaseFilePath = null)
    {
        DatabasePath = databaseFilePath
            ?? Path.Combine(AppContext.BaseDirectory, "data", "Database", "romscollect.db");

        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            ForeignKeys = true,
        }.ToString();
    }

    public string DatabasePath { get; }

    public SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
