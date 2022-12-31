namespace Snoop.Infrastructure.Helpers;

using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

public static class WindowHelper
{
    /// <summary>
    /// Gets the <see cref="Window"/> associated with <paramref name="hwnd"/> which belongs to <paramref name="dispatcher"/>.
    /// </summary>
    /// <returns>
    /// <c>null</c> if <paramref name="hwnd"/> or <paramref name="dispatcher"/> are <c>null</c>.
    /// </returns>
    public static Window? GetVisibleWindow(long hwnd, Dispatcher? dispatcher)
    {
        return GetVisibleWindow(new IntPtr(hwnd), dispatcher);
    }

    /// <summary>
    /// Gets the <see cref="Window"/> associated with <paramref name="hwnd"/> which belongs to <paramref name="dispatcher"/>.
    /// </summary>
    /// <returns>
    /// <c>null</c> if <paramref name="hwnd"/> or <paramref name="dispatcher"/> are <c>null</c>.
    /// </returns>
    public static Window? GetVisibleWindow(IntPtr hwnd, Dispatcher? dispatcher)
    {
        if (hwnd == IntPtr.Zero
            || dispatcher is null)
        {
            return null;
        }

        var hwndSource = HwndSource.FromHwnd(hwnd);

        if (hwndSource is not null
            && (hwndSource.Dispatcher is null || hwndSource.CheckAccess())
            && hwndSource.RootVisual is Window window
            && window.Dispatcher == dispatcher
            && window.Visibility == Visibility.Visible)
        {
            return window;
        }

        return null;
    }
}