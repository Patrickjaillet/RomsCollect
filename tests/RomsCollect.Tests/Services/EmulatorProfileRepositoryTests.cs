// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;

namespace RomsCollect.Tests.Services;

public class EmulatorProfileRepositoryTests : SqliteRepositoryTestBase
{
    private readonly EmulatorProfileRepository _profiles;
    private readonly int _systemId;

    public EmulatorProfileRepositoryTests()
    {
        var systems = new SystemRepository(ConnectionFactory);
        _systemId = systems.Add(new GameSystem { Key = "psx", Name = "PlayStation" });
        _profiles = new EmulatorProfileRepository(ConnectionFactory);
    }

    [Fact]
    public void Add_ThenGetById_ReturnsTheSameProfile()
    {
        var id = _profiles.Add(new EmulatorProfile
        {
            SystemId = _systemId,
            Name = "DuckStation",
            ExecutablePath = @"C:\Emulators\DuckStation\duckstation-qt-x64-ReleaseLTCG.exe",
            CommandLineTemplate = "\"{RomPath}\"",
            IsDefault = true,
        });

        var profile = _profiles.GetById(id);

        Assert.NotNull(profile);
        Assert.Equal("DuckStation", profile!.Name);
        Assert.True(profile.IsDefault);
    }

    [Fact]
    public void GetDefaultForSystem_ReturnsOnlyTheDefaultProfile()
    {
        _profiles.Add(new EmulatorProfile { SystemId = _systemId, Name = "Emu A", ExecutablePath = "a.exe", IsDefault = false });
        _profiles.Add(new EmulatorProfile { SystemId = _systemId, Name = "Emu B", ExecutablePath = "b.exe", IsDefault = true });

        var defaultProfile = _profiles.GetDefaultForSystem(_systemId);

        Assert.NotNull(defaultProfile);
        Assert.Equal("Emu B", defaultProfile!.Name);
    }

    [Fact]
    public void GetBySystem_ReturnsAllProfilesForThatSystem()
    {
        _profiles.Add(new EmulatorProfile { SystemId = _systemId, Name = "Emu A", ExecutablePath = "a.exe" });
        _profiles.Add(new EmulatorProfile { SystemId = _systemId, Name = "Emu B", ExecutablePath = "b.exe" });

        var profiles = _profiles.GetBySystem(_systemId);

        Assert.Equal(2, profiles.Count);
    }

    [Fact]
    public void Update_PersistsChanges()
    {
        var id = _profiles.Add(new EmulatorProfile { SystemId = _systemId, Name = "Old name", ExecutablePath = "old.exe" });
        var profile = _profiles.GetById(id)!;
        profile.Name = "New name";
        profile.ExecutablePath = "new.exe";

        _profiles.Update(profile);

        var updated = _profiles.GetById(id)!;
        Assert.Equal("New name", updated.Name);
        Assert.Equal("new.exe", updated.ExecutablePath);
    }

    [Fact]
    public void Delete_RemovesTheProfile()
    {
        var id = _profiles.Add(new EmulatorProfile { SystemId = _systemId, Name = "Temp", ExecutablePath = "temp.exe" });

        _profiles.Delete(id);

        Assert.Null(_profiles.GetById(id));
    }
}
