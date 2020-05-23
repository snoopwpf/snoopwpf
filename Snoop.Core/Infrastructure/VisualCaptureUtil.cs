// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Infrastructure
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using JetBrains.Annotations;

    public class VisualCaptureUtil
    {
        public static void SaveVisual(Visual visual, int dpi, string filename)
        {
            // sometimes RenderTargetBitmap doesn't render the Visual or doesn't render the Visual properly
            // below i am using the trick that jamie rodriguez posted on his blog
            // where he wraps the Visual inside of a VisualBrush and then renders it.
            // http://blogs.msdn.com/b/jaimer/archive/2009/07/03/rendertargetbitmap-tips.aspx

            if (visual == null || !IsSafeToVisualize(visual))
            {
                return;
            }

            Rect bounds;
            var uiElement = visual as UIElement;
            if (uiElement != null)
            {
                bounds = new Rect(new Size((int)uiElement.RenderSize.Width, (int)uiElement.RenderSize.Height));
            }
            else
            {
                bounds = VisualTreeHelper.GetDescendantBounds(visual);
            }

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
            {
                var vb = new VisualBrush(visual);
                ctx.DrawRectangle(vb, null, new Rect(default, bounds.Size));
            }

            var rtb = RenderVisualToRenderTargetBitmap(dv, bounds, dpi, PixelFormats.Pbgra32);
            rtb.Render(dv);

            SaveRTBAsPNG(rtb, filename);
        }

        [CanBeNull]
        public static VisualBrush CreateVisualBrushSafe(Visual visual)
        {
            return IsSafeToVisualize(visual) ? new VisualBrush(visual) : null;
        }

        public static bool IsSafeToVisualize(Visual visual)
        {
            if (visual is null)
            {
                return false;
            }

            if (visual is Window)
            {
                var source = PresentationSource.FromVisual(visual) as HwndSource;
                return source?.CompositionTarget != null;
            }

            return true;
        }

        private static void SaveRTBAsPNG(RenderTargetBitmap bitmap, string filename)
        {
            var pngBitmapEncoder = new PngBitmapEncoder();
            pngBitmapEncoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var fileStream = System.IO.File.Create(filename))
            {
                pngBitmapEncoder.Save(fileStream);
            }
        }

        private const double BaseDpi = 96;

        public static RenderTargetBitmap RenderVisualToRenderTargetBitmap(Visual visual, Rect bounds, int dpi, PixelFormat pixelFormat, Viewport3D viewport3D = null)
        {
            return RenderVisualToRenderTargetBitmap(visual, new Size(bounds.Width, bounds.Height), dpi, pixelFormat, viewport3D);
        }

        public static RenderTargetBitmap RenderVisualToRenderTargetBitmap(Visual visual, Size bounds, int dpi, PixelFormat pixelFormat, Viewport3D viewport3D = null)
        {
            var scale = dpi / BaseDpi;

            var renderTargetBitmap = new RenderTargetBitmap((int)Math.Ceiling(scale * bounds.Width), (int)Math.Ceiling(scale * bounds.Height), scale * BaseDpi, scale * BaseDpi, pixelFormat);
            if (viewport3D != null)
            {
                typeof(RenderTargetBitmap)
                    .GetMethod("RenderForBitmapEffect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(renderTargetBitmap, new object[] { visual, Matrix.Identity, Rect.Empty });
            }
            else
            {
                renderTargetBitmap.Render(visual);
            }

            return renderTargetBitmap;
        }
    }
}
