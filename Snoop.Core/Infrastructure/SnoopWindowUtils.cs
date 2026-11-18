// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using global::Windows.Win32;
using global::Windows.Win32.Graphics.Gdi;
using Snoop.Data;
using Snoop.Infrastructure.Helpers;
using Application = System.Windows.Application;
using Rectangle = System.Drawing.Rectangle;

public static class SnoopWindowUtils
{
    public static Window? FindOwnerWindow(Window ownedWindow)
    {
        var ownerWindow = TransientSettingsData.Current is not null
            ? WindowHelper.GetVisibleWindow(TransientSettingsData.Current.TargetWindowHandle, ownedWindow.Dispatcher)
            : null;

        if (ownerWindow is null
            && SnoopModes.MultipleDispatcherMode)
        {
            foreach (PresentationSource? presentationSource in PresentationSource.CurrentSources)
            {
                if (presentationSource is null)
                {
                    continue;
                }

                if (presentationSource.CheckAccess()
                    && presentationSource.RootVisual is Window window
                    && window.CheckAccess()
                    && window.Visibility == Visibility.Visible)
                {
                    ownerWindow = window;
                    break;
                }
            }
        }
        else if (ownerWindow is null
                 && Application.Current is not null
                 && Application.Current.CheckAccess())
        {
            if (Application.Current.MainWindow is not null
                && Application.Current.MainWindow.CheckAccess()
                && Application.Current.MainWindow.Visibility == Visibility.Visible)
            {
                // first: set the owner window as the current application's main window, if visible.
                ownerWindow = Application.Current.MainWindow;
            }
            else
            {
                // second: try and find a visible window in the list of the current application's windows
                foreach (Window? window in Application.Current.Windows)
                {
                    if (window is null)
                    {
                        continue;
                    }

                    if (window.CheckAccess()
                        && window.Visibility == Visibility.Visible)
                    {
                        ownerWindow = window;
                        break;
                    }
                }
            }
        }

        if (ownerWindow is null)
        {
            // third: try and find a visible window in the list of current presentation sources
            foreach (PresentationSource? presentationSource in PresentationSource.CurrentSources)
            {
                if (presentationSource is null)
                {
                    continue;
                }

                if (presentationSource.CheckAccess()
                    && presentationSource.RootVisual is Window window
                    && window.CheckAccess()
                    && window.Visibility == Visibility.Visible)
                {
                    ownerWindow = window;
                    break;
                }
            }
        }

        if (ReferenceEquals(ownerWindow, ownedWindow))
        {
            return null;
        }

        if (ownerWindow is not null
            && ownerWindow.Dispatcher != ownedWindow.Dispatcher)
        {
            return null;
        }

        return ownerWindow;
    }

    public static void LoadWindowPlacement(Window window, WINDOWPLACEMENT? windowPlacement)
    {
        if (windowPlacement.HasValue == false)
        {
            return;
        }

        var windowPlacementValue = windowPlacement.Value;

        try
        {
            if (windowPlacementValue.NormalPosition.Width is not 0
                     && windowPlacementValue.NormalPosition.Height is not 0
                     && IsVisibleOnAnyScreen(windowPlacementValue.NormalPosition, out var screenBounds))
            {
                var screenContainsPosition = screenBounds.Contains(windowPlacement.Value.NormalPosition.Left, windowPlacement.Value.NormalPosition.Top);
                var hwnd = new WindowInteropHelper(window).Handle;
                var logicalScreenPosition = DPIHelper.DevicePixelsToLogical(new Point(windowPlacement.Value.NormalPosition.Left, windowPlacement.Value.NormalPosition.Top), hwnd);
                window.Top = screenContainsPosition ? logicalScreenPosition.Y : screenBounds.Top;
                window.Left = screenContainsPosition ? logicalScreenPosition.X : screenBounds.Left;
                var logicalWindowSize = DPIHelper.DevicePixelsToLogical(new Point(windowPlacement.Value.NormalPosition.Width, windowPlacement.Value.NormalPosition.Height), hwnd);
                var logicalScreenSize = DPIHelper.DevicePixelsToLogical(new Point(screenBounds.Width, screenBounds.Height), hwnd);
                window.Width = Math.Max(100, Math.Min(logicalScreenSize.X, logicalWindowSize.X));
                window.Height = Math.Max(26, Math.Min(logicalScreenSize.Y, logicalWindowSize.Y));
            }

            if (windowPlacementValue.ShowCmd is NativeMethods.SW_SHOWMAXIMIZED)
            {
                window.WindowState = WindowState.Maximized;
            }
        }
        catch (Exception exception)
        {
            LogHelper.WriteWarning(exception);
        }
    }

    public static void SaveWindowPlacement(Window window, Action<WINDOWPLACEMENT> saveAction)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        NativeMethods.GetWindowPlacement(hwnd, out var windowPlacement);

        saveAction(windowPlacement);
    }

    private static bool IsVisibleOnAnyScreen(RECT rect, out Rectangle screenBounds)
    {
        screenBounds = Rectangle.Empty;

        var rectangle = new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);

        var apiRect = new global::Windows.Win32.Foundation.RECT(rectangle);

        var monitor = PInvoke.MonitorFromRect(apiRect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONULL);
        if (monitor == HMONITOR.Null)
        {
            return false;
        }

        var monitorInfo = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (PInvoke.GetMonitorInfo(monitor, ref monitorInfo) == false)
        {
            return false;
        }

        screenBounds = monitorInfo.rcMonitor;
        return true;
    }
}