namespace Snoop.Converters;

using System;
using System.Globalization;
using System.Windows.Data;
using Snoop.Views.DebugListenerTab;

[ValueConversion(typeof(object), typeof(object))]
[ValueConversion(typeof(FilterType), typeof(int))]
public class FilterTypeToIntConverter : IValueConverter
{
    public static readonly FilterTypeToIntConverter Default = new();

    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not FilterType)
        {
            return value;
        }

        var filterType = (FilterType)value;
        return (int)filterType;
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is not int)
        {
            return value;
        }

        var intValue = (int)value;
        return (FilterType)intValue;
    }
}