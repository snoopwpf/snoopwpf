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
            LogMessage($"Trying to load \"{pathToDll}\" in process \"{processWrapper.Id}\"...");

            if (File.Exists(pathToDll) == false)
            {
                throw new FileNotFoundException("Could not find file for loading in foreign process.", pathToDll);
            }

            var moduleHandleInForeignProcess = IntPtr.Zero;

            var stringForRemoteProcess = pathToDll;

            var bufLen = (stringForRemoteProcess.Length + 1) * Marshal.SizeOf(typeof(char));
            var remoteAddress = NativeMethods.VirtualAllocEx(processWrapper.Handle, IntPtr.Zero, (uint)bufLen, NativeMethods.AllocationType.Commit, NativeMethods.MemoryProtection.ReadWrite);

            if (remoteAddress == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            var address = Marshal.StringToHGlobalUni(stringForRemoteProcess);
            var size = (uint)(sizeof(char) * stringForRemoteProcess.Length);

            try
            {
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

                try
                {
                    if (remoteThread == IntPtr.Zero)
                    {
                        LogMessage("Could not create remote thread.");
                    }
                    else
                    {
                        NativeMethods.WaitForSingleObject(remoteThread);

                        // Get handle of the loaded module
                        NativeMethods.GetExitCodeThread(remoteThread, out moduleHandleInForeignProcess);
                    }
                }
                finally
                {
                    NativeMethods.CloseHandle(remoteThread);
                }

                try
                {
                    NativeMethods.VirtualFreeEx(processWrapper.Handle, remoteAddress, bufLen, NativeMethods.AllocationType.Release);
                }
                catch (Exception e)
                {
                    LogMessage(e.ToString());
                }

                if (moduleHandleInForeignProcess == IntPtr.Zero)
                {
                    throw new Exception($"Could not load \"{pathToDll}\" in process \"{processWrapper.Id}\".");
                }

                LogMessage($"Successfully loaded \"{pathToDll}\" with handle \"{moduleHandleInForeignProcess}\" in process \"{processWrapper.Id}\".");
            }
            finally
            {
                Marshal.FreeHGlobal(address);
            }

            return moduleHandleInForeignProcess;
        }

        /// <summary>
        /// Frees a library in a foreign process.
        /// </summary>
        private static bool FreeLibraryInForeignProcess(ProcessWrapper processWrapper, string moduleName)
        {
            LogMessage($"Trying to free module \"{moduleName}\" in process \"{processWrapper.Id}\"...");

            var moduleHandleInForeignProcess = NativeMethods.GetRemoteModuleHandle(processWrapper.Process, moduleName);

            if (moduleHandleInForeignProcess == IntPtr.Zero)
            {
                LogMessage($"Could not find loaded module \"{moduleName}\" in process \"{processWrapper.Id}\".");
                return false;
            }

            LogMessage($"Freeing \"{moduleHandleInForeignProcess}\" in process \"{processWrapper.Id}\"...");

            var hLibrary = NativeMethods.GetModuleHandle("kernel32");

            var procAddress = NativeMethods.GetProcAddress(hLibrary, "FreeLibraryAndExitThread");

            if (procAddress == UIntPtr.Zero)
            {
                // todo: error handling
            }

            var remoteThread = NativeMethods.CreateRemoteThread(processWrapper.Handle,
                IntPtr.Zero,
                0,
                procAddress,
                moduleHandleInForeignProcess,
                0,
                out _);
            try
            {
                if (remoteThread == IntPtr.Zero)
                {
                    LogMessage("Could not create remote thread.");
                    return false;
                }

                NativeMethods.WaitForSingleObject(remoteThread);
            }
            finally
            {
                NativeMethods.CloseHandle(remoteThread);
            }

            LogMessage($"Successfully freed \"{moduleHandleInForeignProcess}\" in process \"{processWrapper.Id}\".");

            return true;
        }

        private static void InjectSnoop(ProcessWrapper processWrapper, InjectorData injectorData)
        {
            var injectorDllName = $"Snoop.GenericInjector.{processWrapper.Bitness}.dll";
            var pathToInjectorDll = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), injectorDllName);

            LogMessage($"Trying to load \"{pathToInjectorDll}\"...");

            if (File.Exists(pathToInjectorDll) == false)
            {
                throw new FileNotFoundException("Could not find injector dll.", pathToInjectorDll);
            }

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

            var remoteAddress = NativeMethods.VirtualAllocEx(processWrapper.Handle, IntPtr.Zero, (uint)bufLen, NativeMethods.AllocationType.Commit, NativeMethods.MemoryProtection.ReadWrite);

            if (remoteAddress == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            var address = Marshal.StringToHGlobalUni(stringForRemoteProcess);
            var size = (uint)(sizeof(char) * stringForRemoteProcess.Length);

            try
            {
                var writeProcessMemoryResult = NativeMethods.WriteProcessMemory(processWrapper.Handle, remoteAddress, address, size, out var bytesWritten);

                if (writeProcessMemoryResult == false
                    || bytesWritten == 0)
                {
                    throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
                }

                IntPtr hLibrary = IntPtr.Zero;

                try
                {
                    // Load library into current process before trying to get the remote proc address
                    hLibrary = NativeMethods.LoadLibrary(pathToInjectorDll);

                    // Load library into foreign process before invoking our method
                    LoadLibraryInForeignProcess(processWrapper, pathToInjectorDll);

                    try
                    {
                        var remoteProcAddress = NativeMethods.GetRemoteProcAddress(processWrapper.Process, injectorDllName, "ExecuteInDefaultAppDomain");

                        if (remoteProcAddress == UIntPtr.Zero)
                        {
                            LogMessage("Could not get proc address for \"ExecuteInDefaultAppDomain\".");
                            return;
                        }

                        var remoteThread = NativeMethods.CreateRemoteThread(processWrapper.Handle, IntPtr.Zero, 0, remoteProcAddress, remoteAddress, 0, out _);

                        try
                        {
                            if (remoteThread == IntPtr.Zero)
                            {
                                LogMessage("Could not create remote thread.");
                                return;
                            }

                            NativeMethods.WaitForSingleObject(remoteThread);

                            // Get handle of the loaded module
                            NativeMethods.GetExitCodeThread(remoteThread, out var resultFromExecuteInDefaultAppDomain);

                            LogMessage($"Received \"{resultFromExecuteInDefaultAppDomain}\" from injector component.");

                            if (resultFromExecuteInDefaultAppDomain != IntPtr.Zero)
                            {
                                throw Marshal.GetExceptionForHR((int)resultFromExecuteInDefaultAppDomain.ToInt64());
                            }
                        }
                        finally
                        {
                            NativeMethods.CloseHandle(remoteThread);
                        }
                    }
                    finally
                    {
                        FreeLibraryInForeignProcess(processWrapper, injectorDllName);
                    }
                }
                finally
                {
                    if (hLibrary != IntPtr.Zero)
                    {
                        NativeMethods.FreeLibrary(hLibrary);
                    }

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
            finally
            {
                Marshal.FreeHGlobal(address);
            }
        }
    }
}