namespace Snoop.Infrastructure;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class LowLevelMouseHook
{
    private IntPtr hookId = IntPtr.Zero;

    // We need to place this on a field/member.
    // Otherwise the delegate will be garbage collected and our hook crashes.
    private readonly NativeMethods.HookProc cachedProc;

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

    public event EventHandler<LowLevelMouseMoveEventArgs>? LowLevelMouseMove;

    public bool IsRunning => this.hookId != IntPtr.Zero;

    public void Start()
    {
        if (this.hookId != IntPtr.Zero)
        {
            return;
        }

        this.hookId = CreateHook(this.cachedProc);
    }

    public void Stop()
    {
        if (this.hookId == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnhookWindowsHookEx(this.hookId);
        this.hookId = IntPtr.Zero;
    }

    private static IntPtr CreateHook(NativeMethods.HookProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        return NativeMethods.SetWindowsHookEx(NativeMethods.HookType.WH_MOUSE_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            //you need to call CallNextHookEx without further processing
            //and return the value returned by CallNextHookEx
            return NativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        var hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

        this.LowLevelMouseMove?.Invoke(this, new LowLevelMouseMoveEventArgs(hookStruct.Point));

        return NativeMethods.CallNextHookEx(this.hookId, nCode, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once IdentifierTypo
    private struct MSLLHOOKSTRUCT
    {
        public readonly POINT Point;

        public readonly int MouseData;

        public readonly int Flags;

        public readonly int Time;

        public readonly IntPtr ExtraInfo;
    }
}