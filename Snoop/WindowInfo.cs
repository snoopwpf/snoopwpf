namespace Snoop;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Snoop.Infrastructure;

public class WindowInfo
{
    private static readonly ConcurrentDictionary<IntPtr, WindowInfo> windowInfoCache = new();

    // we have to match "HwndWrapper[{0};{1};{2}]" which is used at https://referencesource.microsoft.com/#WindowsBase/Shared/MS/Win32/HwndWrapper.cs,2a8e13c293bb3f8c
    private static readonly Regex windowClassNameRegex = new(@"^HwndWrapper\[.*;.*;.*\]$", RegexOptions.Compiled);

    private ProcessInfo? owningProcessInfo;

    private static readonly int snoopProcessId = Process.GetCurrentProcess().Id;

    private WindowInfo(IntPtr hwnd)
    {
        this.HWnd = hwnd;
    }

    private WindowInfo(IntPtr hwnd, Process? owningProcess)
        : this(hwnd)
    {
        if (owningProcess is not null)
        {
            this.owningProcessInfo = new(owningProcess);
        }
    }

    public static WindowInfo GetWindowInfo(IntPtr hwnd, Process? owningProcess = null)
    {
        if (windowInfoCache.TryGetValue(hwnd, out var windowInfo))
        {
            return windowInfo;
        }

        windowInfo = new(hwnd, owningProcess);
        while (windowInfoCache.TryAdd(hwnd, windowInfo) == false)
        {
        }

        return windowInfo;
    }

    public static void ClearCachedWindowHandleInfo()
    {
        windowInfoCache.Clear();
    }

    private bool? isValidProcess;

    public bool IsValidProcess
    {
        get
        {
            if (this.isValidProcess is not null)
            {
                return this.isValidProcess.Value;
            }

            this.isValidProcess = false;

            try
            {
                if (this.HWnd == IntPtr.Zero)
                {
                    return this.isValidProcess.Value;
                }

                if (this.OwningProcessId == -1)
                {
                    this.isValidProcess = false;
                    return this.isValidProcess.Value;
                }

                // else determine the process validity and cache it.
                if (this.OwningProcessId == snoopProcessId)
                {
                    this.isValidProcess = false;

                    // the above line stops the user from snooping on snoop, since we assume that ... that isn't their goal.
                    // to get around this, the user can bring up two snoops and use the second snoop ... to snoop the first snoop.
                    // well, that let's you snoop the app chooser. in order to snoop the main snoop ui, you have to bring up three snoops.
                    // in this case, bring up two snoops, as before, and then bring up the third snoop, using it to snoop the first snoop.
                    // since the second snoop inserted itself into the first snoop's process, you can now spy the main snoop ui from the
                    // second snoop (bring up another main snoop ui to do so). pretty tricky, huh! and useful!
                }
                else
                {
                    // WPF-Windows have a defined class name
                    if (windowClassNameRegex.IsMatch(this.ClassName))
                    {
                        this.isValidProcess = true;
                    }

                    if (this.isValidProcess == false)
                    {
                        // a process is valid to snoop if it contains a dependency on milcore (wpfgfx).
                        // this includes the files:
                        // wpfgfx_v0300.dll (WPF 3.0/3.5 Full)
                        // wpfgrx_v0400.dll (WPF 4.0 Full)
                        // wpfgfx_cor3.dll (WPF 3.0/3.1 Core)
                        // wpfgfx_cor3.dll (WPF 5.0 Core)
                        // wpfgfx_net6.dll (WPF 6.0 Core)
                        foreach (var module in NativeMethods.GetModulesFromWindowHandle(this.HWnd))
                        {
                            if (module.szModule.StartsWith("wpfgfx_", StringComparison.OrdinalIgnoreCase))
                            {
                                this.isValidProcess = true;
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }

            return this.isValidProcess.Value;
        }
    }

    private int? owningProcessId;

    public int OwningProcessId
    {
        get
        {
            if (this.owningProcessId is null)
            {
                NativeMethods.GetWindowThreadProcessId(this.HWnd, out var processId);
                this.owningProcessId = processId;
            }

            return this.owningProcessId.Value;
        }
    }

    public ProcessInfo? OwningProcessInfo
    {
        get
        {
            if (this.owningProcessInfo is not null)
            {
                return this.owningProcessInfo;
            }

            try
            {
                var windowProcess = Process.GetProcessById(this.OwningProcessId);

                return this.owningProcessInfo = new(windowProcess);
            }
            catch
            {
                return null;
            }
        }
    }

    public IntPtr HWnd { get; }

    public string Description => $"{this.WindowTitle} - {this.OwningProcessInfo?.Process.ProcessName ?? string.Empty} [{this.OwningProcessId}]";

    #region UI Binding sources

    public string WindowTitle
    {
        get
        {
            var windowTitle = NativeMethods.GetText(this.HWnd);

            if (string.IsNullOrEmpty(windowTitle))
            {
                try
                {
                    windowTitle = this.OwningProcessInfo?.Process.MainWindowTitle;
                }
                catch (InvalidOperationException)
                {
                    // The process closed while we were trying to evaluate it
                    return string.Empty;
                }
            }

            return windowTitle ?? string.Empty;
        }
    }

    public string? ProcessName => this.OwningProcessInfo?.Process.ProcessName;

    #endregion

    public string ClassName => NativeMethods.GetClassName(this.HWnd);

    public string TraceInfo => $"{this.Description} [{this.HWnd.ToInt64():X8}] {this.ClassName}";

    public override string ToString()
    {
        return this.Description;
    }
}