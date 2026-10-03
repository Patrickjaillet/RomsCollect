// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Helpers;

namespace RomsCollect.Tests.Helpers;

public class LocalizationServiceTests
{
    [Fact]
    public void Indexer_ReturnsTranslatedValue_WhenKeyExistsInEnglishCatalog()
    {
        var localization = new LocalizationService("en");

        Assert.Equal("RomsCollect", localization["App.Title"]);
    }

    [Fact]
    public void Indexer_ReturnsRawKey_WhenTranslationIsMissing()
    {
        var localization = new LocalizationService("en");

        Assert.Equal("Some.Missing.Key", localization["Some.Missing.Key"]);
    }

    [Fact]
    public void Indexer_ReturnsRawKey_WhenLanguageCatalogFileDoesNotExist()
    {
        var localization = new LocalizationService("xx-not-a-language");

        Assert.Equal("App.Title", localization["App.Title"]);
    }
}
