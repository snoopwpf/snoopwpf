namespace Snoop.Infrastructure
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    public class LowLevelMouseHook
    {
        private const int WH_MOUSE_LL = 14;

        internal IntPtr hookID = IntPtr.Zero;

        // We need to place this on a field/member.
        // Otherwise the delegate will be garbage collected and our hook crashes.
        private readonly LowLevelMouseProc cachedProc;

        public LowLevelMouseHook()
        {
            this.cachedProc = this.HookCallback;
        }

        public class LowLevelMouseMoveEventArgs : EventArgs
        {
            public LowLevelMouseMoveEventArgs(POINT point)
            {
                this.Point = point;
            }

            public POINT Point { get; }
        }

        public event EventHandler<LowLevelMouseMoveEventArgs> LowLevelMouseMove;

        public bool IsRunning => this.hookID != IntPtr.Zero;

        public void Start()
        {
            if (this.hookID != IntPtr.Zero)
            {
                return;
            }

            this.hookID = CreateHook(this.cachedProc);
        }

        public void Stop()
        {
            if (this.hookID == IntPtr.Zero)
            {
                return;
            }

            UnhookWindowsHookEx(this.hookID);
            this.hookID = IntPtr.Zero;
        }

        private static IntPtr CreateHook(LowLevelMouseProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            {
                using (var curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                //you need to call CallNextHookEx without further processing
                //and return the value returned by CallNextHookEx
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }

            if (MouseMessages.WM_LBUTTONUP == (MouseMessages)wParam)
            {
            }

            var xx = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            this.LowLevelMouseMove?.Invoke(this, new LowLevelMouseMoveEventArgs(xx.pt));

            return CallNextHookEx(this.hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        internal delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public readonly POINT pt;
            public readonly int mouseData;
            public readonly int flags;
            public readonly int time;
            public readonly IntPtr dwExtraInfo;
        }
    }
}