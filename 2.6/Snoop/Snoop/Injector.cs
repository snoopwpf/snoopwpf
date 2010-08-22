// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace Snoop
{
    class Injector
    {
        static string Suffix(IntPtr windowHandle)
        {
            var window = new WindowInfo(windowHandle, null);
            string bitness = IntPtr.Size == 8 ? "64" : "32";
            string clr = "3.5";

            foreach (var module in window.Modules)
            {
                if
                (
                    module.szModule.Contains("PresentationFramework.dll") ||
                    module.szModule.Contains("PresentationFramework.ni.dll")
                )
                {
                    if (FileVersionInfo.GetVersionInfo(module.szExePath).FileMajorPart > 3)
                    {
                        clr = "4.0";
                    }
                }
                if (module.szModule.Contains("wow64.dll"))
                {
                    if (FileVersionInfo.GetVersionInfo(module.szExePath).FileMajorPart > 3)
                    {
                        bitness = "32";
                    }
                }
            }
            return bitness + "-" + clr;
        }
        internal static void Launch(IntPtr windowHandle, Assembly assembly, string className, string methodName)
        {
            var location = Assembly.GetEntryAssembly().Location;
            var directory = Path.GetDirectoryName(location);
            var file = Path.Combine(directory, "ManagedInjectorLauncher" + Suffix(windowHandle) + ".exe");

            Process.Start(file, windowHandle + " \"" + assembly.Location + "\" \"" + className + "\" \"" + methodName + "\"");
        }
    }
}
