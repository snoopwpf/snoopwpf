namespace Snoop
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Runtime.InteropServices;

	public static class NativeMethods
	{
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
		[DllImport("user32.Dll")]
		private static extern int EnumWindows(EnumWindowsCallBackDelegate callback, IntPtr lParam);

		[DllImport("user32.Dll")]
		private static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);

		private static bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam)
		{
			((List<IntPtr>)((GCHandle)lParam).Target).Add(hwnd);
			return true;
		}
	}
}
