namespace Snoop.Infrastructure.Helpers;

using System.Windows;

public static class ResourceKeyHelper
{
    public static bool IsValidResourceKey(object? key)
    {
        return key is not null
               && ReferenceEquals(key, DependencyProperty.UnsetValue) == false;
    }
}