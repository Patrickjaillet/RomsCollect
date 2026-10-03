// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Services.Scanning;

/// <summary>
/// Scans <c>data/Roms/&lt;system&gt;/</c> recursively for each configured
/// system, matches files against known ROM extensions, and reports what
/// would be added/updated/unchanged — nothing is written to the database
/// until the caller explicitly commits the report via <see cref="Commit"/>.
/// </summary>
public sealed class CollectionScanner
{
    private readonly SystemRepository _systems;
    private readonly GameRepository _games;
    private readonly string _romsRootDirectory;
    private readonly RomHashAlgorithm _hashAlgorithm;

    public CollectionScanner(
        SystemRepository systems,
        GameRepository games,
        string romsRootDirectory,
        RomHashAlgorithm hashAlgorithm = RomHashAlgorithm.Crc32)
    {
        _systems = systems;
        _games = games;
        _romsRootDirectory = romsRootDirectory;
        _hashAlgorithm = hashAlgorithm;
    }

    /// <summary>
    /// Scans every configured system's ROM directory and returns a report
    /// describing what would change, without touching the database.
    /// </summary>
    /// <param name="systemKey">
    /// When set, only this system's ROM directory is scanned instead of
    /// every configured system.
    /// </param>
    /// <param name="progress">
    /// Reports the key of the system currently being scanned, so the UI can
    /// show per-system feedback during a scan of the whole collection.
    /// </param>
    public ScanReport Scan(string? systemKey = null, IProgress<string>? progress = null)
    {
        var report = new ScanReport();

        var systemsToScan = systemKey is null
            ? _systems.GetAll()
            : _systems.GetAll().Where(s => string.Equals(s.Key, systemKey, StringComparison.OrdinalIgnoreCase));

        foreach (var system in systemsToScan)
        {
            progress?.Report(system.Key);

            var systemDirectory = Path.Combine(_romsRootDirectory, system.Key);
            if (!Directory.Exists(systemDirectory))
            {
                continue;
            }

            var extensions = ParseExtensions(system.RomExtensions);
            if (extensions.Count == 0)
            {
                continue;
            }

            var existingGames = _games.GetBySystem(system.Id)
                .Where(g => g.RomPath is not null)
                .ToDictionary(g => g.RomPath!, StringComparer.OrdinalIgnoreCase);
            var seenRomPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var filePath in Directory.EnumerateFiles(systemDirectory, "*", SearchOption.AllDirectories))
            {
                if (!extensions.Contains(Path.GetExtension(filePath)))
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(systemDirectory, filePath);
                seenRomPaths.Add(relativePath);

                try
                {
                    var hash = RomHasher.ComputeHash(filePath, _hashAlgorithm);

                    if (existingGames.TryGetValue(relativePath, out var existingGame))
                    {
                        if (!string.Equals(existingGame.RomHash, hash, StringComparison.OrdinalIgnoreCase))
                        {
                            report.Updated.Add(new ScanReportEntry(system.Key, relativePath, hash, "ROM content changed"));
                        }
                        else
                        {
                            report.Unchanged.Add(new ScanReportEntry(system.Key, relativePath, hash));
                        }
                    }
                    else
                    {
                        report.Added.Add(new ScanReportEntry(system.Key, relativePath, hash));
                    }
                }
                catch (IOException ex)
                {
                    report.Errors.Add(new ScanReportEntry(system.Key, relativePath, Message: ex.Message));
                }
                catch (UnauthorizedAccessException ex)
                {
                    report.Errors.Add(new ScanReportEntry(system.Key, relativePath, Message: ex.Message));
                }
            }
        }

        return report;
    }

    /// <summary>
    /// Applies a previously produced <see cref="ScanReport"/> to the
    /// database: inserts new games and updates the stored hash of changed
    /// ones. Unchanged entries and errors are not written.
    /// </summary>
    public void Commit(ScanReport report)
    {
        var systemsByKey = _systems.GetAll().ToDictionary(s => s.Key);

        foreach (var entry in report.Added)
        {
            if (!systemsByKey.TryGetValue(entry.SystemKey, out var system))
            {
                continue;
            }

            var title = Path.GetFileNameWithoutExtension(entry.RelativeRomPath);
            var now = DateTime.UtcNow;

            _games.Add(new Game
            {
                SystemId = system.Id,
                Title = title,
                SortTitle = title,
                RomPath = entry.RelativeRomPath,
                RomHash = entry.RomHash,
                DateAdded = now,
                DateModified = now,
            });
        }

        foreach (var entry in report.Updated)
        {
            if (!systemsByKey.TryGetValue(entry.SystemKey, out var system))
            {
                continue;
            }

            var existingGame = _games.GetBySystemAndRomPath(system.Id, entry.RelativeRomPath);
            if (existingGame is null)
            {
                continue;
            }

            existingGame.RomHash = entry.RomHash;
            existingGame.DateModified = DateTime.UtcNow;
            _games.Update(existingGame);
        }
    }

    private static HashSet<string> ParseExtensions(string commaSeparatedExtensions)
        => commaSeparatedExtensions
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
