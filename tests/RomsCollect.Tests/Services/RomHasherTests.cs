// SPDX-License-Identifier: GPL-3.0-or-later
using RomsCollect.Services.Scanning;

namespace RomsCollect.Tests.Services;

public class RomHasherTests : IDisposable
{
    private readonly string _tempDirectory;

    public RomHasherTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "RomsCollectHasherTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose() => Directory.Delete(_tempDirectory, recursive: true);

    private string WriteFile(string fileName, string content)
    {
        var path = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void ComputeHash_Crc32_MatchesKnownValue_ForKnownInput()
    {
        // "The quick brown fox jumps over the lazy dog" has a well-known CRC-32 of 0x414FA339.
        var path = WriteFile("fox.txt", "The quick brown fox jumps over the lazy dog");

        var hash = RomHasher.ComputeHash(path, RomHashAlgorithm.Crc32);

        Assert.Equal("414fa339", hash);
    }

    [Fact]
    public void ComputeHash_Crc32_IsStable_AcrossRepeatedCalls()
    {
        var path = WriteFile("stable.rom", "some rom bytes");

        var first = RomHasher.ComputeHash(path, RomHashAlgorithm.Crc32);
        var second = RomHasher.ComputeHash(path, RomHashAlgorithm.Crc32);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ComputeHash_Crc32_DiffersForDifferentContent()
    {
        var pathA = WriteFile("a.rom", "content A");
        var pathB = WriteFile("b.rom", "content B");

        Assert.NotEqual(
            RomHasher.ComputeHash(pathA, RomHashAlgorithm.Crc32),
            RomHasher.ComputeHash(pathB, RomHashAlgorithm.Crc32));
    }

    [Fact]
    public void ComputeHash_Md5_ReturnsThirtyTwoHexCharacters()
    {
        var path = WriteFile("hash-me.rom", "some rom bytes");

        var hash = RomHasher.ComputeHash(path, RomHashAlgorithm.Md5);

        Assert.Matches("^[0-9a-f]{32}$", hash);
    }

    [Fact]
    public void ComputeHash_DetectsRenamedDuplicate_ByContent()
    {
        var original = WriteFile("Original Name.rom", "identical bytes");
        var renamed = WriteFile("Renamed Copy.rom", "identical bytes");

        Assert.Equal(
            RomHasher.ComputeHash(original, RomHashAlgorithm.Crc32),
            RomHasher.ComputeHash(renamed, RomHashAlgorithm.Crc32));
    }
}
