// ReSharper disable once CheckNamespace
namespace Snoop;

using System.Windows;

public static class FreezableExtensions
{
    public static void FreezeIfPossible(this Freezable freezable)
    {
        if (freezable.CanFreeze)
        {
            freezable.Freeze();
        }
    }
}