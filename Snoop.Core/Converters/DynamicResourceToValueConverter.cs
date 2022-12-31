namespace Snoop.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

internal class DynamicResourceToValueConverter : IValueConverter
{
    private readonly object target;

    public DynamicResourceToValueConverter(object target)
    {
        this.target = target;
    }

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DynamicResourceExtension == false)
        {
            return value;
        }

        var resourceKey = ((DynamicResourceExtension)value).ResourceKey;

        {
            if (this.target is FrameworkElement frameworkElement
                // ReSharper disable once PatternAlwaysOfType
                && frameworkElement.TryFindResource(resourceKey) is object foundResource)
            {
                return foundResource;
            }
        }

        {
            if (this.target is FrameworkContentElement frameworkContentElement
                // ReSharper disable once PatternAlwaysOfType
                && frameworkContentElement.TryFindResource(resourceKey) is object foundResource)
            {
                return foundResource;
            }
        }

        return value;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}