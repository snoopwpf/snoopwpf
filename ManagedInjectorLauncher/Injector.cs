namespace ManagedInjectorLauncher
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml.Serialization;
    using Snoop;

    /// <summary>
    /// Class responsible for injecting snoop into a foreign process.
    /// </summary>
    public static class Injector
    {
        public static void LogMessage(string message, bool append)
        {
            var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            applicationDataPath += "\\Snoop";

            if (!Directory.Exists(applicationDataPath))
            {
                Directory.CreateDirectory(applicationDataPath);
            }

            var pathname = Path.Combine(applicationDataPath, "SnoopLog.txt");

            if (!append)
            {
                File.Delete(pathname);
            }

            var logMessage = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " : " + message;

            Trace.WriteLine(logMessage);

            var fi = new FileInfo(pathname);

            using (var sw = fi.AppendText())
            {
                sw.WriteLine(logMessage);
            }
        }

        public static void InjectIntoProcess(IntPtr windowHandle, InjectorData injectorData)
        {
            string transportDataString;

            {
                var serializer = new XmlSerializer(typeof(InjectorData));

                using (var stream = new StringWriter())
                {
                    serializer.Serialize(stream, injectorData);
                    transportDataString = stream.ToString();
                }
            }

            NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);

            using (var process = Process.GetProcessById(processId))
            {
                var processWrapper = new ProcessWrapper(process);

                if (processWrapper.SupportedFrameworkName == "netcoreapp3.0")
                {
                    InjectIJWHost(processWrapper);
                }

                InjectSnoop(processWrapper, transportDataString);
            }
        }

        private static void InjectIJWHost(ProcessWrapper processWrapper)
        {
            var ijwHostPath = GetPathToIJWHost(processWrapper);

            // We also have to inject the ijwhost.dll into this process, otherwise a later call to LoadLibrary on that DLL will fail
            {
                var hLibrary = NativeMethods.LoadLibrary(ijwHostPath);

                if (hLibrary == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }
            }

            LoadLibraryInForeignProcess(processWrapper, ijwHostPath);
        }

        /// <summary>
        /// Loads a library into a foreign process and returns the module handle of the loaded library.
        /// </summary>
        private static IntPtr LoadLibraryInForeignProcess(ProcessWrapper processWrapper, string pathToDll)
        {
            Trace.WriteLine($"Trying to load '{pathToDll}' in process ");

            var moduleHandleInForeignProcess = IntPtr.Zero;

            var stringForRemoteProcess = pathToDll;

            var bufLen = (stringForRemoteProcess.Length + 1) * Marshal.SizeOf(typeof(char));
            var remoteAddress = NativeMethods.VirtualAllocEx(processWrapper.Handle, IntPtr.Zero, (uint)bufLen, NativeMethods.AllocationType.Commit, NativeMethods.MemoryProtection.ReadWrite);

            if (remoteAddress != IntPtr.Zero)
            {
                var address = Marshal.StringToHGlobalUni(stringForRemoteProcess);
                var size = (uint)(sizeof(char) * stringForRemoteProcess.Length);

                var writeProcessMemoryResult = NativeMethods.WriteProcessMemory(processWrapper.Handle, remoteAddress, address, size, out var bytesWritten);

                if (writeProcessMemoryResult == false
                    || bytesWritten == 0)
                {
                    throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
                }

                var hLibrary = NativeMethods.GetModuleHandle("kernel32");

                // Load dll into the remote process
                // (via CreateRemoteThread & LoadLibrary)
                var procAddress = NativeMethods.GetProcAddress(hLibrary, "LoadLibraryW");

                if (procAddress == UIntPtr.Zero)
                {
                    // todo: error handling
                }

                var remoteThread = NativeMethods.CreateRemoteThread(processWrapper.Handle,
                                                                    IntPtr.Zero, 
                                                                    0, 
                                                                    procAddress, 
                                                                    remoteAddress, 
                                                                    0, 
                                                                    out _);

                // todo: error handling
                if (remoteThread != IntPtr.Zero)
                {
                    NativeMethods.WaitForSingleObject(remoteThread);

                    // Get handle of the loaded module
                    NativeMethods.GetExitCodeThread(remoteThread, out moduleHandleInForeignProcess);
                }

                NativeMethods.CloseHandle(remoteThread);

                try
                {
                    NativeMethods.VirtualFreeEx(processWrapper.Handle, remoteAddress, bufLen, NativeMethods.AllocationType.Release);
                }
                catch (Exception e)
                {
                    LogMessage(e.ToString(), true);
                }
            }
            else
            {
                throw new Win32Exception();
            }

            if (moduleHandleInForeignProcess == IntPtr.Zero)
            {
                throw new Exception($"Could not load '{pathToDll}' in process '{processWrapper.Id}'.");
            }

            return moduleHandleInForeignProcess;
        }

        private static string GetPathToIJWHost(ProcessWrapper processWrapper)
        {
            const string ijwhostDllFilename = "ijwhost.dll";
            const string hostfxrDllFilename = "hostfxr.dll";

            Trace.WriteLine($"Trying to find path to '{ijwhostDllFilename}'...");

            //var samplePath = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Host.win-x64\3.0.0-preview6-27804-01\runtimes\win-x64\native\ijwhost.dll";
            //var hostfxrPath = @"C:\Program Files\dotnet\host\fxr\3.0.0-preview6-27804-01\hostfxr.dll";
            string hostfxrPath = null;

            Trace.WriteLine($"Trying to find loaded module '{hostfxrDllFilename}'...");

            var modules = NativeMethods.GetModules(processWrapper.Process);

            foreach (var module in modules)
            {
                if (module.szModule.Equals(hostfxrDllFilename, StringComparison.OrdinalIgnoreCase))
                {
                    hostfxrPath = module.szExePath;
                    Trace.WriteLine($"Found path to '{hostfxrDllFilename}' with value '{hostfxrPath}'.");
                    break;
                }
            }

            if (string.IsNullOrEmpty(hostfxrPath))
            {
                throw new FileNotFoundException($"Could not find path to '{hostfxrDllFilename}'.", hostfxrDllFilename);
            }

            var hostfxrDirectoryPath = Path.GetDirectoryName(hostfxrPath);

            if (string.IsNullOrEmpty(hostfxrDirectoryPath) == false)
            {
                var dotnetPath = Path.GetFullPath(Path.Combine(hostfxrDirectoryPath, @"..\..\.."));
                var frameworkVersion = new DirectoryInfo(hostfxrDirectoryPath).Name;

                var ijwHostDllPath = $@"{dotnetPath}\packs\Microsoft.NETCore.App.Host.win-{processWrapper.Bitness}\{frameworkVersion}\runtimes\win-{processWrapper.Bitness}\native\{ijwhostDllFilename}";

                Trace.WriteLine($"Path to '{ijwhostDllFilename}' might be '{ijwHostDllPath}'.");

                if (File.Exists(ijwHostDllPath))
                {
                    Trace.WriteLine($"Path to '{ijwhostDllFilename}' is '{ijwHostDllPath}'.");
                    return ijwHostDllPath;
                }
            }

            throw new FileNotFoundException($"Could not find path to '{ijwhostDllFilename}'.", ijwhostDllFilename);
        }

        private static void InjectSnoop(ProcessWrapper processWrapper, string transportDataString)
        {
            var pathToHookDll = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"ManagedInjector.{processWrapper.SupportedFrameworkName}.{processWrapper.Bitness}.dll");

            var hLibrary = NativeMethods.LoadLibrary(pathToHookDll);

            if (hLibrary == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            var hLibraryInForeignProcess = LoadLibraryInForeignProcess(processWrapper, pathToHookDll);

            if (hLibrary != hLibraryInForeignProcess)
            {
                throw new Exception($"Different module handle/offset. Local = {hLibrary}, Remote = {hLibraryInForeignProcess}.\r\nThis case is currently not handled.\r\nPlease create an issue on github including your sample application.");
            }

            var stringForRemoteProcess = transportDataString;

            var bufLen = (stringForRemoteProcess.Length + 1) * Marshal.SizeOf(typeof(char));
            var remoteAddress = NativeMethods.VirtualAllocEx(processWrapper.Handle, IntPtr.Zero, (uint)bufLen,
                                                               NativeMethods.AllocationType.Commit,
                                                               NativeMethods.MemoryProtection.ReadWrite);

            if (remoteAddress != IntPtr.Zero)
            {
                var address = Marshal.StringToHGlobalUni(stringForRemoteProcess);
                var size = (uint)(sizeof(char) * stringForRemoteProcess.Length);

                var writeProcessMemoryResult = NativeMethods.WriteProcessMemory(processWrapper.Handle, remoteAddress, address, size, out var bytesWritten);

                if (writeProcessMemoryResult == false
                    || bytesWritten == 0)
                {
                    throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
                }

                // Load dll into the remote process
                // (via CreateRemoteThread & LoadLibrary)
                var procAddress = NativeMethods.GetProcAddress(hLibrary, "LoadSnoop");

                if (procAddress == UIntPtr.Zero)
                {
                    // todo: error handling
                }

                var remoteThread = NativeMethods.CreateRemoteThread(processWrapper.Handle,
                                                               IntPtr.Zero,
                                                               0,
                                                               procAddress,
                                                               remoteAddress,
                                                               0,
                                                               out _);

                // todo: error handling
                if (remoteThread != IntPtr.Zero)
                {
                    NativeMethods.WaitForSingleObject(remoteThread);
                }

                NativeMethods.CloseHandle(remoteThread);

                try
                {
                    NativeMethods.VirtualFreeEx(processWrapper.Handle, remoteAddress, bufLen, NativeMethods.AllocationType.Release);
                }
                catch (Exception e)
                {
                    LogMessage(e.ToString(), true);
                }
            }
            else
            {
                throw new Win32Exception();
            }

            NativeMethods.FreeLibrary(hLibrary);
        }

        private class ProcessWrapper
        {
            public ProcessWrapper(Process process)
            {
                this.Process = process;
                this.Id = process.Id;
                this.Handle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, process.Id);

                this.Bitness = NativeMethods.IsProcess64Bit(this.Process)
                                   ? "x64"
                                   : "x86";

                this.SupportedFrameworkName = GetTargetFramework(process);
            }

            public Process Process { get; }

            public int Id { get; }

            public NativeMethods.ProcessHandle Handle { get; }

            public string Bitness { get; }

            public string SupportedFrameworkName { get; }

            private static string GetTargetFramework(Process process)
            {
                var modules = NativeMethods.GetModules(process);

                foreach (var module in modules)
                {
                    if (module.szModule.IndexOf("wpfgfx_cor3", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return "netcoreapp3.0";
                    }
                }

                return "net40";
            }
        }
    }
}