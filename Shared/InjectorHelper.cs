namespace Snoop
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Xml.Serialization;

    public static class InjectorHelper
    {
        private static readonly uint messageId;

        static InjectorHelper()
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

            var fi = new FileInfo(pathname);

            using (var sw = fi.AppendText())
            {
                sw.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " : " + message);
            }
        }

        public static void Launch(IntPtr windowHandle, InjectorData injectorData)
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
                //var version = applicationInfo.RuntimeVersion.Contains("4") ? "40" : "35";
                //var hookName = string.Format("Hook{0}_{1}.dll", applicationInfo.Bitness, version);

                var bitness = Environment.Is64BitProcess
                    ? "x64"
                    : "x86";

                var framework = IsDotNetCoreProcess(process)
                                    ? "netcoreapp3.0"
                                    : "net40";

                var hookName = $"ManagedInjector.{framework}.{bitness}.dll";

                var hInstance = NativeMethods.LoadLibrary(hookName);

                if (hInstance == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                var bufLen = (transportDataString.Length + 1) * Marshal.SizeOf(typeof(char));
                var hProcess = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, processId);
                var remoteAddress = NativeMethods.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)bufLen,
                                                                   NativeMethods.AllocationType.Commit,
                                                                   NativeMethods.MemoryProtection.ReadWrite);

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
        }

        private static bool IsDotNetCoreProcess(Process process)
        {
            var modules = NativeMethods.GetModules(process);

            foreach (var module in modules)
            {
                if (module.szModule.IndexOf("wpfgfx_cor3", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class InjectorData
    {
        public string AssemblyName { get; set; }

        public string ClassName { get; set; }

        public string MethodName { get; set; }

        public string SettingsFile { get; set; }
    }
}