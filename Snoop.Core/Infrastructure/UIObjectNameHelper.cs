namespace Snoop.Infrastructure;

using System.Windows;
using System.Windows.Automation;

public static class UIObjectNameHelper
{
    public static string GetNameAndType(DependencyObject? dependencyObject)
    {
        if (dependencyObject is null)
        {
            return string.Empty;
        }

        return $"{GetName(dependencyObject)} ({dependencyObject.GetType().Name})";
    }

    public static string GetName(DependencyObject? dependencyObject)
    {
        var result = string.Empty;

        if (dependencyObject is FrameworkElement targetFrameworkElement)
        {
            result = targetFrameworkElement.Name;
        }

        if (string.IsNullOrEmpty(result)
            && dependencyObject is not null)
        {
            result = AutomationProperties.GetAutomationId(dependencyObject);
        }

        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        return result ?? string.Empty;
    }
}