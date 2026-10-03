// SPDX-License-Identifier: GPL-3.0-or-later
using Dapper;
using RomsCollect.Models;

namespace RomsCollect.Services.Database;

/// <summary>CRUD access to the EmulatorProfiles table.</summary>
public sealed class EmulatorProfileRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public EmulatorProfileRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public int Add(EmulatorProfile profile)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO EmulatorProfiles (SystemId, Name, ExecutablePath, CommandLineTemplate, WorkingDirectory, IsDefault)
            VALUES (@SystemId, @Name, @ExecutablePath, @CommandLineTemplate, @WorkingDirectory, @IsDefault);
            SELECT last_insert_rowid();
            """;
        return connection.ExecuteScalar<int>(sql, profile);
    }

    public EmulatorProfile? GetById(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM EmulatorProfiles WHERE Id = @Id;";
        return connection.QuerySingleOrDefault<EmulatorProfile>(sql, new { Id = id });
    }

    public IReadOnlyList<EmulatorProfile> GetBySystem(int systemId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM EmulatorProfiles WHERE SystemId = @SystemId ORDER BY Name;";
        return connection.Query<EmulatorProfile>(sql, new { SystemId = systemId }).ToList();
    }

    public EmulatorProfile? GetDefaultForSystem(int systemId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            SELECT * FROM EmulatorProfiles
            WHERE SystemId = @SystemId AND IsDefault = 1
            LIMIT 1;
            """;
        return connection.QuerySingleOrDefault<EmulatorProfile>(sql, new { SystemId = systemId });
    }

    public void Update(EmulatorProfile profile)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            UPDATE EmulatorProfiles SET
                Name = @Name, ExecutablePath = @ExecutablePath, CommandLineTemplate = @CommandLineTemplate,
                WorkingDirectory = @WorkingDirectory, IsDefault = @IsDefault
            WHERE Id = @Id;
            """;
        connection.Execute(sql, profile);
    }

    public void Delete(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM EmulatorProfiles WHERE Id = @Id;";
        connection.Execute(sql, new { Id = id });
    }
}
