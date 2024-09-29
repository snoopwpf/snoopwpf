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
        "KeyFixer", typeof(object), typeof(DefaultStyleKeyFixer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnKeyFixerChanged));

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

        // Workaround for https://github.com/dotnet/wpf/issues/8860
        if (defaultStyleKey is Type or ResourceKey)
        {
            if (d is FrameworkElement { TemplatedParent: not null } fe)
            {
                fe.Loaded += ControlLoaded;
            }
            else if (d is FrameworkContentElement { TemplatedParent: not null } fce)
            {
                fce.Loaded += ControlLoaded;
            }
        }

        if (defaultStyleKey is null or Type or ResourceKey
            || (bool)d.GetValue(FrameworkElement.OverridesDefaultStyleProperty))
        {
            return;
        }

        var dpoType = d.GetType();
        DefaultStyleKeyHelper.SetDefaultStyleKey(d, dpoType);
    }

    // Workaround for https://github.com/dotnet/wpf/issues/8860
    private static void ControlLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DependencyObject dpo)
        {
            return;
        }

        var defaultStyleKey = DefaultStyleKeyHelper.GetDefaultStyleKey(dpo);

        if (defaultStyleKey is not Type and not ResourceKey)
        {
            return;
        }

        if (sender is FrameworkElement fe)
        {
            fe.Loaded -= ControlLoaded;

            if (fe.Style is not null
                && DependencyPropertyHelper.GetValueSource(fe, FrameworkElement.StyleProperty).BaseValueSource is BaseValueSource.ImplicitStyleReference
                && Application.Current?.TryFindResource(defaultStyleKey) == fe.Style
                && fe.TryFindResource(defaultStyleKey) is Style feStyle)
            {
                fe.Style = feStyle;
            }
        }
        else if (sender is FrameworkContentElement fce)
        {
            fce.Loaded -= ControlLoaded;

            if (fce.Style is not null
                && DependencyPropertyHelper.GetValueSource(fce, FrameworkContentElement.StyleProperty).BaseValueSource is BaseValueSource.ImplicitStyleReference
                && Application.Current?.TryFindResource(defaultStyleKey) == fce.Style
                && fce.TryFindResource(defaultStyleKey) is Style fceStyle)
            {
                fce.Style = fceStyle;
            }
        }
    }
}