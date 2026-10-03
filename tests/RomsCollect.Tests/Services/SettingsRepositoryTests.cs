// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

public class SettingsRepositoryTests : SqliteRepositoryTestBase
{
    private readonly SettingsRepository _settings;

    public SettingsRepositoryTests()
    {
        _settings = new SettingsRepository(ConnectionFactory);
    }

    [Fact]
    public void Get_ReturnsNull_WhenKeyDoesNotExist()
    {
        Assert.Null(_settings.Get("language"));
    }

    [Fact]
    public void Set_ThenGet_ReturnsTheStoredValue()
    {
        _settings.Set("language", "en");

        Assert.Equal("en", _settings.Get("language"));
    }

    [Fact]
    public void Set_OverwritesExistingValue_ForTheSameKey()
    {
        _settings.Set("theme", "dark");
        _settings.Set("theme", "light");

        Assert.Equal("light", _settings.Get("theme"));
    }

    [Fact]
    public void GetAll_ReturnsEverySetting()
    {
        _settings.Set("language", "en");
        _settings.Set("theme", "dark");

        var all = _settings.GetAll();

        Assert.Equal(2, all.Count);
        Assert.Equal("en", all["language"]);
        Assert.Equal("dark", all["theme"]);
    }

    [Fact]
    public void Delete_RemovesTheSetting()
    {
        _settings.Set("temporary", "value");

        _settings.Delete("temporary");

        Assert.Null(_settings.Get("temporary"));
    }
}
