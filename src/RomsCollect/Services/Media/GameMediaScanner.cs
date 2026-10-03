// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Services.Media;

/// <summary>
/// Matches media files under <c>data/Media/&lt;system&gt;/&lt;MediaType&gt;/</c>
/// to games by file name (without extension), following RomsCollect's own
/// media folder convention.
/// </summary>
public sealed class GameMediaScanner
{
    private readonly SystemRepository _systems;
    private readonly GameRepository _games;
    private readonly GameMediaRepository _media;
    private readonly string _mediaRootDirectory;

    public GameMediaScanner(
        SystemRepository systems,
        GameRepository games,
        GameMediaRepository media,
        string mediaRootDirectory)
    {
        _systems = systems;
        _games = games;
        _media = media;
        _mediaRootDirectory = mediaRootDirectory;
    }

    /// <summary>
    /// Scans every configured system's media directory and links any
    /// unmatched file to the game whose ROM file name (without extension)
    /// matches. Returns the number of newly linked media assets.
    /// </summary>
    public int ScanAndLink()
    {
        var linkedCount = 0;

        foreach (var system in _systems.GetAll())
        {
            var systemMediaDirectory = Path.Combine(_mediaRootDirectory, system.Key);
            if (!Directory.Exists(systemMediaDirectory))
            {
                continue;
            }

            var gamesByRomFileName = _games.GetBySystem(system.Id)
                .Where(g => g.RomPath is not null)
                .ToDictionary(g => Path.GetFileNameWithoutExtension(g.RomPath!), StringComparer.OrdinalIgnoreCase);

            foreach (var mediaTypeDirectory in Directory.EnumerateDirectories(systemMediaDirectory))
            {
                var mediaType = Path.GetFileName(mediaTypeDirectory);

                foreach (var filePath in Directory.EnumerateFiles(mediaTypeDirectory))
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                    if (!gamesByRomFileName.TryGetValue(fileNameWithoutExtension, out var game))
                    {
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(_mediaRootDirectory, filePath);
                    var alreadyLinked = _media.GetByGameAndType(game.Id, mediaType)
                        .Any(m => string.Equals(m.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));
                    if (alreadyLinked)
                    {
                        continue;
                    }

                    _media.Add(new GameMedia
                    {
                        GameId = game.Id,
                        MediaType = mediaType,
                        RelativePath = relativePath,
                    });
                    linkedCount++;
                }
            }
        }

        return linkedCount;
    }
}
