// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using System;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Controls;

namespace Snoop
{
	public partial class StandardValueEditor: ValueEditor
	{
		public StandardValueEditor()
		{
		}



        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
            set { SetValue(VerticalScrollBarVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VerticalScrollBarVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
            DependencyProperty.Register("VerticalScrollBarVisibility", typeof(ScrollBarVisibility), typeof(StandardValueEditor), new PropertyMetadata(ScrollBarVisibility.Hidden));



        public double TextBoxHeight
        {
            get { return (double)GetValue(TextBoxHeightProperty); }
            set { SetValue(TextBoxHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextBoxHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextBoxHeightProperty =
            DependencyProperty.Register("TextBoxHeight", typeof(double), typeof(StandardValueEditor), new PropertyMetadata(0.0d));

		public string StringValue
		{
			get { return (string)this.GetValue(StandardValueEditor.StringValueProperty); }
			set { this.SetValue(StandardValueEditor.StringValueProperty, value); }
		}
		public static readonly DependencyProperty StringValueProperty =
			DependencyProperty.Register
			(
				"StringValue",
				typeof(string),
				typeof(StandardValueEditor),
				new PropertyMetadata(StandardValueEditor.HandleStringPropertyChanged)
			);
		private static void HandleStringPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((StandardValueEditor)sender).OnStringPropertyChanged((string)e.NewValue);
		}
		protected virtual void OnStringPropertyChanged(string newValue)
		{
			if (this.isUpdatingValue)
				return;

			if (PropertyInfo != null)
			{
				PropertyInfo.IsValueChangedByUser = true;
			}

			Type targetType = this.PropertyType;

			if (targetType.IsAssignableFrom(typeof(string)))
			{
				this.Value = newValue;
			}
			else
			{
				TypeConverter converter = TypeDescriptor.GetConverter(targetType);
				if (converter != null)
				{
					try
					{
						SetValueFromConverter(newValue, targetType, converter);
					}
					catch (Exception)
					{
					}
				}
			}
		}

		private void SetValueFromConverter(string newValue, Type targetType, TypeConverter converter)
		{
			if (!converter.CanConvertFrom(targetType) && string.IsNullOrEmpty(newValue))
			{
				this.Value = null;
			}
			else
			{
				this.Value = converter.ConvertFrom(newValue);
			}
		}


		protected override void OnValueChanged(object newValue)
		{
			this.isUpdatingValue = true;

			object value = this.Value;
			if (value != null)
				this.StringValue = value.ToString();
			else
				this.StringValue = string.Empty;

			this.isUpdatingValue = false;

			BindingExpression binding = BindingOperations.GetBindingExpression(this, StandardValueEditor.StringValueProperty);
			if (binding != null)
				binding.UpdateSource();
		}


		private bool isUpdatingValue = false;
	}
}
