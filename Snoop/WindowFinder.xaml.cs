// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop;

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Snoop.Infrastructure;
using Snoop.Windows;

public enum WindowFinderType
{
    Snoop,
    Magnify
}

public partial class WindowFinder
{
    private static readonly Point cursorHotSpot = new(16, 20);
    private Cursor? crosshairsCursor;
    private Point startPoint;
    private WindowInfo? currentWindowInfo;
    private readonly LowLevelMouseHook lowLevelMouseHook;

    private Cursor? currentWindowInfoCursor;
    private Cursor? lastWindowInfoCursor;

    public WindowFinder()
    {
        this.InitializeComponent();

        this.Loaded += (_, _) => this.crosshairsCursor = this.ConvertToCursor(this.WindowInfoControl, cursorHotSpot);

        this.lowLevelMouseHook = new LowLevelMouseHook();
        this.lowLevelMouseHook.LowLevelMouseMove += this.LowLevelMouseMove;
    }

    public WindowFinderType WindowFinderType { get; set; }

    public WindowInfoControl WindowInfoControl { get; } = new();

    /// <inheritdoc />
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        this.startPoint = e.GetPosition(null);
        this.StartSnoopTargetsSearch();
        e.Handled = true;

        base.OnPreviewMouseLeftButtonDown(e);
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var currentPosition = e.GetPosition(null);
        var diff = this.startPoint - currentPosition;

        if (e.LeftButton == MouseButtonState.Pressed
            && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            this.lowLevelMouseHook.Start();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        var windowInfoToUse = this.currentWindowInfo;

        this.StopSnoopTargetsSearch();

        if (windowInfoToUse is null
            || windowInfoToUse.IsValidProcess == false)
        {
            return;
        }

        switch (this.WindowFinderType)
        {
            case WindowFinderType.Snoop:
                AttachSnoop(windowInfoToUse);
                break;

            case WindowFinderType.Magnify:
                AttachMagnify(windowInfoToUse);
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.StopSnoopTargetsSearch();
        }

        base.OnPreviewKeyDown(e);
    }

    private void LowLevelMouseMove(object sender, LowLevelMouseHook.LowLevelMouseMoveEventArgs e)
    {
        var windowUnderCursor = NativeMethods.GetWindowUnderMouse();

        // the window under the cursor has changed
        if (windowUnderCursor != this.currentWindowInfo?.HWnd)
        {
            this.currentWindowInfo = WindowInfo.GetWindowInfo(windowUnderCursor);
            this.WindowInfoControl.DataContext = this.currentWindowInfo;

            LogHelper.WriteLine($"Window under cursor: {this.currentWindowInfo.TraceInfo}");

            this.UpdateCursor();
        }
    }

    private void StartSnoopTargetsSearch()
    {
        this.CaptureMouse();
        Keyboard.Focus(this.btnStartWindowsSearch);

        this.snoopCrosshairsImage.Visibility = Visibility.Hidden;
        this.currentWindowInfo = null;

        this.lowLevelMouseHook.Start();

        this.UpdateCursor();
    }

    private void StopSnoopTargetsSearch()
    {
        this.lowLevelMouseHook.Stop();

        this.ReleaseMouseCapture();

        this.currentWindowInfo = null;

        this.snoopCrosshairsImage.Visibility = Visibility.Visible;

        // clear out cached process info to make the force refresh do the process check over again.
        WindowInfo.ClearCachedWindowHandleInfo();

        this.UpdateCursor();
    }

    private void UpdateCursor()
    {
        if (this.currentWindowInfo?.IsValidProcess == true)
        {
            this.lastWindowInfoCursor = this.currentWindowInfoCursor;

            this.currentWindowInfoCursor = this.ConvertToCursor(this.WindowInfoControl, cursorHotSpot);
            this.Cursor = this.currentWindowInfoCursor;
        }
        else if (this.lowLevelMouseHook.IsRunning)
        {
            this.Cursor = this.crosshairsCursor;
        }
        else
        {
            this.Cursor = null;
        }

        this.lastWindowInfoCursor?.Dispose();
        this.lastWindowInfoCursor = null;
    }

    public static void AttachSnoop(WindowInfo windowInfo)
    {
        var result = windowInfo.OwningProcessInfo?.Snoop(windowInfo.HWnd);

        if (result?.Success == false)
        {
            ErrorDialog.ShowDialog(result.AttachException, "Can't Snoop the process", $"Failed to attach to '{windowInfo.Description}'.", true);
        }
    }

