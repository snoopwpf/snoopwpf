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
        var defaultStyleKey = FrameworkElementDefaultStyleKeyHelper.GetDefaultStyleKey(fe) ?? fe.GetType();
        return fe.TryFindResource(defaultStyleKey) as Style;
    }

    public static Style? GetStyle(FrameworkContentElement fce)
    {
        var defaultStyleKey = FrameworkContentElementDefaultStyleKeyHelper.GetDefaultStyleKey(fce) ?? fce.GetType();
        return fce.TryFindResource(defaultStyleKey) as Style;
    }

    public static FrameworkTemplate? GetTemplate(FrameworkElement fe)
    {
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

#pragma warning disable CA1812

    private class FrameworkElementDefaultStyleKeyHelper : FrameworkElement
    {
        public static object? GetDefaultStyleKey(FrameworkElement element) => element.GetValue(DefaultStyleKeyProperty);
    }

    private class FrameworkContentElementDefaultStyleKeyHelper : FrameworkElement
    {
        public static object? GetDefaultStyleKey(FrameworkContentElement element) => element.GetValue(DefaultStyleKeyProperty);
    }

#pragma warning restore CA1812
}