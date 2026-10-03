// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows.Documents;

namespace RomsCollect.Helpers;

/// <summary>How one parsed segment of description markup should be rendered.</summary>
public enum MarkupSpanStyle
{
    Plain,
    Bold,
    Italic,
    Bullet,
    LineBreak,
}

/// <summary>One parsed segment of description markup, carrying its text and how it should be rendered.</summary>
public readonly record struct MarkupSpan(MarkupSpanStyle Style, string Text);

/// <summary>
/// Parses the minimal description markup used by the game-edit dialog's
/// Description tab — "**bold**", "*italic*", and "- " bullet lines — into a
/// flat list of <see cref="MarkupSpan"/>. Deliberately not a general
/// Markdown parser: only the three constructs the editor's toolbar can
/// insert are recognized, per the "lightweight markup, no HTML/RTF
/// dependency" scope. Parsing itself has no WPF dependency, so it is
/// testable headlessly; <see cref="AppendInlinesTo"/> is the only part that
/// needs a UI thread, turning the parsed spans into WPF <see cref="Inline"/>
/// runs for display.
/// </summary>
public static class LightweightMarkupParser
{
    public static IReadOnlyList<MarkupSpan> Parse(string text)
    {
        var spans = new List<MarkupSpan>();
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                spans.Add(new MarkupSpan(MarkupSpanStyle.Bullet, line[2..]));
            }
            else
            {
                ParseInlineSpans(spans, line);
            }

            if (i < lines.Length - 1)
            {
                spans.Add(new MarkupSpan(MarkupSpanStyle.LineBreak, string.Empty));
            }
        }

        return spans;
    }

    private static void ParseInlineSpans(List<MarkupSpan> spans, string line)
    {
        var position = 0;
        while (position < line.Length)
        {
            // "**bold**" is checked before a lone "*italic*" at the same position.
            if (line.AsSpan(position).StartsWith("**"))
            {
                var boldEnd = line.IndexOf("**", position + 2, StringComparison.Ordinal);
                if (boldEnd > -1)
                {
                    spans.Add(new MarkupSpan(MarkupSpanStyle.Bold, line[(position + 2)..boldEnd]));
                    position = boldEnd + 2;
                    continue;
                }
            }
            else if (line[position] == '*')
            {
                var italicEnd = line.IndexOf('*', position + 1);
                if (italicEnd > -1)
                {
                    spans.Add(new MarkupSpan(MarkupSpanStyle.Italic, line[(position + 1)..italicEnd]));
                    position = italicEnd + 1;
                    continue;
                }
            }

            // No valid marker here (or an unterminated one): take plain text
            // up to the next '*' so an unterminated marker is retried there.
            var nextMarker = line.IndexOf('*', position + 1);
            var segmentEnd = nextMarker == -1 ? line.Length : nextMarker;
            spans.Add(new MarkupSpan(MarkupSpanStyle.Plain, line[position..segmentEnd]));
            position = segmentEnd;
        }
    }

    /// <summary>Renders <paramref name="text"/> into WPF inlines, appended to <paramref name="inlines"/>.</summary>
    [SupportedOSPlatform("windows")]
    public static void AppendInlinesTo(InlineCollection inlines, string text)
    {
        foreach (var span in Parse(text))
        {
            Inline inline = span.Style switch
            {
                MarkupSpanStyle.Bold => new Bold(new Run(span.Text)),
                MarkupSpanStyle.Italic => new Italic(new Run(span.Text)),
                MarkupSpanStyle.Bullet => new Run("• " + span.Text),
                MarkupSpanStyle.LineBreak => new LineBreak(),
                _ => new Run(span.Text),
            };
            inlines.Add(inline);
        }
    }
}
