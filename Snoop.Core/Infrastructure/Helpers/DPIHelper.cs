namespace Snoop.Infrastructure.Helpers;

using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

public static class DPIHelper
{
    public static Point DevicePixelsToLogical(POINT devicePoint, IntPtr hwnd)
    {
        return DevicePixelsToLogical(new Point(devicePoint.X, devicePoint.Y), hwnd);
    }

    public static Point DevicePixelsToLogical(Point devicePoint, IntPtr hwnd)
    {
        var hwndSource = HwndSource.FromHwnd(hwnd);

        if (hwndSource?.CompositionTarget is null)
        {
            return devicePoint;
        }

        return hwndSource.CompositionTarget.TransformFromDevice.Transform(devicePoint);
    }
}