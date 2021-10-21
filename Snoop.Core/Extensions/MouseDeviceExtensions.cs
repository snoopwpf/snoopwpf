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

        public static UIElement? GetDirectlyOver(this MouseDevice mouseDevice)
        {
            return GetElementAtMousePos(mouseDevice.Dispatcher)
                   ?? rawDirectlyOverPropertyInfo?.GetValue(mouseDevice, null) as UIElement
                   ?? mouseDevice.DirectlyOver as UIElement;
        }

        private static UIElement? GetElementAtMousePos(Dispatcher dispatcher)
        {
            var windowHandleUnderMouse = NativeMethods.GetWindowUnderMouse();
            var windowUnderMouse = WindowHelper.GetVisibleWindow(windowHandleUnderMouse, dispatcher);

            UIElement? directlyOverElement = null;

            if (windowUnderMouse is not null)
            {
                VisualTreeHelper.HitTest(windowUnderMouse, FilterCallback, r => ResultCallback(r, ref directlyOverElement), new PointHitTestParameters(Mouse.GetPosition(windowUnderMouse)));
            }

            return directlyOverElement;
        }

        private static HitTestFilterBehavior FilterCallback(DependencyObject target)
        {
            return target switch
            {
                UIElement { IsVisible: false } => HitTestFilterBehavior.ContinueSkipSelfAndChildren,
                UIElement uiElement when uiElement.IsPartOfSnoopVisualTree() => HitTestFilterBehavior.ContinueSkipSelfAndChildren,
                _ => HitTestFilterBehavior.Continue
            };
        }

        private static HitTestResultBehavior ResultCallback(HitTestResult? result, ref UIElement? directlyOverElement)
        {
            if (result?.VisualHit is not UIElement uiElement)
            {
                return HitTestResultBehavior.Continue;
            }

            directlyOverElement = uiElement;
            return HitTestResultBehavior.Stop;
        }
    }
}