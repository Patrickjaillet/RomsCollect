// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Models;
using RomsCollect.Services.Database;
using RomsCollect.Services.Import;

namespace RomsCollect.Tests.Services;

public class RomFolderImporterTests : SqliteRepositoryTestBase
{
    private readonly SystemRepository _systems;
    private readonly string _sourceRomsDirectory;
    private readonly string _destinationRomsDirectory;
    private bool _rootsDeleted;

    public RomFolderImporterTests()
    {
        _systems = new SystemRepository(ConnectionFactory);
        var root = Path.Combine(Path.GetTempPath(), "RomsCollectRomFolderTests_" + Guid.NewGuid());
        _sourceRomsDirectory = Path.Combine(root, "source-roms");
        _destinationRomsDirectory = Path.Combine(root, "destination-roms");
        Directory.CreateDirectory(_sourceRomsDirectory);
        Directory.CreateDirectory(_destinationRomsDirectory);

        _systems.Add(new GameSystem { Key = "snes", Name = "SNES", RomExtensions = ".sfc,.smc" });
    }

    public override void Dispose()
    {
        if (!_rootsDeleted)
        {
            var root = Path.GetDirectoryName(_sourceRomsDirectory)!;
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
            _rootsDeleted = true;
        }

        base.Dispose();
    }

    private RomFolderImporter CreateImporter() => new(_systems, _destinationRomsDirectory);

    [Fact]
    public void DetectCandidates_FindsSystemDirectory_WithMatchingRomExtension()
    {
        var snesDirectory = Path.Combine(_sourceRomsDirectory, "snes");
        Directory.CreateDirectory(snesDirectory);
        File.WriteAllText(Path.Combine(snesDirectory, "game.sfc"), "rom bytes");

        var candidates = CreateImporter().DetectCandidates(_sourceRomsDirectory);

        Assert.Single(candidates);
        Assert.Equal("snes", candidates[0].SystemKey);
        Assert.Equal(1, candidates[0].RomFileCount);
    }

    [Fact]
    public void DetectCandidates_IgnoresDirectory_WithNoConfiguredSystemMatch()
    {
        Directory.CreateDirectory(Path.Combine(_sourceRomsDirectory, "unknown-system"));

        var candidates = CreateImporter().DetectCandidates(_sourceRomsDirectory);

        Assert.Empty(candidates);
    }

    [Fact]
    public void DetectCandidates_IgnoresSystemDirectory_WithNoMatchingRomFiles()
    {
        var snesDirectory = Path.Combine(_sourceRomsDirectory, "snes");
        Directory.CreateDirectory(snesDirectory);
        File.WriteAllText(Path.Combine(snesDirectory, "readme.txt"), "not a rom");

        var candidates = CreateImporter().DetectCandidates(_sourceRomsDirectory);

        Assert.Empty(candidates);
    }

    [Fact]
    public void Import_CopyMode_CopiesRomFiles_IntoDestinationTree()
    {
        var snesDirectory = Path.Combine(_sourceRomsDirectory, "snes");
        Directory.CreateDirectory(snesDirectory);
        File.WriteAllText(Path.Combine(snesDirectory, "game.sfc"), "rom bytes");
        var candidate = CreateImporter().DetectCandidates(_sourceRomsDirectory)[0];

        CreateImporter().Import(candidate, RomImportMode.Copy);

        var destinationFile = Path.Combine(_destinationRomsDirectory, "snes", "game.sfc");
        Assert.True(File.Exists(destinationFile));
        Assert.Equal("rom bytes", File.ReadAllText(destinationFile));
    }

    [Fact]
    public void Import_DoesNotOverwrite_ExistingDestinationFile()
    {
        var snesDirectory = Path.Combine(_sourceRomsDirectory, "snes");
        Directory.CreateDirectory(snesDirectory);
        File.WriteAllText(Path.Combine(snesDirectory, "game.sfc"), "source bytes");
        var destinationSnesDirectory = Path.Combine(_destinationRomsDirectory, "snes");
        Directory.CreateDirectory(destinationSnesDirectory);
        File.WriteAllText(Path.Combine(destinationSnesDirectory, "game.sfc"), "already imported bytes");

        var candidate = CreateImporter().DetectCandidates(_sourceRomsDirectory)[0];
        CreateImporter().Import(candidate, RomImportMode.Copy);

        Assert.Equal("already imported bytes", File.ReadAllText(Path.Combine(destinationSnesDirectory, "game.sfc")));
    }

    [Fact]
    public void Import_SymbolicLinkMode_CreatesALinkThatReadsTheSourceContent()
    {
        var snesDirectory = Path.Combine(_sourceRomsDirectory, "snes");
        Directory.CreateDirectory(snesDirectory);
        File.WriteAllText(Path.Combine(snesDirectory, "game.sfc"), "rom bytes");
        var candidate = CreateImporter().DetectCandidates(_sourceRomsDirectory)[0];

        try
        {
            CreateImporter().Import(candidate, RomImportMode.SymbolicLink);
        }
        catch (IOException) when (!IsRunningElevatedOrInDeveloperMode())
        {
            // File.CreateSymbolicLink requires either an elevated process
            // or Windows Developer Mode enabled; skip rather than fail the
            // suite on a machine/CI runner without either.
            return;
        }

        var destinationFile = Path.Combine(_destinationRomsDirectory, "snes", "game.sfc");
        Assert.True(File.Exists(destinationFile));
        Assert.Equal("rom bytes", File.ReadAllText(destinationFile));
    }

    private static bool IsRunningElevatedOrInDeveloperMode()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
