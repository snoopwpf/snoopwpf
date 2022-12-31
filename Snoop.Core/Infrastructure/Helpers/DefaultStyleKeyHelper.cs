namespace Snoop.Infrastructure.Helpers;

using System;
using System.Windows;

public class DefaultStyleKeyHelper : FrameworkElement
{
    public static object? GetDefaultStyleKey(DependencyObject element) => element.GetValue(DefaultStyleKeyProperty);

    public static void SetDefaultStyleKey(DependencyObject element, object? defaultStyleKey) => element.SetValue(DefaultStyleKeyProperty, defaultStyleKey);
}

#pragma warning disable WPF0013
#pragma warning disable WPF0016
public class DefaultStyleKeyFixer : DependencyObject
{
    public static readonly DependencyProperty KeyFixerProperty = DependencyProperty.RegisterAttached(
        "KeyFixer", typeof(object), typeof(DefaultStyleKeyFixer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits, OnKeyFixerChanged));

    public static void SetKeyFixer(DependencyObject element, object? value)
    {
        element.SetValue(KeyFixerProperty, value);
    }

    public static object? GetKeyFixer(DependencyObject element)
    {
        return (object?)element.GetValue(KeyFixerProperty);
    }

    private static void OnKeyFixerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var defaultStyleKey = DefaultStyleKeyHelper.GetDefaultStyleKey(d);

        if (defaultStyleKey is null or Type or ResourceKey
            || (bool)d.GetValue(FrameworkElement.OverridesDefaultStyleProperty) == true)
        {
            return;
        }

        var dpoType = d.GetType();
        DefaultStyleKeyHelper.SetDefaultStyleKey(d, dpoType);
    }
}