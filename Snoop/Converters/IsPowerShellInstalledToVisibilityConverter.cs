﻿// (c) Copyright Bailey Ling.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Microsoft.Win32;

namespace Snoop.Converters
{
    public class IsPowerShellInstalledToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine");
            if (key != null)
            {
                object keyValue = key.GetValue("PowerShellVersion");
                if ("2.0".Equals(keyValue))
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}