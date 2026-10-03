// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Text.Json;

namespace RomsCollect.Helpers;

/// <summary>
/// Resolves user-facing strings from the i18n JSON files shipped next to the
/// executable. Falls back to the raw key when no translation is found, so a
/// missing entry is always visible rather than silently blank.
/// </summary>
public sealed class LocalizationService
{
    private readonly Dictionary<string, string> _strings = new();

    public LocalizationService(string languageCode = "en")
    {
        var path = Path.Combine(AppContext.BaseDirectory, "i18n", $"{languageCode}.json");
        if (!File.Exists(path))
        {
            return;
        }

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                _strings[property.Name] = property.Value.GetString() ?? property.Name;
            }
        }
    }

    public string this[string key] => _strings.TryGetValue(key, out var value) ? value : key;
}
