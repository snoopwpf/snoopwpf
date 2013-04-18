// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.IO;

namespace Snoop
{

    /// <summary>
    /// Class for ILSpy messaging. We'll move this after code review.
    /// </summary>
    public static class ILSpyInterop
    {
        public const string ILSPY_PREFIX = "ILSpy:";
        public const string ILSPY_NAVIGATE_TO_TYPE = "/navigateTo:T:";
        public const string ILSPY_NAVIGATE_TO_METHOD = "/navigateTo:M:";
        public const string ILSPY_LINE_BREAK = "\r\n";
        private static readonly string NAVIGATE_TO_TYPE_MESSAGE;//Format of message to be sent to an existing ilspy process via send message.
        private static readonly string NAVIGATE_TO_METHOD_MESSAGE;//Format of message to be sent to an existing ilspy process via send message.
        private static readonly string NAVIGATE_TO_TYPE_ARGUMENT;//Format of command line argument to be sent when starting ILSpy
        private static readonly string NAVIGATE_TO_METHOD_ARGUMENT;//Format of command line argument to be sent when starting ILSpy

        static ILSpyInterop()
        {
            NAVIGATE_TO_TYPE_MESSAGE = ILSPY_PREFIX + ILSPY_LINE_BREAK + "{0}" + ILSPY_LINE_BREAK + ILSPY_NAVIGATE_TO_TYPE + "{1}";
            NAVIGATE_TO_METHOD_MESSAGE = ILSPY_PREFIX + ILSPY_LINE_BREAK + "{0}" + ILSPY_LINE_BREAK + ILSPY_NAVIGATE_TO_METHOD + "{1}";
            //"\"{0}\" /navigateTo:T:{1}"
            NAVIGATE_TO_TYPE_ARGUMENT = "\"{0}\" " + ILSPY_NAVIGATE_TO_TYPE + "{1}";
            NAVIGATE_TO_METHOD_ARGUMENT = "\"{0}\" " + ILSPY_NAVIGATE_TO_METHOD + "{1}";
        }

        public static void OpenTypeInILSpy(string fullAssemblyPath, string fullTypeName, Process ilSpyProcess)
        {
            IntPtr windowHandle = ilSpyProcess.MainWindowHandle;
            string args = string.Format(NAVIGATE_TO_TYPE_MESSAGE, fullAssemblyPath, fullTypeName);
            NativeMethods.SetForegroundWindow(ilSpyProcess.MainWindowHandle);
            NativeMethods.Send(windowHandle, args);

        }

        public static void OpenTypeInILSpy(string fullAssemblyPath, string fullTypeName, IntPtr windowHandle)
        {
            fullTypeName = fullTypeName.Replace('+', '.');
            string args = string.Format(NAVIGATE_TO_TYPE_MESSAGE, fullAssemblyPath, fullTypeName);
            NativeMethods.SetForegroundWindow(windowHandle);
            NativeMethods.Send(windowHandle, args);

        }

        public static void OpenMethodInILSpy(string fullAssemblyPath, string fullTypeName, string methodName, IntPtr windowHandle)
        {
            fullTypeName = fullTypeName.Replace('+', '.');
            string args = string.Format(NAVIGATE_TO_METHOD_MESSAGE, fullAssemblyPath, fullTypeName + "." + methodName);
            NativeMethods.SetForegroundWindow(windowHandle);
            NativeMethods.Send(windowHandle, args);

        }

        //public static Process GetOrCreateILSpyProcess(string fullAssemblyPath, string fullTypeName)
        //{
        //    fullTypeName = fullTypeName.Replace('+', '.');
        //    //string arguments = string.Format("\"{0}\" /navigateTo:T:{1}", fullAssemblyPath, fullTypeName);
        //    //string sendToProcessArgs = string.Format("ILSpy:\r\n{0}\r\n/navigateTo:T:{1}", fullAssemblyPath, fullTypeName);
        //    string arguments = string.Format(NAVIGATE_TO_TYPE_ARGUMENT, fullAssemblyPath, fullTypeName);
        //    string sendToProcessArgs = string.Format(NAVIGATE_TO_TYPE_MESSAGE, fullAssemblyPath, fullTypeName);
        //    return CreateILSpyProcessWithArguments(fullAssemblyPath, fullTypeName, arguments, sendToProcessArgs);
        //}

        //public static Process GetOrCreateILSpyProcess(string fullAssemblyPath, string fullTypeName, string methodName)
        //{
        //    fullTypeName = fullTypeName.Replace('+', '.');
        //    //string arguments = string.Format("\"{0}\" /navigateTo:M:{1}", fullAssemblyPath, fullTypeName + "." + methodName);
        //    //string sendToProcessArgs = string.Format("ILSpy:\r\n{0}\r\n/navigateTo:M:{1}", fullAssemblyPath, fullTypeName + "." + methodName);
        //    string arguments = string.Format(NAVIGATE_TO_METHOD_ARGUMENT, fullAssemblyPath, fullTypeName + "." + methodName);
        //    string sendToProcessArgs = string.Format(NAVIGATE_TO_METHOD_MESSAGE, fullAssemblyPath, fullTypeName + "." + methodName);

        //    return CreateILSpyProcessWithArguments(fullAssemblyPath, fullTypeName, arguments, sendToProcessArgs);
        //}

