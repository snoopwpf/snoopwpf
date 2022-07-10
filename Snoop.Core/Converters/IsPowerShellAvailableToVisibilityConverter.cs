// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

public class IsPowerShellAvailableToVisibilityConverter : IValueConverter
{
    public static readonly IsPowerShellAvailableToVisibilityConverter DefaultInstance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Snoop.PowerShell.ShellConstants.IsPowerShellInstalled
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}