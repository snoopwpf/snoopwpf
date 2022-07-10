namespace Snoop.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

public class EqualsToVisibilityConverter : IValueConverter
{
    public static readonly EqualsToVisibilityConverter DefaultInstance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}