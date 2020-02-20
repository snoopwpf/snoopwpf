using System;
using System.IO;
using System.Security.Cryptography;

static class FileHelper
{
    public static string SHA256CheckSum(string filePath)
    {
        using (var SHA256 = SHA256Managed.Create())
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                var checksum = SHA256.ComputeHash(fileStream);
                return BitConverter.ToString(checksum)
                                   .Replace("-", String.Empty);
            }
        }
    }
}