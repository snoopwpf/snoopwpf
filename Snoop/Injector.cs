// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System.Diagnostics;
    using System.Reflection;
    using System.IO;

    public static class Injector
	{
	    private static string GetSuffix(WindowInfo windowInfo)
	    {
	        var bitness = windowInfo.IsOwningProcess64Bit ? "64" : "32";
	        var clr = "4.0";

	        return bitness + "-" + clr;
	    }

		internal static void Launch(WindowInfo windowInfo, Assembly assembly, string className, string methodName, string settingsFile)
		{
			var location = Assembly.GetEntryAssembly().Location;
			var directory = Path.GetDirectoryName(location);
			var file = Path.Combine(directory, $"ManagedInjectorLauncher{GetSuffix(windowInfo)}.exe");

		    var startInfo = new ProcessStartInfo(file, $"{windowInfo.HWnd} \"{assembly.Location}\" \"{className}\" \"{methodName}\" \"{settingsFile}\"")
		                    {
		                        Verb = windowInfo.IsOwningProcessElevated
		                                   ? "runas"
		                                   : null
		                    };           
		    
            using (var process = Process.Start(startInfo))
            {
                process?.WaitForExit();
            }

            File.Delete(settingsFile);
		}
	}
}