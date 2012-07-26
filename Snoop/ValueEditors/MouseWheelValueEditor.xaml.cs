// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

namespace Snoop.ValueEditors
{
	/// <summary>
	/// Interaction logic for MouseWheelValueEditor.xaml
	/// </summary>
	public partial class MouseWheelValueEditor : UserControl
	{
		public MouseWheelValueEditor()
		{
			InitializeComponent();

			MouseWheel += new MouseWheelEventHandler(MouseWheelHandler);
		}


		private void MouseWheelHandler(object sender, MouseWheelEventArgs e)
		{
			FrameworkElement fe = e.OriginalSource as FrameworkElement;
			if (fe == null)
			{
				return;
			}

			bool increment = true;
			bool largeIncrement = false;
			bool tinyIncrement = false;

			if (e.Delta > 0)
			{
				increment = false;
			}

			if (((Keyboard.GetKeyStates(Key.LeftShift) & KeyStates.Down) > 0)
					|| (Keyboard.GetKeyStates(Key.RightShift) & KeyStates.Down) > 0)
			{
				largeIncrement = true;
			}

			if (((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) > 0)
					|| (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) > 0)
			{
				tinyIncrement = true;
			}

			var tb = fe as TextBlock;
			if (tb != null)
			{
				int fieldNum = Int32.Parse(tb.Tag.ToString());

				switch (PropertyInfo.Property.PropertyType.Name)
				{
					case "Double":
						PropertyInfo.StringValue = ChangeDoubleValue(tb.Text, increment, largeIncrement, tinyIncrement);
						break;

					case "Int32":
					case "Int16":
						PropertyInfo.StringValue = ChangeIntValue(tb.Text, increment, largeIncrement);
						break;

					case "Boolean":
						PropertyInfo.StringValue = ChangeBooleanValue(tb.Text);
						break;

					case "Visibility":
						PropertyInfo.StringValue = ChangeEnumValue<Visibility>(tb.Text, increment);
						break;

					case "HorizontalAlignment":
						PropertyInfo.StringValue = ChangeEnumValue<HorizontalAlignment>(tb.Text, increment);
						break;
					case "VerticalAlignment":
						PropertyInfo.StringValue = ChangeEnumValue<VerticalAlignment>(tb.Text, increment);
						break;

					case "Thickness":
						ChangeThicknessValuePart(fieldNum, tb.Text, increment, largeIncrement);
						break;

					case "Brush":
						ChangeBrushValuePart(fieldNum, tb.Text, increment, largeIncrement);
						break;

					case "Color":
						ChangeBrushValuePart(fieldNum, tb.Text, increment, largeIncrement);
						break;
				}

				PropertyInfo.IsValueChangedByUser = true;
			}

			e.Handled = true;
		}


		private string ChangeIntValue(string current, bool increase, bool largeIncrement)
		{
			int change = 1;
			if (!increase)
			{
				change *= -1;
			}
			if (largeIncrement)
			{
				change *= 10;
			}

			int ret = Int32.Parse(current);
			ret = ret + change;

			return ret.ToString();
		}

		private string ChangeDoubleValue(string current, bool increase, bool largeIncrement, bool tinyIncrement)
		{
			double change = 1;
			if (!increase)
			{
				change *= -1;
			}
			if (largeIncrement)
			{
				change *= 10;
			}
			if (tinyIncrement)
			{
				change /= 10;
			}

			double ret = Double.Parse(current);
			ret = ret + change;

			return ret.ToString();
		}

		private string ChangeBooleanValue(string current)
		{
			bool ret = Boolean.Parse(current);
			ret = !ret;

			return ret.ToString();
		}

		private string ChangeEnumValue<T>(string current, bool increase)
		{
			T ret = (T)Enum.Parse(typeof(T), current);

			// make numeric, so we can add or subtract one
			int value = Convert.ToInt32(ret);
			if (increase)
			{
				value += 1;
			}
			else
			{
				value -= 1;
			}

			value = Math.Min(Enum.GetValues(typeof(T)).Length - 1, Math.Max(0, value));
			// long way around to get the enum typed value from the integer
			ret = (T)(Enum.GetValues(typeof(T)).GetValue(value));

			return ret.ToString();
		}

		/// <summary>
		/// Increments or decrements the field part in the Thickness value.
		/// Replaces that field in the underlying VALUE 
		/// </summary>
		private void ChangeThicknessValuePart(int fieldNum, string current, bool increase, bool largeIncrement)
		{
			int change = 1;
			if (!increase)
			{
				change *= -1;
			}
			if (largeIncrement)
			{
				change *= 20;
			}

			int newVal = Int32.Parse(current);
			newVal = newVal + change;

			string partValue = newVal.ToString();
			string currentValue = PropertyInfo.StringValue;

			// chop the current value up into its parts
			string[] fields = currentValue.Split(',');

			// replace the appropriate field
			fields[fieldNum - 1] = partValue;

			// re-assemble back to Brush value
			string newValue = String.Format(@"{0},{1},{2},{3}", fields[0], fields[1], fields[2], fields[3]);

			PropertyInfo.StringValue = newValue;
		}

		/// <summary>
		/// Increments or decrements the field part in the Brush value.
		/// Replaces that field in the underlying VALUE 
		/// </summary>
		private void ChangeBrushValuePart(int fieldNum, string current, bool increase, bool largeIncrement)
		{
			int change = 1;
			if (!increase)
			{
				change *= -1;
			}
			if (largeIncrement)
			{
				change *= 16;
			}

			int ret = Int32.Parse(current, NumberStyles.HexNumber);
			ret = Math.Min(255, Math.Max(0, ret + change));

			string partValue = ret.ToString("X2");
			string currentValue = PropertyInfo.StringValue;

			// chop the current value up into its parts
			string[] fields = new string[4];
			fields[0] = currentValue.Substring(1, 2);	// start at 1 to skip the leading # sign
			fields[1] = currentValue.Substring(3, 2);
			fields[2] = currentValue.Substring(5, 2);
			fields[3] = currentValue.Substring(7, 2);

			// replace the appropriate field
			fields[fieldNum - 1] = partValue;

			// re-assemble back to Brush value
			string newValue = String.Format(@"#{0}{1}{2}{3}", fields[0], fields[1], fields[2], fields[3]);

			PropertyInfo.StringValue = newValue;
		}


		private PropertyInformation PropertyInfo
		{
			get
			{
				return DataContext as PropertyInformation;
			}
		}
	}
}
