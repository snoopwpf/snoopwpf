// ReSharper disable once CheckNamespace
namespace Snoop;

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
        if (TryGetElementAtMousePos(mouseDevice.Dispatcher, out var elementFromFilter, out var elementFromResult))
        {
            return elementFromFilter
                   ?? elementFromResult;
        }

        var elementMouseRawDirectlyOver = rawDirectlyOverPropertyInfo?.GetValue(mouseDevice, null) as UIElement;
        var elementMouseDeviceDirectlyOver = mouseDevice.DirectlyOver as UIElement;

        return elementMouseRawDirectlyOver
               ?? elementMouseDeviceDirectlyOver;
    }

    private static bool TryGetElementAtMousePos(Dispatcher dispatcher, out UIElement? elementFromFilter, out UIElement? elementFromResult)
    {
        elementFromFilter = null;
        elementFromResult = null;

        var windowHandleUnderMouse = NativeMethods.GetWindowUnderMouse();
        var windowUnderMouse = WindowHelper.GetVisibleWindow(windowHandleUnderMouse, dispatcher);

        if (windowUnderMouse is null)
        {
            return false;
        }

        var mousePosition = NativeMethods.TryGetRelativeMousePosition(windowHandleUnderMouse, out var nativeMousePosition)
            ? DPIHelper.DevicePixelsToLogical(nativeMousePosition, windowHandleUnderMouse)
            : Mouse.GetPosition(windowUnderMouse);

        var pointHitTestParameters = new PointHitTestParameters(mousePosition);

        UIElement? elementFromFilterLocal = null;
        UIElement? elementFromResultLocal = null;
        VisualTreeHelper.HitTest(windowUnderMouse, o => FilterCallback(o, ref elementFromFilterLocal), r => ResultCallback(r, ref elementFromResultLocal), pointHitTestParameters);

        elementFromFilter = elementFromFilterLocal;
        elementFromResult = elementFromResultLocal;

        return elementFromFilter is not null
               || elementFromResult is not null;
    }

    private static HitTestFilterBehavior FilterCallback(DependencyObject target, ref UIElement? element)
    {
        var filterResult = target switch
        {
            UIElement { IsVisible: false } => HitTestFilterBehavior.ContinueSkipSelfAndChildren,
            UIElement uiElement when uiElement.IsPartOfSnoopVisualTree() => HitTestFilterBehavior.ContinueSkipSelfAndChildren,
            _ => HitTestFilterBehavior.Continue
        };

        if (filterResult == HitTestFilterBehavior.Continue)
        {
            if (target is UIElement uiElement)
            {
                element = uiElement;
            }
        }

        return filterResult;
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