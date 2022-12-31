namespace Snoop.Infrastructure;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class SystemIcon
{
    public const string ImageResDll = @"%SystemRoot%\system32\imageres.dll";

    public SystemIcon(string origin, int iconIdOrIndex)
    {
        this.Origin = origin;
        this.IconIdOrIndex = iconIdOrIndex;
    }

    public string Origin { get; }

    // Negative values reflect a resource id
    public int IconIdOrIndex { get; }

    public static readonly SystemIcon Shield = new(ImageResDll, -78);

    public static readonly SystemIcon Settings = new(ImageResDll, -114);
}

public static class SystemIconHelper
{
    [DllImport("shell32.dll")]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int DestroyIcon(IntPtr hIcon);

    public static ImageSource? GetImageSource(SystemIcon systemIcon, int width, int height)
    {
        return GetImageSource(systemIcon.Origin, systemIcon.IconIdOrIndex, width, height);
    }

    private static ImageSource? GetImageSource(string filePath, int iconIdOrIndex, int width, int height)
    {
        var iconHandle = ExtractIcon(IntPtr.Zero, filePath, iconIdOrIndex);

        if (iconHandle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            return Imaging.CreateBitmapSourceFromHIcon(iconHandle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(width, height));
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }
}