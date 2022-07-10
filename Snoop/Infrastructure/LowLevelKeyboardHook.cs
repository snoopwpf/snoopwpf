namespace Snoop.Infrastructure;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

public class LowLevelKeyboardHook
{
    private readonly PresentationSource presentationSource;
#pragma warning disable SA1310 // Field names should not contain underscore
    // ReSharper disable InconsistentNaming
    private static readonly IntPtr WM_KEYDOWN = new(0x0100);
    private static readonly IntPtr WM_KEYUP = new(0x0101);
    // ReSharper restore InconsistentNaming
#pragma warning restore SA1310 // Field names should not contain underscore

    private IntPtr hookId = IntPtr.Zero;

    // We need to place this on a field/member.
    // Otherwise the delegate will be garbage collected and our hook crashes.
    private readonly NativeMethods.HookProc cachedProc;

    public LowLevelKeyboardHook(PresentationSource presentationSource)
    {
        this.presentationSource = presentationSource;
        this.cachedProc = this.HookCallback;
    }

    //public class LowLevelKeyPressEventArgs : EventArgs
    //{
    //    public LowLevelKeyPressEventArgs(ModifierKeys modifierKeys, Key key)
    //    {
    //        this.ModifierKeys = modifierKeys;
    //        this.Key = key;
    //    }

    //    public ModifierKeys ModifierKeys { get; }

    //    public Key Key { get; }

    //    public static LowLevelKeyPressEventArgs CreateNew(Key key)
    //    {
    //        var modifierKeys = Keyboard.Modifiers;

    //        if (Keyboard.IsKeyDown(Key.LWin)
    //            || Keyboard.IsKeyDown(Key.RWin))
    //        {
    //            modifierKeys |= ModifierKeys.Windows;
    //        }

    //        return new LowLevelKeyPressEventArgs(modifierKeys, key);
    //    }
    //}

    public event EventHandler<KeyEventArgs>? LowLevelKeyDown;

    public event EventHandler<KeyEventArgs>? LowLevelKeyUp;

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

        return NativeMethods.SetWindowsHookEx(NativeMethods.HookType.WH_KEYBOARD_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // ReSharper disable once InconsistentNaming
        const int HC_ACTION = 0;
        //you need to call CallNextHookEx without further processing
        //and return the value returned by CallNextHookEx
        if (nCode == HC_ACTION)
        {
            var hookStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

            if (wParam == WM_KEYDOWN)
            {
                this.LowLevelKeyDown?.Invoke(this, CreateEventArgs(this.presentationSource, hookStruct));
            }
            else if (wParam == WM_KEYUP)
            {
                this.LowLevelKeyUp?.Invoke(this, CreateEventArgs(this.presentationSource, hookStruct));
            }
        }

        return NativeMethods.CallNextHookEx(this.hookId, nCode, wParam, lParam);
    }

    private static KeyEventArgs CreateEventArgs(PresentationSource presentationSource, KBDLLHOOKSTRUCT hookStruct)
    {
        var key = KeyInterop.KeyFromVirtualKey(hookStruct.VKCode);

        var keyEventArgs = new KeyEventArgs(Keyboard.PrimaryDevice, presentationSource, -1, key);

        return keyEventArgs;
    }

    // ReSharper disable InconsistentNaming
    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int VKCode;
        public int ScanCode;
        public KBDLLHOOKSTRUCTFlags Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [Flags]
    private enum KBDLLHOOKSTRUCTFlags : uint
    {
        LLKHF_EXTENDED = 0x01,
        LLKHF_INJECTED = 0x10,
        LLKHF_ALTDOWN = 0x20,
        LLKHF_UP = 0x80,
    }

    // ReSharper restore InconsistentNaming
}