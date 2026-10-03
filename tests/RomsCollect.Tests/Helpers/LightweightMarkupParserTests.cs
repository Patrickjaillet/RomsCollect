// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Helpers;

namespace RomsCollect.Tests.Helpers;

public class LightweightMarkupParserTests
{
    [Fact]
    public void Parse_RendersBoldSpan_AsASingleBoldSpan()
    {
        var spans = LightweightMarkupParser.Parse("**strong**");

        var span = Assert.Single(spans);
        Assert.Equal(MarkupSpanStyle.Bold, span.Style);
        Assert.Equal("strong", span.Text);
    }

    [Fact]
    public void Parse_RendersItalicSpan_AsASingleItalicSpan()
    {
        var spans = LightweightMarkupParser.Parse("*emphasis*");

        var span = Assert.Single(spans);
        Assert.Equal(MarkupSpanStyle.Italic, span.Style);
        Assert.Equal("emphasis", span.Text);
    }

    [Fact]
    public void Parse_RendersBulletLine_AsABulletSpanWithoutTheDashPrefix()
    {
        var spans = LightweightMarkupParser.Parse("- Loose");

        var span = Assert.Single(spans);
        Assert.Equal(MarkupSpanStyle.Bullet, span.Style);
        Assert.Equal("Loose", span.Text);
    }

    [Fact]
    public void Parse_LeavesPlainTextUnchanged_WhenNoMarkupIsPresent()
    {
        var spans = LightweightMarkupParser.Parse("A classic 1995 RPG.");

        var span = Assert.Single(spans);
        Assert.Equal(MarkupSpanStyle.Plain, span.Style);
        Assert.Equal("A classic 1995 RPG.", span.Text);
    }

    [Fact]
    public void Parse_TreatsAnUnterminatedMarker_AsPlainText()
    {
        var spans = LightweightMarkupParser.Parse("half *bold");

        Assert.All(spans, s => Assert.Equal(MarkupSpanStyle.Plain, s.Style));
        Assert.Equal("half *bold", string.Concat(spans.Select(s => s.Text)));
    }

    [Fact]
    public void Parse_RendersMixedBoldAndItalicAndPlainText_AsSeparateSpansInOrder()
    {
        var spans = LightweightMarkupParser.Parse("Has **bold** and *italic* parts.");

        Assert.Collection(spans,
            s => Assert.Equal((MarkupSpanStyle.Plain, "Has "), (s.Style, s.Text)),
            s => Assert.Equal((MarkupSpanStyle.Bold, "bold"), (s.Style, s.Text)),
            s => Assert.Equal((MarkupSpanStyle.Plain, " and "), (s.Style, s.Text)),
            s => Assert.Equal((MarkupSpanStyle.Italic, "italic"), (s.Style, s.Text)),
            s => Assert.Equal((MarkupSpanStyle.Plain, " parts."), (s.Style, s.Text)));
    }

    [Fact]
    public void Parse_InsertsALineBreakSpan_BetweenMultipleLines()
    {
        var spans = LightweightMarkupParser.Parse("Line one\nLine two");

        Assert.Collection(spans,
            s => Assert.Equal((MarkupSpanStyle.Plain, "Line one"), (s.Style, s.Text)),
            s => Assert.Equal(MarkupSpanStyle.LineBreak, s.Style),
            s => Assert.Equal((MarkupSpanStyle.Plain, "Line two"), (s.Style, s.Text)));
    }
}