    private static void AttachMagnify(WindowInfo windowInfo)
    {
        var result = windowInfo.OwningProcessInfo?.Magnify(windowInfo.HWnd);

        if (result?.Success == false)
        {
            ErrorDialog.ShowDialog(result.AttachException, "Can't Snoop the process", $"Failed to attach to '{windowInfo.Description}'.", true);
        }
    }

    // https://stackoverflow.com/a/27077188/122048
    private Cursor ConvertToCursor(UIElement control, Point hotSpot = default)
    {
        // convert FrameworkElement to PNG stream
        using (var pngStream = new MemoryStream())
        {
            control.InvalidateMeasure();
            control.InvalidateArrange();
            control.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var rect = new Rect(0, 0, control.DesiredSize.Width, control.DesiredSize.Height);

            control.Arrange(rect);
            control.UpdateLayout();

            var dpiScale = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            var rtb = VisualCaptureUtil.RenderVisual(control, new Size(control.DesiredSize.Width, control.DesiredSize.Height), (int)Math.Ceiling(dpiScale.M11 * 96), (int)Math.Ceiling(dpiScale.M22 * 96));

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            encoder.Save(pngStream);

            // write cursor header info
            using (var cursorStream = new MemoryStream())
            {
                cursorStream.Write(new byte[2]
                {
                    0x00,
                    0x00
                }, 0, 2); // ICONDIR: Reserved. Must always be 0.
                cursorStream.Write(new byte[2]
                {
                    0x02,
                    0x00
                }, 0, 2); // ICONDIR: Specifies image type: 1 for icon (.ICO) image, 2 for cursor (.CUR) image. Other values are invalid
                cursorStream.Write(new byte[2]
                {
                    0x01,
                    0x00
                }, 0, 2); // ICONDIR: Specifies number of images in the file.
                cursorStream.Write(new byte[1]
                {
                    (byte)control.DesiredSize.Width
                }, 0, 1); // ICONDIRENTRY: Specifies image width in pixels. Can be any number between 0 and 255. Value 0 means image width is 256 pixels.
                cursorStream.Write(new byte[1]
                {
                    (byte)control.DesiredSize.Height
                }, 0, 1); // ICONDIRENTRY: Specifies image height in pixels. Can be any number between 0 and 255. Value 0 means image height is 256 pixels.
                cursorStream.Write(new byte[1]
                {
                    0x00
                }, 0, 1); // ICONDIRENTRY: Specifies number of colors in the color palette. Should be 0 if the image does not use a color palette.
                cursorStream.Write(new byte[1]
                {
                    0x00
                }, 0, 1); // ICONDIRENTRY: Reserved. Should be 0.
                cursorStream.Write(new byte[2]
                {
                    (byte)hotSpot.X,
                    0x00
                }, 0, 2); // ICONDIRENTRY: Specifies the horizontal coordinates of the hotspot in number of pixels from the left.
                cursorStream.Write(new byte[2]
                {
                    (byte)hotSpot.Y,
                    0x00
                }, 0, 2); // ICONDIRENTRY: Specifies the vertical coordinates of the hotspot in number of pixels from the top.
                cursorStream.Write(new byte[4]
                {
                    // ICONDIRENTRY: Specifies the size of the image's data in bytes
                    (byte)(pngStream.Length & 0x000000FF),
                    (byte)((pngStream.Length & 0x0000FF00) >> 0x08),
                    (byte)((pngStream.Length & 0x00FF0000) >> 0x10),
                    (byte)((pngStream.Length & 0xFF000000) >> 0x18)
                }, 0, 4);
                cursorStream.Write(new byte[4]
                {
                    // ICONDIRENTRY: Specifies the offset of BMP or PNG data from the beginning of the ICO/CUR file
                    0x16,
                    0x00,
                    0x00,
                    0x00
                }, 0, 4);

                // copy PNG stream to cursor stream
                pngStream.Seek(0, SeekOrigin.Begin);
                pngStream.CopyTo(cursorStream);

                // return cursor stream
                cursorStream.Seek(0, SeekOrigin.Begin);
                return new Cursor(cursorStream);
            }
        }
    }
}