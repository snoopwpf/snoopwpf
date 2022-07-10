// ReSharper disable once CheckNamespace
namespace Snoop;

using System;

public static class ConsoleProgram
{
    [STAThread]
    public static int Main(string[] args)
    {
        return Program.Main(args);
    }
}