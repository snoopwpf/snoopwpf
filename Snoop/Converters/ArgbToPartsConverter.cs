// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Snoop.Converters
{
	public class ArgbToPartsConverter : IValueConverter
	{
		public static readonly ArgbToPartsConverter Default = new ArgbToPartsConverter();

		#region IValueConverter Members
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// value (string)	ARGB text (i.e. #FF102030)
			// parameter (int)	which "part" to return (0 = alpha, 1 = Red, 2 = Green, 3 = Blue)
			// return (string)	the 2-digit hex value for the requested portion 

			if (value == null || value == DependencyProperty.UnsetValue)
			{
				return Binding.DoNothing;
			}

			string val = value.ToString();
			if (val.Length < 9)
			{
				return new ArgumentException("Expected converter parameter to be a string in the form of #FF102030");
			}

			int part = Int32.Parse(parameter.ToString());

			string ret = val.Substring((part*2) + 1, 2);
			return ret;
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
