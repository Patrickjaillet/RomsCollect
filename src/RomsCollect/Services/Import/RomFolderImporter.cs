// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using RomsCollect.Services.Database;

namespace RomsCollect.Services.Import;

/// <summary>How an existing ROM file is brought into the portable data tree.</summary>
public enum RomImportMode
{
    Copy,
    SymbolicLink,
}

/// <summary>One system directory found in an existing roms/ tree, ready to import.</summary>
public sealed record RomImportCandidate(string SystemKey, string SourceDirectory, int RomFileCount);

/// <summary>
/// Detects an existing <c>roms/&lt;system&gt;/</c> tree (using the same
/// per-system directory naming convention RomsCollect itself uses) and
/// imports its ROM files into <c>data/Roms/&lt;system&gt;/</c>, either by
/// copying or by creating a symbolic link, at the user's choice. Only file
/// system operations are performed; no network access is ever involved.
/// </summary>
public sealed class RomFolderImporter
{
    private readonly SystemRepository _systems;
    private readonly string _destinationRomsRootDirectory;

    public RomFolderImporter(SystemRepository systems, string destinationRomsRootDirectory)
    {
        _systems = systems;
        _destinationRomsRootDirectory = destinationRomsRootDirectory;
    }

    /// <summary>
    /// Detects which subdirectories of <paramref name="sourceRomsDirectory"/>
    /// match a configured system key and contain at least one file with a
    /// known ROM extension for that system.
    /// </summary>
    public IReadOnlyList<RomImportCandidate> DetectCandidates(string sourceRomsDirectory)
    {
        if (!Directory.Exists(sourceRomsDirectory))
        {
            return [];
        }

        var systemsByKey = _systems.GetAll().ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        var candidates = new List<RomImportCandidate>();

        foreach (var directory in Directory.EnumerateDirectories(sourceRomsDirectory))
        {
            var systemKey = Path.GetFileName(directory);
            if (!systemsByKey.TryGetValue(systemKey, out var system))
            {
                continue;
            }

            var extensions = system.RomExtensions
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (extensions.Count == 0)
            {
                continue;
            }

            var romCount = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Count(f => extensions.Contains(Path.GetExtension(f)));
            if (romCount > 0)
            {
                candidates.Add(new RomImportCandidate(systemKey, directory, romCount));
            }
        }

        return candidates;
    }

    /// <summary>Imports every ROM file of one candidate into the portable data tree.</summary>
    public void Import(RomImportCandidate candidate, RomImportMode mode)
    {
        var destinationDirectory = Path.Combine(_destinationRomsRootDirectory, candidate.SystemKey);
        Directory.CreateDirectory(destinationDirectory);

        foreach (var sourceFilePath in Directory.EnumerateFiles(candidate.SourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(candidate.SourceDirectory, sourceFilePath);
            var destinationFilePath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);

            if (File.Exists(destinationFilePath))
            {
                continue;
            }

            switch (mode)
            {
                case RomImportMode.Copy:
                    File.Copy(sourceFilePath, destinationFilePath);
                    break;
                case RomImportMode.SymbolicLink:
                    File.CreateSymbolicLink(destinationFilePath, sourceFilePath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}
