// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Models;

/// <summary>
/// Result of a collection scan, shown to the user before any change is
/// committed. Scanning never writes to the database silently.
/// </summary>
public sealed class ScanReport
{
    public List<ScanReportEntry> Added { get; } = [];
    public List<ScanReportEntry> Updated { get; } = [];
    public List<ScanReportEntry> Unchanged { get; } = [];
    public List<ScanReportEntry> Errors { get; } = [];
}

/// <summary>One file-level outcome of a collection scan.</summary>
public sealed record ScanReportEntry(string SystemKey, string RelativeRomPath, string? RomHash = null, string? Message = null);
