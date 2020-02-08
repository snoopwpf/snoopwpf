namespace Snoop.InjectorLauncher
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using JetBrains.Annotations;
    using Snoop.Infrastructure;

    /// <summary>
    /// Class responsible for injecting snoop into a foreign process.
    /// </summary>
    public static class Injector
    {
        public static void LogMessage(string message, bool append = true)
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

            var logMessage = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + ": " + message;

            Trace.WriteLine(logMessage);
            Console.WriteLine(logMessage);

            var fi = new FileInfo(pathname);

            using (var sw = fi.AppendText())
            {
                sw.WriteLine(logMessage);
            }
        }

        [PublicAPI]
        public static void InjectIntoProcess(IntPtr windowHandle, InjectorData injectorData)
        {
            InjectIntoProcess(ProcessWrapper.FromWindowHandle(windowHandle), injectorData);
        }

        [PublicAPI]
        public static void InjectIntoProcess(ProcessWrapper processWrapper, InjectorData injectorData)
        {
            InjectSnoop(processWrapper, injectorData);
        }

        /// <summary>
        /// Loads a library into a foreign process and returns the module handle of the loaded library.
        /// </summary>
        private static IntPtr LoadLibraryInForeignProcess(ProcessWrapper processWrapper, string pathToDll)
        {
            LogMessage($"Trying to load '{pathToDll}' in process {processWrapper.Id}...");

            if (File.Exists(pathToDll) == false)
            {
                throw new FileNotFoundException("Could not find file for loading in foreign process.", pathToDll);
            }

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
                    LogMessage(e.ToString());
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

            LogMessage($"Successfully loaded '{pathToDll}' with handle {moduleHandleInForeignProcess} in process {processWrapper.Id}.");

            return moduleHandleInForeignProcess;
        }

        private static void InjectSnoop(ProcessWrapper processWrapper, InjectorData injectorData)
        {
            var injectorDllName = $"Snoop.GenericInjector.{processWrapper.Bitness}.dll";
            var pathToInjectorDll = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), injectorDllName);

            LogMessage($"Trying to load '{pathToInjectorDll}'...");

            if (File.Exists(pathToInjectorDll) == false)
            {
                throw new FileNotFoundException("Could not find injector dll.", pathToInjectorDll);
            }

            var hLibrary = NativeMethods.LoadLibrary(pathToInjectorDll);

            if (hLibrary == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            LogMessage($"Successfully loaded '{pathToInjectorDll}' with handle {hLibrary}.");

            var hLibraryInForeignProcess = LoadLibraryInForeignProcess(processWrapper, pathToInjectorDll);

            var parameters = new[]
                             {
                                 processWrapper.SupportedFrameworkName,
                                 injectorData.FullAssemblyPath,
                                 injectorData.ClassName,
                                 injectorData.MethodName,
                                 injectorData.SettingsFile
                             };

            var stringForRemoteProcess = string.Join("<|>", parameters);

            var bufLen = (stringForRemoteProcess.Length + 1) * Marshal.SizeOf(typeof(char));

            try
            {
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

                    try
                    {
                        // Load dll into the remote process
                        // (via CreateRemoteThread & LoadLibrary)
                        var procAddress = hLibrary == hLibraryInForeignProcess
                                              ? NativeMethods.GetProcAddress(hLibrary, "ExecuteInDefaultAppDomain")
                                              : NativeMethods.GetRemoteProcAddress(processWrapper.Process, injectorDllName, "ExecuteInDefaultAppDomain");

                        if (procAddress != UIntPtr.Zero)
                        {
                            var remoteThread = NativeMethods.CreateRemoteThread(processWrapper.Handle, IntPtr.Zero, 0, procAddress, remoteAddress, 0, out _);

                            try
                            {
                                if (remoteThread != IntPtr.Zero)
                                {
                                    NativeMethods.WaitForSingleObject(remoteThread);

                                    // Get handle of the loaded module
                                    NativeMethods.GetExitCodeThread(remoteThread, out var resultFromExecuteInDefaultAppDomain);

                                    LogMessage($"Received \"{resultFromExecuteInDefaultAppDomain}\" from injector component.");

                                    if (resultFromExecuteInDefaultAppDomain != IntPtr.Zero)
                                    {
                                        throw Marshal.GetExceptionForHR((int)resultFromExecuteInDefaultAppDomain.ToInt64());
                                    }
                                }
                            }
                            finally
                            {
                                NativeMethods.CloseHandle(remoteThread);
                            }
                        }
                        else
                        {
                            LogMessage("Could not get proc address for \"ExecuteInDefaultAppDomain\".");
                        }
                    }
                    finally
                    {
                        try
                        {
                            NativeMethods.VirtualFreeEx(processWrapper.Handle, remoteAddress, bufLen, NativeMethods.AllocationType.Release);
                        }
                        catch (Exception e)
                        {
                            LogMessage(e.ToString());
                        }
                    }
                }
                else
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                NativeMethods.FreeLibrary(hLibrary);
            }
        }
    }
}