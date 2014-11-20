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
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Windows.Media;
using DevExpress.Xpf.Core.Internal;

namespace Snoop
{
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
    public static class DXMethods {
        static readonly ReflectionHelper Helper = new ReflectionHelper();
        public static int RenderChildrenCount(object obj) {
            return Helper.GetInstanceMethodHandler<object, Func<object, int>>(obj, "get_RenderChildrenCount", BindingFlags.NonPublic | BindingFlags.Instance)(obj);
        }
        public static FrameworkElement GetParent(object elementHost) {
            return ReflectionHelper.CreateInstanceMethodHandler<Func<object, FrameworkElement>>(elementHost, "get_Parent", BindingFlags.Public | BindingFlags.Instance, GetCoreAssembly(elementHost).GetType("DevExpress.Xpf.Core.Native.IElementHost"), false, null, typeof(object))(elementHost);
        }
        public static bool Is(object obj, string typeName, string typeNamespace, bool isInterface) {
            if (obj == null)
                return false;
            var type = obj.GetType();
            while(type!=null) {
                Type[] types = { type };
                if(isInterface) {
                    types = types.Concat(type.GetInterfaces()).ToArray();
                }
                foreach(var typeOrInterface in types) {
                    bool isValidType = 
                        (string.IsNullOrEmpty(typeNamespace) || string.Equals(typeNamespace, typeOrInterface.Namespace))
                        && (string.IsNullOrEmpty(typeName) || string.Equals(typeName, typeOrInterface.Name));
                    if (isValidType)
                        return true;
                }                
                type = type.BaseType;
            }
            return false;
        }

        public static void Render(object factory, object dc, object context) {
            ReflectionHelper.CreateInstanceMethodHandler<Action<object, object, object>>(factory, "Render", BindingFlags.Public | BindingFlags.Instance, factory.GetType(), true, null, typeof(object))(factory, dc, context);
        }

        public static bool IsChrome(object obj) {
            return Is(obj, "Chrome", "DevExpress.Xpf.Core.Native", false);
        }
        public static bool IsIFrameworkRenderElementContext(object obj) {
            return Is(obj, "IFrameworkRenderElementContext", "DevExpress.Xpf.Core.Native", true);
        }
        public static bool IsFrameworkRenderElementContext(object obj) {
            return Is(obj, "FrameworkRenderElementContext", "DevExpress.Xpf.Core.Native", false);
        }
        public static Assembly GetCoreAssembly(object obj) {
            if (Is(obj, null, "DevExpress.Xpf.Core.Native", false)) {
                return obj.GetType().Assembly;
            }
            return null;
        }
        public static object GetRenderChild(object source, int index) {
            return Helper.GetInstanceMethodHandler<object, Func<object, int, object>>(
                source, "GetRenderChild", BindingFlags.NonPublic | BindingFlags.Instance)(source, index);
        }
    }    
    public class RenderTreeHelper {
        [ThreadStatic]
        static Func<object, IEnumerable> renderDescendants;
        public static IEnumerable<object> RenderDescendants(object context) {
            if (renderDescendants == null)
                renderDescendants = ReflectionHelper.CreateInstanceMethodHandler<Func<object, IEnumerable>>(
                    null,
                    "RenderDescendants",
                    BindingFlags.Public | BindingFlags.Static,
                    DXMethods.GetCoreAssembly(context).GetType("DevExpress.Xpf.Core.Native.RenderTreeHelper"),
                    true, typeof(IEnumerable), null
                    );
            return renderDescendants(context).OfType<object>();
        }
        [ThreadStatic]
        static Func<object, Transform> transformToRoot;
        public static Transform TransformToRoot(object frec) {
            if (transformToRoot == null)
                transformToRoot = ReflectionHelper.CreateInstanceMethodHandler<Func<object, Transform>>(
                    null,
                    "TransformToRoot",
                    BindingFlags.Public | BindingFlags.Static,
                    DXMethods.GetCoreAssembly(frec).GetType("DevExpress.Xpf.Core.Native.RenderTreeHelper"),
                    true, typeof(Transform), null
                    );
            return transformToRoot(frec);
        }
        [ThreadStatic]
        static Func<object, IEnumerable> renderAncestors;
        public static IEnumerable<object> RenderAncestors(object context) {
            if (renderAncestors == null)
                renderAncestors = ReflectionHelper.CreateInstanceMethodHandler<Func<object, IEnumerable>>(
                    null,
                    "RenderAncestors",
                    BindingFlags.Public | BindingFlags.Static,
                    DXMethods.GetCoreAssembly(context).GetType("DevExpress.Xpf.Core.Native.RenderTreeHelper"),
                    true, typeof(IEnumerable), null
                    );
            return renderAncestors(context).OfType<object>();
        }
        [ThreadStatic]
        static Func<object, object, object> hitTest;
        public static object HitTest(object root, Point point) {
            if (hitTest == null)
                hitTest = ReflectionHelper.CreateInstanceMethodHandler<Func<object, object, object>>(
                    null,
                    "HitTest",
                    BindingFlags.Public | BindingFlags.Static,
                    DXMethods.GetCoreAssembly(root).GetType("DevExpress.Xpf.Core.Native.RenderTreeHelper"),
                    true, typeof(object), null, 2
                    );
            return hitTest(root, point);
        }
    }
}
