// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;

namespace RomsCollect.ViewModels;

/// <summary>
/// A pre-filled command-line template for a common emulator. RomsCollect
/// never downloads or bundles any emulator — this only provides the text
/// template the user applies to an executable they already have installed.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed record EmulatorPreset(string Name, string CommandLineTemplate)
{
    // Computed on each access (rather than a static-initialized field) so
    // the localized "Custom" label always reflects App.Localization, with
    // no dependency on static field initialization order.
    public static IReadOnlyList<EmulatorPreset> All =>
    [
        new("RetroArch", "-L \"{CorePath}\" \"{RomPath}\""),
        new("Dolphin", "-b -e \"{RomPath}\""),
        new("PCSX2", "-- \"{RomPath}\""),
        new("DuckStation", "-batch \"{RomPath}\""),
        new(App.Localization["Settings.Emulators.Preset.Custom"], "\"{RomPath}\""),
    ];
}
