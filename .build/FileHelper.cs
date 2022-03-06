using System;
using System.IO;
using System.Security.Cryptography;

static class FileHelper
{
    public static string SHA256CheckSum(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = File.OpenRead(filePath);
        var checksum = sha256.ComputeHash(fileStream);
        return BitConverter.ToString(checksum)
            .Replace("-", string.Empty);
    }
}