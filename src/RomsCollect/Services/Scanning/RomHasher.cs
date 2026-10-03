// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Security.Cryptography;

namespace RomsCollect.Services.Scanning;

/// <summary>The hash algorithm used for ROM duplicate detection.</summary>
public enum RomHashAlgorithm
{
    Crc32,
    Md5,
}

/// <summary>
/// Computes a content hash for a ROM file so renamed copies of the same ROM
/// can be detected as duplicates during a scan.
/// </summary>
public static class RomHasher
{
    public static string ComputeHash(string filePath, RomHashAlgorithm algorithm)
    {
        using var stream = File.OpenRead(filePath);
        return algorithm switch
        {
            RomHashAlgorithm.Crc32 => ComputeCrc32(stream),
            RomHashAlgorithm.Md5 => Convert.ToHexString(MD5.HashData(stream)).ToLowerInvariant(),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
        };
    }

    private static string ComputeCrc32(Stream stream)
    {
        uint crc = 0xFFFFFFFF;
        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var i = 0; i < bytesRead; i++)
            {
                crc ^= buffer[i];
                for (var bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
                }
            }
        }

        return (~crc).ToString("x8");
    }
}
