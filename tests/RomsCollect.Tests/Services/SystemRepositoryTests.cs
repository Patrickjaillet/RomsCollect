// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

public class SystemRepositoryTests : SqliteRepositoryTestBase
{
    private readonly SystemRepository _repository;

    public SystemRepositoryTests()
    {
        _repository = new SystemRepository(ConnectionFactory);
    }

    [Fact]
    public void Add_ThenGetById_ReturnsTheSameSystem()
    {
        var id = _repository.Add(new GameSystem { Key = "snes", Name = "Super Nintendo Entertainment System", RomExtensions = ".sfc,.smc" });

        var system = _repository.GetById(id);

        Assert.NotNull(system);
        Assert.Equal("snes", system!.Key);
        Assert.Equal("Super Nintendo Entertainment System", system.Name);
        Assert.Equal(".sfc,.smc", system.RomExtensions);
    }

    [Fact]
    public void GetByKey_ReturnsMatchingSystem()
    {
        _repository.Add(new GameSystem { Key = "megadrive", Name = "Sega Genesis" });

        var system = _repository.GetByKey("megadrive");

        Assert.NotNull(system);
        Assert.Equal("Sega Genesis", system!.Name);
    }

    [Fact]
    public void GetByKey_ReturnsNull_WhenNoSystemMatches()
    {
        Assert.Null(_repository.GetByKey("does-not-exist"));
    }

    [Fact]
    public void GetAll_ReturnsSystemsOrderedBySortOrderThenName()
    {
        _repository.Add(new GameSystem { Key = "b", Name = "B System", SortOrder = 1 });
        _repository.Add(new GameSystem { Key = "a", Name = "A System", SortOrder = 0 });

        var systems = _repository.GetAll();

        Assert.Equal(["A System", "B System"], systems.Select(s => s.Name));
    }

    [Fact]
    public void Update_PersistsChanges()
    {
        var id = _repository.Add(new GameSystem { Key = "psx", Name = "PlayStation" });
        var system = _repository.GetById(id)!;
        system.Name = "Sony PlayStation";

        _repository.Update(system);

        Assert.Equal("Sony PlayStation", _repository.GetById(id)!.Name);
    }

    [Fact]
    public void Delete_RemovesTheSystem()
    {
        var id = _repository.Add(new GameSystem { Key = "nes", Name = "NES" });

        _repository.Delete(id);

        Assert.Null(_repository.GetById(id));
    }

    [Fact]
    public void Add_ThrowsSqliteException_WhenKeyAlreadyExists()
    {
        _repository.Add(new GameSystem { Key = "dup", Name = "First" });

        Assert.ThrowsAny<Exception>(() => _repository.Add(new GameSystem { Key = "dup", Name = "Second" }));
    }
}
