namespace Snoop.Infrastructure.Helpers;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

public static class PathHelper
{
    public static bool TryFindPathOnPath(string fileName, [NotNullWhen(true)] out string? foundFullPath)
    {
        return TryFindPathOnPath(Environment.GetEnvironmentVariable("PATH")!, fileName, out foundFullPath);
    }

    public static bool TryFindPathOnPath(string? path, string fileName, [NotNullWhen(true)] out string? foundFullPath)
    {
        foundFullPath = FindPathOnPath(path, fileName);
        return string.IsNullOrEmpty(foundFullPath) == false;
    }

    public static string? FindPathOnPath(string fileName)
    {
        return FindPathOnPath(Environment.GetEnvironmentVariable("PATH")!, fileName);
    }

    public static string? FindPathOnPath(string? path, string fileName)
    {
        if (File.Exists(fileName))
        {
            return Path.GetFullPath(fileName);
        }

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        path = Environment.ExpandEnvironmentVariables(path);

        if (File.Exists(path))
        {
            return Path.GetFullPath(path);
        }

        foreach (var pathPart in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrEmpty(pathPart))
            {
                continue;
            }

            var fullPath = Path.Combine(pathPart, fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}