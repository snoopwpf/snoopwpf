namespace Snoop.Infrastructure.Helpers;

using System.Reflection;
using System.Windows;
using System.Windows.Controls;

public static class FrameworkElementHelper
{
    private static readonly PropertyInfo frameworkElementThemeStylePropertyInfo = typeof(FrameworkElement).GetProperty("ThemeStyle", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly PropertyInfo frameworkContentElementThemeStylePropertyInfo = typeof(FrameworkContentElement).GetProperty("ThemeStyle", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static Style? GetStyle(FrameworkElement fe)
    {
        if (fe.Style is not null)
        {
            return fe.Style;
        }

        return GetThemeStyle(fe);
    }

    public static Style? GetStyle(FrameworkContentElement fce)
    {
        if (fce.Style is not null)
        {
            return fce.Style;
        }

        return GetThemeStyle(fce);
    }

    public static FrameworkTemplate? GetTemplate(FrameworkElement fe)
    {
        if (fe is Control control)
        {
            return control.Template;
        }

        return (FrameworkTemplate?)fe.GetType().GetProperty("TemplateInternal", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fe);
    }

    public static Style? GetThemeStyle(FrameworkElement fe)
    {
        return (Style?)frameworkElementThemeStylePropertyInfo.GetValue(fe);
    }

    public static Style? GetThemeStyle(FrameworkContentElement fce)
    {
        return (Style?)frameworkContentElementThemeStylePropertyInfo.GetValue(fce);
    }
}