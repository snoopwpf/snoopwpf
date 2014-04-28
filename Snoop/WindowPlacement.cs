// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;

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
        public static implicit operator string(WINDOWPLACEMENT placement) {
            return placement.SR();
        }
        public static implicit operator WINDOWPLACEMENT(string placement) {
            return placement.DSR<WINDOWPLACEMENT>();
        }
	}

    public static class XSH {
        public static string SR<T>(this T val) {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringBuilder builder =new StringBuilder();
            StringWriter writer = new StringWriter(builder);
            serializer.Serialize(writer, val);
            return builder.ToString();
        }
        public static T DSR<T>(this string val) {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            StringReader reader = new StringReader(val);
            return (T)serializer.Deserialize(reader);
        }
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
}
