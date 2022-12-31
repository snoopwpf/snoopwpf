// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Converters;

using System;
using System.Windows.Data;

public class CsvStringToArrayConverter : IValueConverter
{
    public static readonly CsvStringToArrayConverter Default = new();

    #region IValueConverter Members
    public object Convert(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        // value (String[]) 
        // return   string      CSV version of the string array

        if (value is null)
        {
            return string.Empty;
        }

        var val = (string[])value;
        return string.Join(",", val);
    }

    public object ConvertBack(object? value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        // value (string)       CSV version of the string array
        // return (string[])    array of strings split by ","

        if (value is null)
        {
            return new string[0];
        }

        var val = value.ToString()?.Trim() ?? string.Empty;
        return val.Split(',');
    }
    #endregion
}