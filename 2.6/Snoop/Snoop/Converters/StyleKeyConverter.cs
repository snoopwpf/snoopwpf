using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.ComponentModel;

namespace Snoop.Converters
{
	public class StyleKeyConverter : IValueConverter
	{
		#region IValueConverter Members
		/// <summary>
		/// Converts a Style to a StyleKeyPair if possible ... and if not, just returns the Style object.
		/// </summary>
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// if the value to be converted is not a style
			// or
			// the parameter is not a FrameworkElement or is null
			// then
			// just return the Style object
			var frameworkElement = parameter as FrameworkElement;
			if (frameworkElement == null || !(value is Style))
				return value;

			string styleKey = StyleKeyHelpers.GetKeyOfStyle(frameworkElement);
			if (string.IsNullOrEmpty(styleKey))
			{
				// if we can't find the key, just return the Style object
				return value;
			}
			else
			{
				// else create a StyleKeyPair and return that
				return new StyleKeyPair { Style = (Style)value, Key = styleKey };
			}
		}
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return value;
		}
		#endregion
	}

	public class StyleKeyPair
	{
		public Style Style { get; set; }

		[DisplayName("x:Key")]
		public string Key { get; set; }

		public override string ToString()
		{
			return string.IsNullOrEmpty(Key) ? Style.ToString() : Key + " (Style)";
		}
	}

	public static class StyleKeyHelpers
	{
		public static string GetKeyOfStyle(FrameworkElement frameworkElement)
		{
			Style style = frameworkElement.Style;
			if (style != null)
			{
				// check the resource dictionary on the target FrameworkElement first.
				string name = FindNameFromResource(frameworkElement.Resources, style);
				if (name != null)
					return name;

				// get the parent of the target and check its resource dictionary
				// if not found, continue traveling up the hierarchy and checking
				DependencyObject d = VisualTreeHelper.GetParent(frameworkElement);
				while (d != null)
				{
					FrameworkElement fe = d as FrameworkElement;
					if (fe != null)
					{
						name = FindNameFromResource(fe.Resources, style);
					}
					if (name != null)
					{
						return name;
					}

					if (fe != null && fe.Parent != null)
					{
						d = fe.Parent;
					}
					else
					{
						d = VisualTreeHelper.GetParent(d);
					}
				}

				// check the application resources
				if (Application.Current != null)
				{
					name = FindNameFromResource(Application.Current.Resources, style);
					if (name != null)
						return name;
				}
			}
			return string.Empty;
		}

		public static string FindNameFromResource(ResourceDictionary dictionary, object resourceItem)
		{
			foreach (object key in dictionary.Keys)
			{
				if (dictionary[key] == resourceItem)
				{
					return key.ToString();
				}
			}

			if (dictionary.MergedDictionaries != null)
			{
				foreach (var dic in dictionary.MergedDictionaries)
				{
					string name = FindNameFromResource(dic, resourceItem);
					if (!string.IsNullOrEmpty(name))
					{
						return name;
					}
				}
			}

			return null;
		}
	}
}
