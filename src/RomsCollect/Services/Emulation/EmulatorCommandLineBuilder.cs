// SPDX-License-Identifier: GPL-3.0-or-later
namespace RomsCollect.Services.Emulation;

/// <summary>
/// Expands an emulator command-line template (e.g. <c>-L core.dll "{RomPath}"</c>)
/// by substituting the <c>{RomPath}</c> placeholder with the ROM's full path,
/// quoted so paths containing spaces or special characters are passed to the
/// emulator process as a single argument.
/// </summary>
public static class EmulatorCommandLineBuilder
{
    private const string RomPathPlaceholder = "{RomPath}";
    private const string QuotedRomPathPlaceholder = "\"{RomPath}\"";

    public static string Build(string commandLineTemplate, string romFullPath)
    {
        var quotedRomPath = $"\"{romFullPath}\"";

        // A template that already wraps the placeholder in quotes (every
        // preset and the default custom template do: "-L \"{CorePath}\"
        // \"{RomPath}\"") must not have the ROM path quoted a second time,
        // or the emulator receives a doubled-quote argument that most
        // command-line parsers do not treat as the intended single path.
        if (commandLineTemplate.Contains(QuotedRomPathPlaceholder, StringComparison.Ordinal))
        {
            return commandLineTemplate.Replace(QuotedRomPathPlaceholder, quotedRomPath, StringComparison.Ordinal);
        }

        return commandLineTemplate.Contains(RomPathPlaceholder, StringComparison.Ordinal)
            ? commandLineTemplate.Replace(RomPathPlaceholder, quotedRomPath, StringComparison.Ordinal)
            : $"{commandLineTemplate} {quotedRomPath}";
    }
}
