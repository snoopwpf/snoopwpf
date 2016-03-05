// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Snoop
{
	// AK: TODO: Move this to NativeMethods.cs

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
		public int length;
		public int flags;
		public int showCmd;
		public POINT minPosition;
		public POINT maxPosition;
		public RECT normalPosition;
	}

	public static class Win32
	{
		[DllImport("user32.dll")]
		public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll")]
		public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

		public const int SW_SHOWNORMAL = 1;
		public const int SW_SHOWMINIMIZED = 2;
	}

	public static class WindowPlacement
	{
		// Win32 API declarations to set and get window placement
		[DllImport("user32.dll")]
		static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32.dll")]
		static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

		const int SW_SHOWNORMAL = 1;
		const int SW_SHOWMINIMIZED = 2;

		public static void SetPlacement(this Window window, WINDOWPLACEMENT wp)
		{
			try
			{
				wp.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
				wp.flags = 0;
				if (wp.showCmd == SW_SHOWMINIMIZED)
					wp.showCmd = SW_SHOWNORMAL;
				IntPtr hwnd = new WindowInteropHelper(window).Handle;
				SetWindowPlacement(hwnd, ref wp);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
			}
		}

		public static WINDOWPLACEMENT? GetPlacement(this Window window)
		{
			WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
			IntPtr hwnd = new WindowInteropHelper(window).Handle;
			if (GetWindowPlacement(hwnd, out wp))
				return wp;
			else
				return null;
		}
	}
}