        public static Process GetILSpyProcess()
        {
            var processes = Process.GetProcessesByName("ILSpy");
            if (processes.Length > 0)
            {
                var ilSpyProcess = processes[0];
                NativeMethods.SetForegroundWindow(ilSpyProcess.MainWindowHandle);
                //NativeMethods.Send(ilSpyProcess.MainWindowHandle, sendToProcessArgs);

                return ilSpyProcess;
            }
            return null;
        }

        private static Process CreateILSpyProcessWithArguments(string fullAssemblyPath, string fullTypeName, string arguments, string sendToProcessArgs)
        {
            Process ilSpyProcess = null;
            var location = typeof(Snoop.SnoopUI).Assembly.Location;
            var directory = Path.GetDirectoryName(location);
            directory = Path.Combine(directory, "ILSpy");
            var ilSpyProgram = Path.Combine(directory, "ILSpy.exe");

            var processes = Process.GetProcessesByName("ILSpy");
            if (processes.Length > 0)
            {
                ilSpyProcess = processes[0];
                NativeMethods.SetForegroundWindow(ilSpyProcess.MainWindowHandle);
                NativeMethods.Send(ilSpyProcess.MainWindowHandle, sendToProcessArgs);

                return ilSpyProcess;
            }

            ilSpyProcess = new Process();
            ilSpyProcess.StartInfo.FileName = ilSpyProgram;
            ilSpyProcess.StartInfo.WorkingDirectory = directory;
            ilSpyProcess.StartInfo.Arguments = arguments;
            //ilSpyProcess.EnableRaisingEvents = true;
            ilSpyProcess.Start();
            return ilSpyProcess;
        }
    }

    //ILSPY
    [StructLayout(LayoutKind.Sequential)]
    public struct CopyDataStruct
    {
        public IntPtr Padding;
        public int Size;
        public IntPtr Buffer;

        public CopyDataStruct(IntPtr padding, int size, IntPtr buffer)
        {
            this.Padding = padding;
            this.Size = size;
            this.Buffer = buffer;
        }
    }

	public static class NativeMethods
    {

        #region ILSpy
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd, uint msg, IntPtr wParam, ref CopyDataStruct lParam,
            uint flags, uint timeout, out IntPtr result);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public const uint WM_COPYDATA = 0x4a;

        public static IntPtr Send(IntPtr hWnd, string message)
        {
            const uint SMTO_NORMAL = 0;

            CopyDataStruct lParam;
            lParam.Padding = IntPtr.Zero;
            lParam.Size = message.Length * 2;
            lParam.Buffer = Marshal.StringToHGlobalUni(message);

            IntPtr result;
            NativeMethods.SendMessageTimeout(hWnd, NativeMethods.WM_COPYDATA, IntPtr.Zero, ref lParam, SMTO_NORMAL, 3000,
                                             out result);
            return result;
        }

        #endregion

        public static IntPtr[] ToplevelWindows
		{
			get
			{
				List<IntPtr> windowList = new List<IntPtr>();
				GCHandle handle = GCHandle.Alloc(windowList);
				try
				{
					NativeMethods.EnumWindows(NativeMethods.EnumWindowsCallback, (IntPtr)handle);
				}
				finally
				{
					handle.Free();
				}

				return windowList.ToArray();
			}
		}
		public static Process GetWindowThreadProcess(IntPtr hwnd)
		{
			int processID;
			NativeMethods.GetWindowThreadProcessId(hwnd, out processID);

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

		[StructLayoutAttribute(LayoutKind.Sequential)]
		public struct MODULEENTRY32
		{
			public uint dwSize;
			public uint th32ModuleID;
			public uint th32ProcessID;
			public uint GlblcntUsage;
			public uint ProccntUsage;
			IntPtr modBaseAddr;
			public uint modBaseSize;
			IntPtr hModule;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string szModule;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szExePath;
		};

		public class ToolHelpHandle : SafeHandleZeroOrMinusOneIsInvalid
		{
			private ToolHelpHandle()
				: base(true)
			{
			}

			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
			override protected bool ReleaseHandle()
			{
				return NativeMethods.CloseHandle(handle);
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

		[DllImport("user32.dll")]
		private static extern int EnumWindows(EnumWindowsCallBackDelegate callback, IntPtr lParam);

		[DllImport("user32.dll")]
		public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);

		[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		[DllImport("kernel32")]
		public extern static IntPtr LoadLibrary(string librayName);

		[DllImport("kernel32.dll", SetLastError = true)]
		static public extern ToolHelpHandle CreateToolhelp32Snapshot(SnapshotFlags dwFlags, int th32ProcessID);

		[DllImport("kernel32.dll")]
		static public extern bool Module32First(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

		[DllImport("kernel32.dll")]
		static public extern bool Module32Next(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

		[DllImport("kernel32.dll", SetLastError = true)]
		static public extern bool CloseHandle(IntPtr hHandle);


		// anvaka's changes below


		public static Point GetCursorPosition()
		{
			var pos = new Point();
			var win32Point = new POINT();
			if (GetCursorPos(ref win32Point))
			{
				pos.X = win32Point.X;
				pos.Y = win32Point.Y;
			}
			return pos;
		}

		public static IntPtr GetWindowUnderMouse()
		{
			POINT pt = new POINT();
			if (GetCursorPos(ref pt))
			{
				return WindowFromPoint(pt);
			}
			return IntPtr.Zero;
		}

		public static Rect GetWindowRect(IntPtr hwnd)
		{
			RECT rect = new RECT();
			GetWindowRect(hwnd, out rect);
			return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetCursorPos(ref POINT pt);
		
		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(POINT Point);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
	}
}
