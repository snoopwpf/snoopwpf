using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ManagedInjector;
using System.Diagnostics;

namespace ManagedInjectorLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            var windowHandle = (IntPtr)Int64.Parse(args[0]);
            var assemblyName = args[1];
            var className = args[2];
            var methodName = args[3];

            Injector.Launch(windowHandle, assemblyName, className, methodName);
        }
    }
}
