// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
#pragma warning disable CA1008
#pragma warning disable CA1028
#pragma warning disable CA1045
#pragma warning disable CA1051
#pragma warning disable CA1401
#pragma warning disable CA1806
#pragma warning disable CA1815
#pragma warning disable CA1819
#pragma warning disable CA2101

namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Win32.SafeHandles;

public static class NativeMethods
{
    public const int ERROR_ACCESS_DENIED = 5;

    public static IntPtr[] TopLevelWindows
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

    public static Dictionary<int, IList<IntPtr>> GetProcessesAndWindows()
    {
        var map = new Dictionary<int, IList<IntPtr>>();
        var rootWindows = TopLevelWindows;

        foreach (var rootWindow in rootWindows)
        {
            GetWindowThreadProcessId(rootWindow, out var processId);

            if (map.TryGetValue(processId, out var windows) == false)
            {
                windows = new List<IntPtr>();
                map.Add(processId, windows);
            }

            windows.Add(rootWindow);
        }

        return map;
    }

    public static List<IntPtr> GetRootWindowsOfProcess(int pid)
    {
        var rootWindows = TopLevelWindows;
        return GetRootWindowsOfProcess(pid, rootWindows);
    }

    public static List<IntPtr> GetRootWindowsOfProcess(int pid, IntPtr[] rootWindows)
    {
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

    private delegate bool EnumWindowsCallBackDelegate(IntPtr hwnd, IntPtr lParam);

    private static bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam)
    {
        var target = ((GCHandle)lParam).Target;

        if (target is not List<IntPtr> intPtrs)
        {
            return false;
        }

        intPtrs.Add(hwnd);

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

    private enum ImageFileMachine : ushort
    {
        I386 = 0x14C,
        AMD64 = 0x8664,
        ARM = 0x1c0,
        ARM64 = 0xAA64,
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process2(IntPtr process, out ImageFileMachine processMachine, out ImageFileMachine nativeMachine);

    public static string GetArchitectureWithoutException(Process process)
    {
        try
        {
            return GetArchitecture(process);
        }
        catch (Exception exception)
        {
            LogHelper.WriteError(exception);
            return Environment.Is64BitOperatingSystem
                ? "x64"
                : "x86";
        }
    }

    public static string GetArchitecture(Process process)
    {
        using (var processHandle = OpenProcess(process, ProcessAccessFlags.QueryLimitedInformation))
        {
            if (processHandle.IsInvalid)
            {
                throw new Exception("Could not query process information.");
            }

            try
            {
                if (IsWow64Process2(processHandle.DangerousGetHandle(), out var processMachine, out var nativeMachine) == false)
                {
                    throw new Win32Exception();
                }

                var arch = processMachine == 0
                    ? nativeMachine
                    : processMachine;

                switch (arch)
                {
                    case ImageFileMachine.I386:
                        return "x86";

                    case ImageFileMachine.AMD64:
                        return "x64";

                    case ImageFileMachine.ARM:
                        return "ARM";

                    case ImageFileMachine.ARM64:
                        return "ARM64";

                    default:
                        return "x86";
                }
            }
            catch (EntryPointNotFoundException)
            {
                if (IsWow64Process(processHandle.DangerousGetHandle(), out var isWow64) == false)
                {
                    throw new Win32Exception();
                }

                switch (isWow64)
                {
                    case true when Environment.Is64BitOperatingSystem:
                        return "x86";

                    case false when Environment.Is64BitOperatingSystem:
                        return "x64";

                    default:
                        return "x86";
                }
            }
        }
    }

    // see https://msdn.microsoft.com/en-us/library/windows/desktop/ms684139%28v=vs.85%29.aspx
    private static bool IsWow64Process(Process process)
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
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    public static IntPtr GetRemoteProcAddress(Process targetProcess, string moduleName, string procName)
    {
        long functionOffsetFromBaseAddress = 0;

        foreach (ProcessModule? mod in Process.GetCurrentProcess().Modules)
        {
            if (mod?.ModuleName is null
                || mod.FileName is null)
            {
                continue;
            }

            if (mod.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase)
                || mod.FileName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.WriteLine($"Checking module \"{moduleName}\" with base address \"{mod.BaseAddress}\" for procaddress of \"{procName}\"...");

                var procAddress = GetProcAddress(mod!.BaseAddress, procName).ToInt64();

                if (procAddress != 0)
                {
                    LogHelper.WriteLine($"Got proc address in foreign process with \"{procAddress}\".");
                    functionOffsetFromBaseAddress = procAddress - (long)mod.BaseAddress;
                }

                break;
            }
        }

        if (functionOffsetFromBaseAddress == 0)
        {
            throw new Exception($"Could not find local method handle for \"{procName}\" in module \"{moduleName}\".");
        }

        var remoteModuleHandle = GetRemoteModuleHandle(targetProcess, moduleName);
        return new IntPtr((long)remoteModuleHandle + functionOffsetFromBaseAddress);
    }

    public static IntPtr GetRemoteModuleHandle(Process targetProcess, string moduleName)
    {
        foreach (ProcessModule? mod in targetProcess.Modules)
        {
            if (mod?.ModuleName is null
                || mod?.FileName is null)
            {
                continue;
            }

            if (mod!.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase)
                || mod!.FileName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.WriteLine($"Found module \"{moduleName}\" with base address \"{mod!.BaseAddress}\".");
                return mod!.BaseAddress;
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

    [DllImport("kernel32.dll")]
    public static extern void GetCurrentThreadStackLimits(out IntPtr lowLimit, out IntPtr highLimit);

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
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_SHOWMAXIMIZED = 3;

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

    public enum HookType
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
    public static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc hookProc, IntPtr hMod, uint dwThreadId);

    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateRemoteThread(ProcessHandle handle,
        IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress,
        IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

    [DllImport("kernel32.dll")]
    public static extern bool GetExitCodeThread(IntPtr hThread, out IntPtr exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern WaitResult WaitForSingleObject(IntPtr handle, uint timeoutInMilliseconds = 0xFFFFFFFF);

    #region Console

    /// <summary>
    /// allocates a new console for the calling process.
    /// </summary>
    /// <returns>If the function succeeds, the return value is nonzero.
    /// If the function fails, the return value is zero.
    /// To get extended error information, call Marshal.GetLastWin32Error.</returns>
    [DllImport("kernel32", SetLastError = true)]
    public static extern bool AllocConsole();

    /// <summary>
    /// Detaches the calling process from its console
    /// </summary>
    /// <returns>If the function succeeds, the return value is nonzero.
    /// If the function fails, the return value is zero.
    /// To get extended error information, call Marshal.GetLastWin32Error.</returns>
    [DllImport("kernel32", SetLastError = true)]
    public static extern bool FreeConsole();

    /// <summary>
    /// Attaches the calling process to the console of the specified process.
    /// </summary>
    /// <param name="dwProcessId">[in] Identifier of the process, usually will be ATTACH_PARENT_PROCESS</param>
    /// <returns>If the function succeeds, the return value is nonzero.
    /// If the function fails, the return value is zero.
    /// To get extended error information, call Marshal.GetLastWin32Error.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool AttachConsole(uint dwProcessId);

    /// <summary>Identifies the console of the parent of the current process as the console to be attached.
    /// always pass this with AttachConsole in .NET for stability reasons and mainly because
    /// I have NOT tested interprocess attaching in .NET so don't blame me if it doesn't work! </summary>
    public const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;

    #endregion

    /// <summary>
    /// Try to get the relative mouse position to the given handle in client coordinates.
    /// </summary>
    /// <param name="hWnd">The handle for this method.</param>
    /// <param name="point">The relative mouse position to the given handle.</param>
    public static bool TryGetRelativeMousePosition(IntPtr hWnd, out POINT point)
    {
        point = default;

        var returnValue = hWnd != IntPtr.Zero
                          && TryGetPhysicalCursorPos(out point);

        if (returnValue)
        {
            ScreenToClient(hWnd, ref point);
        }

        return returnValue;
    }

    public static bool TryGetPhysicalCursorPos(out POINT pt)
    {
        var returnValue = _GetPhysicalCursorPos(out pt);
        // Sometimes Win32 will fail this call, such as if you are
        // not running in the interactive desktop. For example,
        // a secure screen saver may be running.
        if (!returnValue)
        {
            System.Diagnostics.Debug.WriteLine("GetPhysicalCursorPos failed!");
            pt.X = 0;
            pt.Y = 0;
        }

        return returnValue;
    }

    [DllImport("user32.dll", CharSet = CharSet.None, SetLastError = true, EntryPoint = "ScreenToClient")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll", EntryPoint = "GetPhysicalCursorPos", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
#pragma warning disable SA1300
    private static extern bool _GetPhysicalCursorPos(out POINT lpPoint);
#pragma warning restore SA1300
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
    [XmlIgnore]
    public int Length;
    [XmlIgnore]
    public int Flags;
    public int ShowCmd;
    [XmlIgnore]
    public POINT MinPosition;
    [XmlIgnore]
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

public static class ConsoleHelper
{
    /// <summary>
    /// Allocate a console if application started from within windows GUI.
    /// Detects the presence of an existing console associated with the application and
    /// attaches itself to it if available.
    /// </summary>
    public static void AttachConsoleToParentProcessOrAllocateNewOne()
    {
        if (NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS) == false
            && Marshal.GetLastWin32Error() == NativeMethods.ERROR_ACCESS_DENIED)
        {
            // A console was not allocated, so we need to make one.
            if (NativeMethods.FreeConsole() == false)
            {
                Trace.WriteLine("Console could not be freed.");
            }
            else
            {
                Trace.WriteLine("Console freed.");
            }

            if (NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS) == false)
            {
                Trace.WriteLine($"Could not attach to parent process console. Error = {Marshal.GetLastWin32Error()}");
            }
            else
            {
                Trace.WriteLine("Console attached to parent process.");
            }
        }
        else
        {
            Trace.WriteLine("Console attached to parent process or process is a standalone console application.");
        }
    }
}