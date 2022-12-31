// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure;

using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public static class VisualCaptureUtil
{
    private const double BaseDpi = 96;

    public static void SaveVisual(Visual? visual, int dpi, string filename)
    {
        // sometimes RenderTargetBitmap doesn't render the Visual or doesn't render the Visual properly
        // below i am using the trick that jamie rodriguez posted on his blog
        // where he wraps the Visual inside of a VisualBrush and then renders it.
        // http://blogs.msdn.com/b/jaimer/archive/2009/07/03/rendertargetbitmap-tips.aspx

        var visualBrush = CreateVisualBrushSafe(visual);

        if (visual is null
            || visualBrush is null)
        {
            return;
        }

        var renderTargetBitmap = RenderVisualWithHighQuality(visual, dpi, dpi);

        SaveAsPng(renderTargetBitmap, filename);
    }

    public static VisualBrush? CreateVisualBrushSafe(Visual? visual)
    {
        return IsSafeToVisualize(visual)
            ? new VisualBrush(visual)
            : null;
    }

    public static bool IsSafeToVisualize(Visual? visual)
    {
        if (visual is null)
        {
            return false;
        }

        if (visual is Window)
        {
            var source = PresentationSource.FromVisual(visual) as HwndSource;
            return source?.CompositionTarget is not null;
        }

        return true;
    }

    private static void SaveAsPng(RenderTargetBitmap bitmap, string filename)
    {
        var pngBitmapEncoder = new PngBitmapEncoder();
        pngBitmapEncoder.Frames.Add(BitmapFrame.Create(bitmap));

        using (var fileStream = File.Create(filename))
        {
            pngBitmapEncoder.Save(fileStream);
        }
    }

    /// <summary>
    /// Draws <paramref name="visual"/> in smaller tiles using multiple <see cref="VisualBrush"/>.
    /// </summary>
    /// <remarks>
    /// This way we workaround a limitation in <see cref="VisualBrush"/> which causes poor quality for larger visuals.
    /// </remarks>
    public static RenderTargetBitmap RenderVisualWithHighQuality(Visual visual, int dpiX, int dpiY, PixelFormat? pixelFormat = null, Viewport3D? viewport3D = null)
    {
        var size = GetSize(visual);

        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            DrawVisualInTiles(visual, drawingContext, size);
        }

        var renderTargetBitmap = RenderVisual(drawingVisual, size, dpiX, dpiY, pixelFormat, viewport3D);
        return renderTargetBitmap;
    }

    public static RenderTargetBitmap RenderVisual(Visual visual, Size bounds, int dpiX, int dpiY, PixelFormat? pixelFormat = null, Viewport3D? viewport3D = null)
    {
        var scaleX = dpiX / BaseDpi;
        var scaleY = dpiY / BaseDpi;

        pixelFormat ??= PixelFormats.Pbgra32;

        var renderTargetBitmap = new RenderTargetBitmap((int)Math.Ceiling(scaleX * bounds.Width), (int)Math.Ceiling(scaleY * bounds.Height), dpiX, dpiY, pixelFormat.Value);

        if (viewport3D is not null)
        {
            typeof(RenderTargetBitmap)
                .GetMethod("RenderForBitmapEffect", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(renderTargetBitmap, new object[] { visual, Matrix.Identity, Rect.Empty });
        }
        else
        {
            renderTargetBitmap.Render(visual);
        }

        return renderTargetBitmap;
    }

    private static Size GetSize(Visual visual)
    {
        if (visual is UIElement uiElement)
        {
            return uiElement.RenderSize;
        }

        var descendantBounds = VisualTreeHelper.GetDescendantBounds(visual);
        return new Size(descendantBounds.Width, descendantBounds.Height);
    }

    /// <summary>
    /// Draws <paramref name="visual"/> in smaller tiles using multiple <see cref="VisualBrush"/> to <paramref name="drawingContext"/>.
    /// This way we workaround a limitation in <see cref="VisualBrush"/> which causes poor quality for larger visuals.
    /// </summary>
    /// <param name="visual">The visual to be drawn.</param>
    /// <param name="drawingContext">The <see cref="DrawingContext"/> to use.</param>
    /// <param name="visualSize">The size of <paramref name="visual"/>.</param>
    /// <param name="tileWidth">The width of one tile.</param>
    /// <param name="tileHeight">The height of one tile.</param>
    /// <remarks>
    /// Original version of this method was copied from https://srndolha.wordpress.com/2012/10/16/exported-drawingvisual-quality-when-using-visualbrush/
    ///
    /// A tile size of 32x32 turned out deliver the best quality while not increasing computation time too much.
    /// </remarks>
    private static void DrawVisualInTiles(Visual visual, DrawingContext drawingContext, Size visualSize, double tileWidth = 32, double tileHeight = 32)
    {
        var visualWidth = visualSize.Width;
        var visualHeight = visualSize.Height;

        var verticalTileCount = visualHeight / tileHeight;
        var horizontalTileCount = visualWidth / tileWidth;

        for (var i = 0; i <= verticalTileCount; i++)
        {
            for (var j = 0; j <= horizontalTileCount; j++)
            {
                var width = tileWidth;
                var height = tileHeight;

                // Check if we would exceed the width of the visual and limit it by the remaining
                if ((j + 1) * tileWidth > visualWidth)
                {
                    width = visualWidth - (j * tileWidth);
                }

                // Check if we would exceed the height of the visual and limit it by the remaining
                if ((i + 1) * tileHeight > visualHeight)
                {
                    height = visualHeight - (i * tileHeight);
                }

                var x = j * tileWidth;
                var y = i * tileHeight;

                var rectangle = new Rect(x, y, width, height);

                var contentBrush = new VisualBrush(visual)
                {
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top,
                    Viewbox = rectangle,
                    ViewboxUnits = BrushMappingMode.Absolute
                };

                drawingContext.DrawRectangle(contentBrush, null, rectangle);
            }
        }
    }
}