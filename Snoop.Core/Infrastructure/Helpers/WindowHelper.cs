namespace Snoop.Infrastructure.Helpers
{
    using System;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Threading;

    public static class WindowHelper
    {
        public static Window GetVisibleWindow(long hwnd, Dispatcher dispatcher = null)
        {
            return GetVisibleWindow(new IntPtr(hwnd), dispatcher);
        }

        public static Window GetVisibleWindow(IntPtr hwnd, Dispatcher dispatcher = null)
        {
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }

            var hwndSource = HwndSource.FromHwnd(hwnd);
            if (hwndSource != null
                && hwndSource.RootVisual is Window window
                && window.Visibility == Visibility.Visible
                && (dispatcher == null || window.Dispatcher == dispatcher))
            {
                return window;
            }

            return null;
        }
    }
}