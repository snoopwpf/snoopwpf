// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

public class ObjectToStringConverter : IValueConverter
{
    public static readonly ObjectToStringConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return this.Convert(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public string Convert(object value)
    {
        switch (value)
        {
            case null:
                return "{null}";

            case FrameworkElement item
                when string.IsNullOrEmpty(item.Name) == false:
                return $"{item.Name} {FormattedTypeName(item)}";

            case RoutedCommand item
                when string.IsNullOrEmpty(item.Name) == false:
                return $"{item.Name} {FormattedTypeName(item)}";

            default:
                return FormattedTypeName(value);
        }
    }

    private static string FormattedTypeName(object item)
    {
        return $"({item.GetType().Name})";
    }
}