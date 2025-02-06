namespace Snoop.Infrastructure.Helpers;

using System;
using System.Diagnostics;

public static class Utils
{
    public static T? IgnoreErrors<T>(Func<T> action, T? defaultValue = default)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            return defaultValue;
        }
    }
}