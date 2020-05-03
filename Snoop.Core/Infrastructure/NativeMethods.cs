// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using Microsoft.Win32.SafeHandles;

    public static class NativeMethods
    {
        public const int ERROR_ACCESS_DENIED = 5;

        public static IntPtr[] ToplevelWindows
        {
            get
            {
                var windowList = new List<IntPtr>();
                var handle = GCHandle.Alloc(windowList);
                try
                {
                    EnumWindows(EnumWindowsCallback, (IntPtr)handle);
                }
                finally
                {
                    handle.Free();
                }

                return windowList.ToArray();
            }
        }

        public static List<IntPtr> GetRootWindowsOfCurrentProcess()
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                return GetRootWindowsOfProcess(currentProcess.Id);
            }
        }

        public static List<IntPtr> GetRootWindowsOfProcess(int pid)
        {
            var rootWindows = ToplevelWindows;
            var dsProcRootWindows = new List<IntPtr>();

            foreach (var hWnd in rootWindows)
            {
                GetWindowThreadProcessId(hWnd, out var processId);
                if (processId == pid)
                {
                    dsProcRootWindows.Add(hWnd);
                }
            }

            return dsProcRootWindows;
        }

        public static Process GetWindowThreadProcess(IntPtr hwnd)
        {
            int processID;
            GetWindowThreadProcessId(hwnd, out processID);

            try
            {
                return Process.GetProcessById(processID);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private delegate bool EnumWindowsCallBackDelegate(IntPtr hwnd, IntPtr lParam);

        private static bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam)
        {
            ((List<IntPtr>)((GCHandle)lParam).Target).Add(hwnd);
            return true;
        }

        [DebuggerDisplay("{" + nameof(szModule) + "}")]
        [StructLayout(LayoutKind.Sequential)]
        public struct MODULEENTRY32
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            public readonly IntPtr modBaseAddr;
            public uint modBaseSize;
            public readonly IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }

        public class ToolHelpHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private ToolHelpHandle()
                : base(true)
            {
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            protected override bool ReleaseHandle()
            {
                return CloseHandle(this.handle);
            }
        }

        public class ProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private ProcessHandle()
                : base(true)
            {
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            protected override bool ReleaseHandle()
            {
                return CloseHandle(this.handle);
            }
        }

        [Flags]
        public enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F
        }

        public static bool IsProcess64BitWithoutException(Process process)
        {
            try
            {
                return IsProcess64Bit(process);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                return false;
            }
        }

        // see https://msdn.microsoft.com/en-us/library/windows/desktop/ms684139%28v=vs.85%29.aspx
        public static bool IsProcess64Bit(Process process)
        {
            if (Environment.Is64BitOperatingSystem == false)
            {
                return false;
            }

            // if this method is not available in your version of .NET, use GetNativeSystemInfo via P/Invoke instead
            using (var processHandle = OpenProcess(process, ProcessAccessFlags.QueryLimitedInformation))
            {
                if (processHandle.IsInvalid)
                {
                    throw new Exception("Could not query process information.");
                }

                if (IsWow64Process(processHandle.DangerousGetHandle(), out var isWow64) == false)
                {
                    throw new Win32Exception();
                }

                return isWow64 == false;
            }
        }

        public static bool IsProcessElevated(Process process)
        {
            using (var processHandle = OpenProcess(process, ProcessAccessFlags.QueryInformation))
            {
                if (processHandle.IsInvalid)
                {
                    var error = Marshal.GetLastWin32Error();

                    return error == ERROR_ACCESS_DENIED;
                }

                return false;
            }
        }

        /// <summary>
        /// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
        /// except that we include 32 bit modules when Snoop runs in 64 bit mode.
        /// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
        /// </summary>
        public static IEnumerable<MODULEENTRY32> GetModulesFromWindowHandle(IntPtr windowHandle)
        {
            GetWindowThreadProcessId(windowHandle, out var processId);

            return GetModules(processId);
        }

        /// <summary>
        /// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
        /// except that we include 32 bit modules when Snoop runs in 64 bit mode.
        /// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
        /// </summary>
        public static IEnumerable<MODULEENTRY32> GetModules(Process process)
        {
            return GetModules(process.Id);
        }

        /// <summary>
        /// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
        /// except that we include 32 bit modules when Snoop runs in 64 bit mode.
        /// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
        /// </summary>
        public static IEnumerable<MODULEENTRY32> GetModules(int processId)
        {
            var me32 = default(MODULEENTRY32);
            var hModuleSnap = CreateToolhelp32Snapshot(SnapshotFlags.Module | SnapshotFlags.Module32, processId);

            if (hModuleSnap.IsInvalid)
            {
                yield break;
            }

            using (hModuleSnap)
            {
                me32.dwSize = (uint)Marshal.SizeOf(me32);

                if (Module32First(hModuleSnap, ref me32))
                {
                    do
                    {
                        yield return me32;
                    }
                    while (Module32Next(hModuleSnap, ref me32));
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern int EnumWindows(EnumWindowsCallBackDelegate callback, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);

        [DllImport("Kernel32.dll")]
        public static extern int GetProcessId(ProcessHandle processHandle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);

        public static string GetClassName(IntPtr hwnd)
        {
            // Pre-allocate 256 characters, since this is the maximum class name length.
            var className = new StringBuilder(256);

            //Get the window class name
            var result = GetClassName(hwnd, className, className.Capacity);

            return result != 0
                       ? className.ToString()
                       : string.Empty;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

        public static string GetText(IntPtr hWnd)
        {
            // Allocate correct string length first
            var length = GetWindowTextLength(hWnd);
            var sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        public static extern ProcessHandle OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        public static ProcessHandle OpenProcess(Process proc, ProcessAccessFlags flags)
        {
            return OpenProcess(flags, false, proc.Id);
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern UIntPtr GetProcAddress(IntPtr hModule, string procName);

        public static UIntPtr GetRemoteProcAddress(Process targetProcess, string moduleName, string procName)
        {
            ulong functionOffsetFromBaseAddress = 0;

            foreach (ProcessModule mod in Process.GetCurrentProcess().Modules)
            {
                if (mod.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase)
                    || mod.FileName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    Trace.WriteLine($"Checking module \"{moduleName}\" with base address \"{mod.BaseAddress}\" for procaddress of \"{procName}\"...");

                    var procAddress = GetProcAddress(mod.BaseAddress, procName).ToUInt64();

                    if (procAddress != 0)
                    {
                        Trace.WriteLine($"Got proc address in foreign process with \"{procAddress}\".");
                        functionOffsetFromBaseAddress = procAddress - (ulong)mod.BaseAddress;
                    }

                    break;
                }
            }

            if (functionOffsetFromBaseAddress == 0)
            {
                throw new Exception($"Could not find local method handle for \"{procName}\" in module \"{moduleName}\".");
            }

            var remoteModuleHandle = GetRemoteModuleHandle(targetProcess, moduleName);
            return new UIntPtr((ulong)remoteModuleHandle + functionOffsetFromBaseAddress);
        }

        public static IntPtr GetRemoteModuleHandle(Process targetProcess, string moduleName)
        {
            foreach (ProcessModule mod in targetProcess.Modules)
            {
                if (mod.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase)
                    || mod.FileName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return mod.BaseAddress;
                }
            }

            return IntPtr.Zero;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32")]
        public static extern IntPtr LoadLibrary(string librayName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern ToolHelpHandle CreateToolhelp32Snapshot(SnapshotFlags dwFlags, int th32ProcessID);

        [DllImport("kernel32.dll")]
        public static extern bool Module32First(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll")]
        public static extern bool Module32Next(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        public static IntPtr GetWindowUnderMouse()
        {
            var pt = default(POINT);
            if (GetCursorPos(ref pt))
            {
                return WindowFromPoint(pt);
            }

            return IntPtr.Zero;
        }

        //public static System.Windows.Rect GetWindowRect(IntPtr hwnd)
        //{
        //  RECT rect = new RECT();
        //  GetWindowRect(hwnd, out rect);
        //  return new System.Windows.Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        //}

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(ref POINT pt);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;

        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        public enum HookType : int
        {
            WH_JOURNALRECORD = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD = 2,
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CBT = 5,
            WH_SYSMSGFILTER = 6,
            WH_MOUSE = 7,
            WH_HARDWARE = 8,
            WH_DEBUG = 9,
            WH_SHELL = 10,
            WH_FOREGROUNDIDLE = 11,
            WH_CALLWNDPROCRET = 12,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(ProcessHandle hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(ProcessHandle hProcess, IntPtr lpAddress, int dwSize, AllocationType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(ProcessHandle hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(HookType hookType, UIntPtr lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(ProcessHandle handle,
                                                IntPtr lpThreadAttributes, uint dwStackSize, UIntPtr lpStartAddress,
                                                IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        public static extern bool GetExitCodeThread(IntPtr hThread, out IntPtr exitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern WaitResult WaitForSingleObject(IntPtr handle, uint timeoutInMilliseconds = 0xFFFFFFFF);
    }

    // RECT structure required by WINDOWPLACEMENT structure
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.Left = left;
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
        }

        public int Width => this.Right - this.Left;

        public int Height => this.Bottom - this.Top;
    }

    // POINT structure required by WINDOWPLACEMENT structure
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    // WINDOWPLACEMENT stores the position, size, and state of a window
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int Length;
        public int Flags;
        public int ShowCmd;
        public POINT MinPosition;
        public POINT MaxPosition;
        public RECT NormalPosition;
    }

    public enum Wait
    {
        INFINITE = -1,
    }

    public enum WaitResult
    {
        WAIT_ABANDONED = 0x80,
        WAIT_OBJECT_0 = 0x00,
        WAIT_TIMEOUT = 0x102,
        WAIT_FAILED = -1
    }
}
