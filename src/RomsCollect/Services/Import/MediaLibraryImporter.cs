// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Xml.Linq;
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Services.Import;

/// <summary>One game entry read from a platform metadata XML file.</summary>
public sealed record MediaLibraryGameEntry(
    string Title,
    string? RomFileName,
    string? Description,
    string? Developer,
    string? Publisher,
    string? ReleaseDate,
    IReadOnlyList<string> Genres);

/// <summary>
/// Reads a platform metadata XML file (one <c>&lt;Game&gt;</c> element per
/// entry, matching the common per-system <c>Platforms/&lt;Platform&gt;.xml</c>
/// layout used by several existing game library managers) and imports its
/// text metadata (title, description, developer, publisher, release year,
/// genre) into the corresponding <see cref="Game"/> rows, matched by ROM
/// file name. This never reads or writes anything outside the local file
/// system: no network access is performed.
/// </summary>
public sealed class MediaLibraryImporter
{
    private readonly GameRepository _games;

    public MediaLibraryImporter(GameRepository games)
    {
        _games = games;
    }

    /// <summary>Parses a single platform metadata XML file into game entries.</summary>
    public static IReadOnlyList<MediaLibraryGameEntry> ParsePlatformXml(string xmlFilePath)
    {
        var document = XDocument.Load(xmlFilePath);
        var entries = new List<MediaLibraryGameEntry>();

        foreach (var gameElement in document.Root?.Elements("Game") ?? Enumerable.Empty<XElement>())
        {
            var title = gameElement.Element("Title")?.Value;
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var applicationPath = gameElement.Element("ApplicationPath")?.Value;
            var romFileName = string.IsNullOrWhiteSpace(applicationPath)
                ? null
                : Path.GetFileName(applicationPath);

            var genres = (gameElement.Element("Genre")?.Value ?? string.Empty)
                .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            entries.Add(new MediaLibraryGameEntry(
                Title: title,
                RomFileName: romFileName,
                Description: gameElement.Element("Notes")?.Value,
                Developer: gameElement.Element("Developer")?.Value,
                Publisher: gameElement.Element("Publisher")?.Value,
                ReleaseDate: gameElement.Element("ReleaseDate")?.Value,
                Genres: genres));
        }

        return entries;
    }

    /// <summary>
    /// Applies the text metadata of every parsed entry to the matching
    /// <see cref="Game"/> row of <paramref name="systemId"/> (matched by ROM
    /// file name, without directory). Entries with no matching game are
    /// skipped and counted in the returned total.
    /// </summary>
    public int ApplyMetadata(int systemId, IReadOnlyList<MediaLibraryGameEntry> entries)
    {
        var gamesByRomFileName = _games.GetBySystem(systemId)
            .Where(g => g.RomPath is not null)
            .ToDictionary(g => Path.GetFileName(g.RomPath!), StringComparer.OrdinalIgnoreCase);

        var matchedCount = 0;

        foreach (var entry in entries)
        {
            if (entry.RomFileName is null || !gamesByRomFileName.TryGetValue(entry.RomFileName, out var game))
            {
                continue;
            }

            game.Title = entry.Title;
            game.Description = entry.Description ?? game.Description;
            game.ReleaseDate = entry.ReleaseDate ?? game.ReleaseDate;
            game.DateModified = DateTime.UtcNow;
            _games.Update(game);

            if (entry.Developer is not null)
            {
                _games.SetDevelopers(game.Id, [entry.Developer]);
            }

            if (entry.Publisher is not null)
            {
                _games.SetPublishers(game.Id, [entry.Publisher]);
            }

            if (entry.Genres.Count > 0)
            {
                _games.SetGenres(game.Id, entry.Genres);
            }

            matchedCount++;
        }

        return matchedCount;
    }
}
