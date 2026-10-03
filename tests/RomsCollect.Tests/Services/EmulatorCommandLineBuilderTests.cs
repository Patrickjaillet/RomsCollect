// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Services.Emulation;

namespace RomsCollect.Tests.Services;

public class EmulatorCommandLineBuilderTests
{
    [Fact]
    public void Build_SubstitutesRomPathPlaceholder_WithQuotedPath()
    {
        var result = EmulatorCommandLineBuilder.Build("-L core.dll {RomPath}", @"C:\Roms\Chrono Trigger.sfc");

        Assert.Equal("-L core.dll \"C:\\Roms\\Chrono Trigger.sfc\"", result);
    }

    [Fact]
    public void Build_AppendsQuotedPath_WhenTemplateHasNoPlaceholder()
    {
        var result = EmulatorCommandLineBuilder.Build("--fullscreen", @"C:\Roms\game.sfc");

        Assert.Equal("--fullscreen \"C:\\Roms\\game.sfc\"", result);
    }

    [Fact]
    public void Build_HandlesPathsWithSpacesAndSpecialCharacters()
    {
        var result = EmulatorCommandLineBuilder.Build("\"{RomPath}\"", @"C:\My Roms (backup)\Game & Friends.sfc");

        Assert.Equal("\"C:\\My Roms (backup)\\Game & Friends.sfc\"", result);
    }

    [Fact]
    public void Build_DoesNotDoubleQuote_WhenTemplateAlreadyWrapsThePlaceholderInQuotes()
    {
        // The default custom template ("\"{RomPath}\"", set in
        // SettingsViewModel) and every pre-filled preset (RetroArch,
        // Dolphin, PCSX2, DuckStation) already wrap {RomPath} in literal
        // quotes in the template itself.
        var result = EmulatorCommandLineBuilder.Build("\"{RomPath}\"", @"C:\Roms\game.sfc");

        Assert.Equal("\"C:\\Roms\\game.sfc\"", result);
    }

    [Fact]
    public void Build_DoesNotDoubleQuote_ForARealPresetTemplateWithACorePathPlaceholderToo()
    {
        var result = EmulatorCommandLineBuilder.Build("-L \"{CorePath}\" \"{RomPath}\"", @"C:\Roms\game.sfc");

        Assert.Equal("-L \"{CorePath}\" \"C:\\Roms\\game.sfc\"", result);
    }

    [Fact]
    public void Build_SubstitutesMultipleOccurrencesOfPlaceholder()
    {
        var result = EmulatorCommandLineBuilder.Build("{RomPath} --verify={RomPath}", @"C:\Roms\game.sfc");

        Assert.Equal("\"C:\\Roms\\game.sfc\" --verify=\"C:\\Roms\\game.sfc\"", result);
    }
}
