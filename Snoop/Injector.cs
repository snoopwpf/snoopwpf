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
	        var bitness = windowInfo.IsOwningProcess64Bit ? "x64" : "x86";
	        var clr = "4.0";

            return bitness;  // + "-" + clr;
        }

		internal static void Launch(WindowInfo windowInfo, Assembly assembly, string className, string methodName, string settingsFile)
		{
            if (File.Exists(settingsFile) == false)
            {
                throw new FileNotFoundException("The generated temporary settings file could not be found.", settingsFile);
            }
            
			var location = Assembly.GetEntryAssembly().Location;
			var directory = Path.GetDirectoryName(location);
			var file = Path.Combine(directory, $"ManagedInjectorLauncher.{GetSuffix(windowInfo)}.exe");

            try
            {
                var location = Assembly.GetExecutingAssembly().Location;
                var directory = Path.GetDirectoryName(location) ?? string.Empty;
                var file = Path.Combine(directory, $"ManagedInjectorLauncher{GetSuffix(windowInfo)}.exe");

                if (File.Exists(file) == false)
                {
                    const string message = @"Could not find the injector launcher.
Snoop requires this component, which is part of the snoop project, to do it's job.
If you compiled snoop you should compile all required components.
If you downloaded snoop you should not omit any files contained in the archive you downloaded.";
                    throw new FileNotFoundException(message, file);
                }

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
            }
            finally
            {
                File.Delete(settingsFile);
            }
        }
	}
}