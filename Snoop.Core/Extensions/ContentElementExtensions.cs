namespace Snoop;

using System;
using System.Reflection;
using System.Windows;

public static class ContentElementExtensions
{
    private static readonly PropertyInfo? parentPropertyInfo = typeof(ContentElement).GetProperty("Parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private static readonly MethodInfo? getUIParentMethodInfo = typeof(ContentElement).GetMethod("GetUIParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

    public static DependencyObject? GetParent(this ContentElement contentElement)
    {
        return parentPropertyInfo?.GetValue(contentElement, null) as DependencyObject;
    }

    public static DependencyObject? GetUIParent(this ContentElement contentElement)
    {
        return getUIParentMethodInfo?.Invoke(contentElement, null) as DependencyObject;
    }
}