namespace ManagedInjectorLauncher
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Xml.Serialization;
    using Snoop;

    /// <summary>
    /// Class responsible for injecting snoop into a foreign process.
    /// </summary>
    public static class Injector
    {
        private static readonly uint messageId;

        static Injector()
        {
            messageId = NativeMethods.RegisterWindowMessage("Injector_GOBABYGO!");
        }
        
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
            var transportDataString = string.Empty;

            {
                var serializer = new XmlSerializer(typeof(InjectorData));

                using (var stream = new StringWriter())
                {
                    serializer.Serialize(stream, injectorData);
                    transportDataString = stream.ToString();
                }
            }

            var threadId = (uint)NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);

            using (var process = Process.GetProcessById(processId))
            {
                //Debugger.Launch();

                var framework = GetTargetFramework(process);

                var bitness = Environment.Is64BitProcess
                                  ? "x64"
                                  : "x86";

                var hProcess = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, processId);

                if (framework == "netcoreapp3.0")
                {
                    InjectIJWHost(hProcess, bitness);
                }

                InjectSnoop(windowHandle, framework, bitness, transportDataString, hProcess, threadId);
            }
        }

        private static void InjectIJWHost(NativeMethods.ProcessHandle hProcess, string bitness)
        {
            var ijwHostPath = GetPathToIJWHost(hProcess, bitness);

            var bufLen = (ijwHostPath.Length + 1) * Marshal.SizeOf(typeof(char));
            var remoteAddress = NativeMethods.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)bufLen,
                                                               NativeMethods.AllocationType.Commit,
                                                               NativeMethods.MemoryProtection.ReadWrite);

            if (remoteAddress != IntPtr.Zero)
            {
                var address = Marshal.StringToHGlobalUni(ijwHostPath);
                var size = (uint)(sizeof(char) * ijwHostPath.Length);

                NativeMethods.WriteProcessMemory(hProcess, remoteAddress, address, size, out var bytesWritten);

                if (bytesWritten == 0)
                {
                    throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
                }

                var hKernel32 = NativeMethods.GetModuleHandle("kernel32");

                // Load "LibSpy.dll" into the remote process
                // (via CreateRemoteThread & LoadLibrary)
                IntPtr remoteThreadId;
                var remoteThread = NativeMethods.CreateRemoteThread(hProcess.DangerousGetHandle(), 
                                                               IntPtr.Zero, 
                                                               0, 
                                                               NativeMethods.GetProcAddress(hKernel32, "LoadLibraryW"),
                                                               remoteAddress, 
                                                               0, 
                                                               out remoteThreadId);

                if (remoteThread != IntPtr.Zero)
                {
                    NativeMethods.WaitForSingleObject(remoteThread);
                }

                NativeMethods.CloseHandle(remoteThread);

                try
                {
                    NativeMethods.VirtualFreeEx(hProcess, remoteAddress, bufLen, NativeMethods.AllocationType.Release);
                }
                catch (Exception e)
                {
                    LogMessage(e.ToString(), true);
                }
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static string GetPathToIJWHost(NativeMethods.ProcessHandle hProcess, string bitness)
        {
            const string ijwhostDllFilename = "ijwhost.dll";
            const string hostfxrDllFilename = "hostfxr.dll";

            Trace.WriteLine($"Trying to find path to '{ijwhostDllFilename}'...");

            //var samplePath = @"C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Host.win-x64\3.0.0-preview6-27804-01\runtimes\win-x64\native\ijwhost.dll";
            //var hostfxrPath = @"C:\Program Files\dotnet\host\fxr\3.0.0-preview6-27804-01\hostfxr.dll";
            string hostfxrPath = null;

            Trace.WriteLine($"Trying to find loaded module '{hostfxrDllFilename}'...");

            Debugger.Launch();

            var modules = NativeMethods.GetModulesFromProcessHandle(hProcess.DangerousGetHandle());

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

                var ijwHostDllPath = $@"{dotnetPath}\packs\Microsoft.NETCore.App.Host.win-{bitness}\{frameworkVersion}\runtimes\win-{bitness}\native\{ijwhostDllFilename}";

                Trace.WriteLine($"Path to '{ijwhostDllFilename}' might be '{ijwHostDllPath}'.");

                if (File.Exists(ijwHostDllPath))
                {
                    Trace.WriteLine($"Path to '{ijwhostDllFilename}' is '{ijwHostDllPath}'.");
                    return ijwHostDllPath;
                }
            }

            throw new FileNotFoundException($"Could not find path to '{ijwhostDllFilename}'.", ijwhostDllFilename);
        }

        private static void InjectSnoop(IntPtr windowHandle, string framework, string bitness, string transportDataString, NativeMethods.ProcessHandle hProcess, uint threadId)
        {
            var hookName = $"ManagedInjector.{framework}.{bitness}.dll";

            var hInstance = NativeMethods.LoadLibrary(hookName);

            if (hInstance == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var bufLen = (transportDataString.Length + 1) * Marshal.SizeOf(typeof(char));
            var remoteAddress = NativeMethods.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)bufLen, NativeMethods.AllocationType.Commit, NativeMethods.MemoryProtection.ReadWrite);

            if (remoteAddress != IntPtr.Zero)
            {
                var address = Marshal.StringToHGlobalUni(transportDataString);
                var size = (uint)(sizeof(char) * transportDataString.Length);

                NativeMethods.WriteProcessMemory(hProcess, remoteAddress, address, size, out var bytesWritten);

                if (bytesWritten == 0)
                {
                    throw Marshal.GetExceptionForHR(Marshal.GetLastWin32Error());
                }

                var procAddress = NativeMethods.GetProcAddress(hInstance, "MessageHookProc");

                if (procAddress == UIntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.HookType.WH_CALLWNDPROC, procAddress, hInstance, threadId);

                if (hookHandle != IntPtr.Zero)
                {
                    NativeMethods.SendMessage(windowHandle, messageId, remoteAddress, IntPtr.Zero);
                    NativeMethods.UnhookWindowsHookEx(hookHandle);
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                try
                {
                    NativeMethods.VirtualFreeEx(hProcess, remoteAddress, bufLen, NativeMethods.AllocationType.Release);
                }
                catch (Exception e)
                {
                    LogMessage(e.ToString(), true);
                }
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            NativeMethods.FreeLibrary(hInstance);
        }

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