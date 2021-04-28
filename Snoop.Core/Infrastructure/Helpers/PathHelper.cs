namespace Snoop.Infrastructure.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public static class PathHelper
    {
        public static bool TryFindPathOnPath(string fileName, [NotNullWhen(true)] out string? foundFullPath)
        {
            foundFullPath = FindPathOnPath(fileName);
            return string.IsNullOrEmpty(foundFullPath) == false;
        }

        public static string? FindPathOnPath(string fileName)
        {
            if (File.Exists(fileName))
            {
                return Path.GetFullPath(fileName);
            }

            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

            foreach (var path in pathEnv.Split(Path.PathSeparator))
            {
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }
    }
}