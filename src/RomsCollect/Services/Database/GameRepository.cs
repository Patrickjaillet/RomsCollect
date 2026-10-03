// SPDX-License-Identifier: GPL-3.0-or-later
using Dapper;
using RomsCollect.Models;

namespace RomsCollect.Services.Database;

/// <summary>
/// CRUD access to the Games table, plus helpers for the list-valued catalog
/// fields (Developers, Publishers, Genres) and personal Tags, stored as
/// many-to-many links to reusable lookup dictionaries.
/// </summary>
public sealed class GameRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public GameRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public int Add(Game game)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO Games (
                SystemId, Title, SortTitle, RomPath, RomHash, ReleaseDate, Series,
                AudienceRating, Barcode, Format, Edition, Region, Description,
                IsFavorite, IsCompleted, Rating, PlayMode, IsPortable, DateAdded, DateModified
            ) VALUES (
                @SystemId, @Title, @SortTitle, @RomPath, @RomHash, @ReleaseDate, @Series,
                @AudienceRating, @Barcode, @Format, @Edition, @Region, @Description,
                @IsFavorite, @IsCompleted, @Rating, @PlayMode, @IsPortable, @DateAdded, @DateModified
            );
            SELECT last_insert_rowid();
            """;
        return connection.ExecuteScalar<int>(sql, game);
    }

    public Game? GetById(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM Games WHERE Id = @Id;";
        return connection.QuerySingleOrDefault<Game>(sql, new { Id = id });
    }

    public IReadOnlyList<Game> GetBySystem(int systemId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM Games WHERE SystemId = @SystemId ORDER BY SortTitle;";
        return connection.Query<Game>(sql, new { SystemId = systemId }).ToList();
    }

    public IReadOnlyList<Game> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM Games ORDER BY SortTitle;";
        return connection.Query<Game>(sql).ToList();
    }

    public Game? GetBySystemAndRomPath(int systemId, string romPath)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT * FROM Games WHERE SystemId = @SystemId AND RomPath = @RomPath;";
        return connection.QuerySingleOrDefault<Game>(sql, new { SystemId = systemId, RomPath = romPath });
    }

    public void Update(Game game)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            UPDATE Games SET
                SystemId = @SystemId, Title = @Title, SortTitle = @SortTitle, RomPath = @RomPath,
                RomHash = @RomHash, ReleaseDate = @ReleaseDate, Series = @Series,
                AudienceRating = @AudienceRating, Barcode = @Barcode, Format = @Format,
                Edition = @Edition, Region = @Region, Description = @Description,
                IsFavorite = @IsFavorite, IsCompleted = @IsCompleted, Rating = @Rating,
                PlayMode = @PlayMode, IsPortable = @IsPortable, DateModified = @DateModified
            WHERE Id = @Id;
            """;
        connection.Execute(sql, game);
    }

    public void Delete(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM Games WHERE Id = @Id;";
        connection.Execute(sql, new { Id = id });
    }

    public IReadOnlyList<string> GetDeveloperNames(int gameId)
        => GetLinkedNames(gameId, "Developers", "GameDevelopers", "DeveloperId");

    public IReadOnlyList<string> GetPublisherNames(int gameId)
        => GetLinkedNames(gameId, "Publishers", "GamePublishers", "PublisherId");

    public IReadOnlyList<string> GetGenreNames(int gameId)
        => GetLinkedNames(gameId, "Genres", "GameGenres", "GenreId");

    /// <summary>
    /// Every distinct genre name currently used by at least one game, for
    /// populating the advanced search filter's genre list.
    /// </summary>
    public IReadOnlyList<string> GetAllUsedGenreNames()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            SELECT DISTINCT l.Name
            FROM GameGenres gl
            JOIN Genres l ON l.Id = gl.GenreId
            ORDER BY l.Name;
            """;
        return connection.Query<string>(sql).ToList();
    }

    /// <summary>
    /// The genre names of every game in the collection, in a single query —
    /// used by the advanced search filter, which would otherwise need one
    /// query per game to check a genre match.
    /// </summary>
    public IReadOnlyDictionary<int, IReadOnlyList<string>> GetGenreNamesByGame()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            SELECT gl.GameId, l.Name
            FROM GameGenres gl
            JOIN Genres l ON l.Id = gl.GenreId;
            """;
        return connection.Query(sql)
            .GroupBy(row => (int)row.GameId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(row => (string)row.Name).ToList());
    }

    public IReadOnlyList<string> GetTagNames(int gameId)
        => GetLinkedNames(gameId, "Tags", "GameTags", "TagId");

    public void SetDevelopers(int gameId, IEnumerable<string> names)
        => SetLinkedNames(gameId, names, "Developers", "GameDevelopers", "DeveloperId");

    public void SetPublishers(int gameId, IEnumerable<string> names)
        => SetLinkedNames(gameId, names, "Publishers", "GamePublishers", "PublisherId");

    public void SetGenres(int gameId, IEnumerable<string> names)
        => SetLinkedNames(gameId, names, "Genres", "GameGenres", "GenreId");

    public void SetTags(int gameId, IEnumerable<string> names)
        => SetLinkedNames(gameId, names, "Tags", "GameTags", "TagId");

    private IReadOnlyList<string> GetLinkedNames(int gameId, string lookupTable, string linkTable, string linkColumn)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        var sql = $"""
            SELECT l.Name
            FROM {linkTable} gl
            JOIN {lookupTable} l ON l.Id = gl.{linkColumn}
            WHERE gl.GameId = @GameId
            ORDER BY l.Name;
            """;
        return connection.Query<string>(sql, new { GameId = gameId }).ToList();
    }

    private void SetLinkedNames(int gameId, IEnumerable<string> names, string lookupTable, string linkTable, string linkColumn)
    {
        var distinctNames = names
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var connection = _connectionFactory.CreateOpenConnection();
        using var transaction = connection.BeginTransaction();

        connection.Execute($"DELETE FROM {linkTable} WHERE GameId = @GameId;", new { GameId = gameId }, transaction);

        foreach (var name in distinctNames)
        {
            var lookupId = connection.ExecuteScalar<int?>(
                $"SELECT Id FROM {lookupTable} WHERE Name = @Name;", new { Name = name }, transaction);

            lookupId ??= connection.ExecuteScalar<int>(
                $"INSERT INTO {lookupTable} (Name) VALUES (@Name); SELECT last_insert_rowid();",
                new { Name = name }, transaction);

            connection.Execute(
                $"INSERT INTO {linkTable} (GameId, {linkColumn}) VALUES (@GameId, @LookupId);",
                new { GameId = gameId, LookupId = lookupId }, transaction);
        }

        transaction.Commit();
    }
}
