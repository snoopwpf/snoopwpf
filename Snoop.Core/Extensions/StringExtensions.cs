// ReSharper disable once CheckNamespace
namespace Snoop;

using System;

public static class StringExtensions
{
    public static bool Contains(this string source, string? value, StringComparison comparisonType)
    {
        if (value is null
            || value.Length == 0)
        {
            return true;
        }

        return source.IndexOf(value, comparisonType) >= 0;
    }

    public static bool ContainsIgnoreCase(this string source, string? value)
    {
        if (value is null
            || value.Length == 0)
        {
            return true;
        }

        return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}