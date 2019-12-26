// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    public class IsPowerShellAvailableToVisibilityConverter : IValueConverter
    {
        public static readonly IsPowerShellAvailableToVisibilityConverter DefaultInstance = new IsPowerShellAvailableToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
#if NETCOREAPP
            // PowerShell support is not currently available on .net core because loading it just fails with "Could not load file or assembly 'System.Management.Automation..."
            return Visibility.Collapsed;
#else
            return Snoop.PowerShell.ShellConstants.IsPowerShellInstalled
                ? Visibility.Visible
                : Visibility.Collapsed;
#endif
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}