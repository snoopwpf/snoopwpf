namespace Snoop.Converters;

using System;
using System.Globalization;
using System.Windows.Data;

public class IsNullOrEmptyStringConverter : IValueConverter
{
    public static readonly IsNullOrEmptyStringConverter DefaultInstance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return string.IsNullOrEmpty(stringValue);
        }

        return value is null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}