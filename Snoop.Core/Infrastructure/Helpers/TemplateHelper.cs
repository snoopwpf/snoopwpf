namespace Snoop.Infrastructure.Helpers;

using System.Windows;
using System.Windows.Controls;

public static class TemplateHelper
{
    public static object? GetChildFromTemplateIfNeeded(DependencyObject element, string? templatePartName)
    {
        if (string.IsNullOrEmpty(templatePartName))
        {
            return element;
        }

        if (element is Control { Template: { } } control)
        {
            return control.Template.FindName(templatePartName, control);
        }

        if (element is FrameworkElement fe)
        {
            return fe.FindName(templatePartName);
        }

        if (element is FrameworkContentElement fec)
        {
            return fec.FindName(templatePartName);
        }

        return null;
    }
}