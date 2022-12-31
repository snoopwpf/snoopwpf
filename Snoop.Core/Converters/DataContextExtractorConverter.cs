namespace Snoop.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(DependencyObject), typeof(object))]
public class DataContextExtractorConverter : IValueConverter
{
    public static readonly DataContextExtractorConverter Instance = new();

    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is DependencyObject dependencyObject)
        {
            return dependencyObject.GetValue(FrameworkElement.DataContextProperty);
        }

        return null;
    }

    public object ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        return Binding.DoNothing;
    }
}