// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;

namespace RomsCollect.Services;

/// <summary>
/// Resolves every portable data directory relative to the executable, per
/// RomsCollect's fixed directory convention (a per-system ROM folder
/// naming convention shared with several existing ROM collections, and
/// its own per-system media-type folder convention).
/// </summary>
public sealed class PortableDataPaths
{
    public PortableDataPaths(string? baseDirectory = null)
    {
        BaseDirectory = baseDirectory ?? AppContext.BaseDirectory;
    }

    public string BaseDirectory { get; }

    public string DataRoot => Path.Combine(BaseDirectory, "data");
    public string RomsRoot => Path.Combine(DataRoot, "Roms");
    public string MediaRoot => Path.Combine(DataRoot, "Media");
    public string DatabaseFile => Path.Combine(DataRoot, "Database", "romscollect.db");
    public string BiosRoot => Path.Combine(DataRoot, "Bios");
    public string SavesRoot => Path.Combine(DataRoot, "Saves");
    public string ScreenshotsRoot => Path.Combine(DataRoot, "Screenshots");
    public string ThemesRoot => Path.Combine(DataRoot, "Themes");
    public string LogsRoot => Path.Combine(DataRoot, "Logs");
    public string BackupRoot => Path.Combine(DataRoot, "Backup");

    public string ResolveMediaPath(string relativePath) => Path.Combine(MediaRoot, relativePath);

    public string ResolveRomPath(string systemKey, string relativeRomPath)
        => Path.Combine(RomsRoot, systemKey, relativeRomPath);
}
