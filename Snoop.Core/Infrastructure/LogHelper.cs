namespace Snoop.Infrastructure;

using System;
using System.Diagnostics;

public static class LogHelper
{
    public static string WriteLine(Exception exception)
    {
        return WriteLine(exception.ToString());
    }

    public static string WriteLine(string message)
    {
        Trace.WriteLine(message);
        Console.WriteLine(message);

        return message;
    }

    public static string WriteWarning(Exception exception)
    {
        return WriteWarning(exception.ToString());
    }

    public static string WriteWarning(string message)
    {
        Trace.TraceWarning(message);
        Console.WriteLine(message);

        return message;
    }

    public static string WriteError(Exception exception)
    {
        return WriteError(exception.ToString());
    }

    public static string WriteError(string message)
    {
        Trace.TraceError(message);
        Console.WriteLine(message);

        return message;
    }
}