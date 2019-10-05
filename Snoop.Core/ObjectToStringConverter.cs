// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Snoop
{
	public class ObjectToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new Exception("The method or operation is not implemented.");
		}

        private static string FormattedTypeName(object item)
        {
            return $"({item.GetType().Name})";
        }
    }
}
