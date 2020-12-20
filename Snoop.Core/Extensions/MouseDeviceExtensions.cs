// ReSharper disable once CheckNamespace
namespace Snoop
{
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;
    using Snoop.Infrastructure;
    using Snoop.Infrastructure.Helpers;

    public static class MouseDeviceExtensions
    {
        private static readonly PropertyInfo? rawDirectlyOverPropertyInfo = typeof(MouseDevice).GetProperty("RawDirectlyOver", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        public static IInputElement GetDirectlyOver(this MouseDevice mouseDevice)
        {
            return GetElementAtMousePos(mouseDevice.Dispatcher)
                   ?? rawDirectlyOverPropertyInfo?.GetValue(mouseDevice, null) as IInputElement
                   ?? mouseDevice.DirectlyOver;
        }

        private static FrameworkElement? GetElementAtMousePos(Dispatcher dispatcher)
        {
            var windowHandleUnderMouse = NativeMethods.GetWindowUnderMouse();
            var windowUnderMouse = WindowHelper.GetVisibleWindow(windowHandleUnderMouse, dispatcher);

            FrameworkElement? directlyOverElement = null;

            if (windowUnderMouse is not null)
            {
                VisualTreeHelper.HitTest(windowUnderMouse, FilterCallback, r => ResultCallback(r, ref directlyOverElement), new PointHitTestParameters(Mouse.GetPosition(windowUnderMouse)));
            }

            return directlyOverElement;
        }

        private static HitTestFilterBehavior FilterCallback(DependencyObject target)
        {
            return HitTestFilterBehavior.Continue;
        }

        private static HitTestResultBehavior ResultCallback(HitTestResult? result, ref FrameworkElement? directlyOverElement)
        {
            if (result is not null
                && result.VisualHit is FrameworkElement frameworkElement
                && frameworkElement.IsVisible
                && frameworkElement.IsPartOfSnoopVisualTree() == false)
            {
                directlyOverElement = frameworkElement;
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }
    }
}